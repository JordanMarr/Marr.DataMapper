/*  Copyright (C) 2008 - 2011 Jordan Marr

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Linq;
using Marr.Data.Mapping;

namespace Marr.Data
{
    /// <summary>
    /// Holds metadata about an object graph that is being queried and loaded.
    /// Contains all metadata needed to instantiate the object and fill it with data from a DataReader.
    /// </summary>
    internal class EntityGraph : IEnumerable<EntityGraph>
    {
        EntityGraph _parent;
        private Type _entityType;
        private Relationship _relationship;
        private ColumnMapCollection _columns;
        private RelationshipCollection _relationships;
        private List<EntityGraph> _children;
        private object _entity;
        private ColumnMapCollection _groupingKeyColumns;
        private List<string> _groupingKeys;

        /// <summary>
        /// A reference to the list where entities should be added to.
        /// For the first entity, this is the query results list.
        /// For child entities, this is the child relationship list of the parent.
        /// </summary>
        private IList _owningList;

        /// <summary>
        /// Recursively builds an entity graph of the given parent type.
        /// </summary>
        /// <param name="entityType"></param>
        public EntityGraph(Type entityType, IList queryResults)
            : this(entityType, null, null)
        {
            _owningList = queryResults;
        }

        private EntityGraph(Type entityType, EntityGraph parent, Relationship relationship)
        {
            MapRepository repository = MapRepository.Instance;

            _entityType = entityType;
            _parent = parent;
            _relationship = relationship;
            _columns = repository.GetColumns(entityType);
            _relationships = repository.GetRelationships(entityType);
            _children = new List<EntityGraph>();
            _groupingKeys = new List<string>();

            // Create and add children
            foreach (Relationship childRelationship in this.Relationships)
            {
                _children.Add(new EntityGraph(childRelationship.RelationshipInfo.EntityType, this, childRelationship));
            }
        }

        /// <summary>
        /// Gets the parent of this EntityGraph.
        /// </summary>
        public EntityGraph Parent
        {
            get
            {
                return _parent;
            }
        }

        /// <summary>
        /// Gets the Type of this EntityGraph.
        /// </summary>
        public Type EntityType
        {
            get { return _entityType; }
        }

        /// <summary>
        /// Gets a boolean than indicates whether this entity is the root node in the graph.
        /// </summary>
        public bool IsRoot
        {
            get
            {
                return _parent == null;
            }
        }

        /// <summary>
        /// Gets a boolean that indicates whether this entity is a child.
        /// </summary>
        public bool IsChild
        {
            get
            {
                return _parent != null;
            }
        }

        /// <summary>
        /// Gets the columns mapped to this entity.
        /// </summary>
        public ColumnMapCollection Columns
        {
            get { return _columns; }
        }

        /// <summary>
        /// Gets the relationships mapped to this entity.
        /// </summary>
        public RelationshipCollection Relationships
        {
            get { return _relationships; }
        }

        /// <summary>
        /// A list of EntityGraph objects that hold metadata about the child entities that will be loaded.
        /// </summary>
        public List<EntityGraph> Children
        {
            get { return _children; }
        }
        
        /// <summary>
        /// Adds an entity to the appropriate place in the object graph.
        /// </summary>
        /// <param name="entityInstance"></param>
        public void AddEntity(object entityInstance)
        {
            this._entity = entityInstance;

            // Add newly created entityInstance to list (Many) or set it to field (One)
            if (this.IsRoot || _relationship.RelationshipInfo.RelationType == RelationshipTypes.Many)
            {
                _owningList.Add(entityInstance);
            }
            else // RelationshipTypes.One
            {
                ReflectionHelper.SetFieldValue(_parent._entity, _relationship.Member.Name, entityInstance);
            }

            InitOneToManyChildLists(entityInstance);
        }

        /// <summary>
        /// Concatenates the values of the GroupingKeys property and compares them
        /// against the LastKeyGroup property.  Returns true if the values are different,
        /// or false if the values are the same.
        /// The currently concatenated keys are saved in the LastKeyGroup property.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public bool IsNewGroup(DbDataReader reader)
        {
            bool isNewGroup = false;

            // Get primary keys from parent entity and any one-to-one child entites
            ColumnMapCollection groupingKeyColumns = this.GroupingKeyColumns;

            // Concatenate column values
            string groupingKey = CreateGroupingKey(groupingKeyColumns, reader);

            if (groupingKey != string.Empty        // Create a new group if PK is not empty
                && _groupingKeys.LastOrDefault() != groupingKey)    // and PK values are not the same
            {
                isNewGroup = true;

                if (_groupingKeys.Contains(groupingKey))
                {
                    throw new DataMappingException("DataMapper QueryToGraph has detected query results that have not been properly ordered. Please ensure that the query is sorted by all parent nodes from top to bottom.");
                }

                // Save last pk value
                _groupingKeys.Add(groupingKey);
            }

            return isNewGroup;
        }
        
        /// <summary>
        /// Gets the GroupingKeys for this entity.  
        /// GroupingKeys determine when to create and add a new entity to the graph.
        /// </summary>
        /// <remarks>
        /// A simple entity with no relationships will return only its PrimaryKey columns.
        /// A parent entity with one-to-one child relationships will include its own PrimaryKeys,
        /// and it will recursively traverse all Children with one-to-one relationships and add their PrimaryKeys.
        /// A child entity that has a one-to-one relationship with its parent will use the same 
        /// GroupingKeys already defined by its parent.
        /// </remarks>
        public ColumnMapCollection GroupingKeyColumns
        {
            get
            {
                if (_groupingKeyColumns == null)
                    _groupingKeyColumns = GetGroupingKeyColumns();

                return _groupingKeyColumns;
            }
        }

        /// <summary>
        /// Initializes the owning lists on many-to-many Children.
        /// </summary>
        /// <param name="entityInstance"></param>
        private void InitOneToManyChildLists(object entityInstance)
        {
            // Get a reference to the parent's the childrens' OwningLists to the parent entity
            for (int i = 0; i < Relationships.Count; i++)
            {
                Relationship relationship = Relationships[i];
                if (relationship.RelationshipInfo.RelationType == RelationshipTypes.Many)
                {
                    try
                    {
                        IList list = (IList)ReflectionHelper.CreateInstance(relationship.MemberType);
                        Children[i]._owningList = list;
                        ReflectionHelper.SetFieldValue(entityInstance, relationship.Member.Name, list);
                    }
                    catch (Exception ex)
                    {
                        throw new DataMappingException(
                            string.Format("{0}.{1} is a \"Many\" relationship type so it must derive from IList.",
                                entityInstance.GetType().Name, relationship.Member.Name),
                            ex);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively adds primary key columns from contiguous child graphs with a one-to-one relationship type to the pKeys collection..
        /// </summary>
        /// <param name="pKeys"></param>
        /// <param name="entity"></param>
        private void AddOneToOneChildKeys(ColumnMapCollection pKeys, EntityGraph entity)
        {
            var oneToOneChildren = entity.Children
                .Where(c => c._relationship.RelationshipInfo.RelationType == RelationshipTypes.One);

            foreach (var child in oneToOneChildren)
            {
                pKeys.AddRange(child.Columns.PrimaryKeys);
                AddOneToOneChildKeys(pKeys, child);
            }
        }

        /// <summary>
        /// Gets a list of keys to group by.
        /// </summary>
        /// <remarks>
        /// When converting an unnormalized set of data from a database view,
        /// a new entity is only created when the grouping keys have changed.
        /// NOTE: This behavior works on the assumption that the view result set
        /// has been sorted by the root entity primary key(s), followed by the
        /// child entity primary keys.
        /// </remarks>
        /// <returns></returns>
        private ColumnMapCollection GetGroupingKeyColumns()
        {
            // Get primary keys for this parent entity
            ColumnMapCollection groupingKeyColumns = Columns.PrimaryKeys;

            bool isEndNode = this.Children.Count == 0;
            if (!isEndNode && groupingKeyColumns.Count == 0)
                throw new MissingPrimaryKeyException(string.Format("There are no primary key mappings defined for the following entity: '{0}'.", this.EntityType.Name));

            // Add parent's keys
            if (IsChild)
                groupingKeyColumns.AddRange(Parent.GroupingKeyColumns);

            AddOneToOneChildKeys(groupingKeyColumns, this);

            return groupingKeyColumns;
        }

        /// <summary>
        /// Returns a concatented string containing the primary key values of the current record.
        /// If any of the PKs are null or empty, the entire grouping key will be string.Empty.
        /// </summary>
        /// <param name="primaryKeys">The mapped primary keys for this entity.</param>
        /// <param name="reader">The open data reader.</param>
        /// <returns>Returns the primary key value(s) as a string.</returns>
        private string CreateGroupingKey(ColumnMapCollection columns, DbDataReader reader)
        {
            StringBuilder pkValues = new StringBuilder();
            foreach (ColumnMap pkColumn in columns)
            {
                string pkValue = reader[pkColumn.ColumnInfo.GetColumName(true)].ToString();
                
                // A primary key should not have a null value
                if (string.IsNullOrEmpty(pkValue))
                    return string.Empty;

                pkValues.Append(reader[pkColumn.ColumnInfo.GetColumName(true)].ToString());
            }
            return pkValues.ToString();
        }

        #region IEnumerable<EntityGraph> Members

        public IEnumerator<EntityGraph> GetEnumerator()
        {
            return TraverseGraph(this);
        }

        /// <summary>
        /// Recursively traverses through every entity in the EntityGraph.
        /// </summary>
        /// <param name="entityGraph"></param>
        /// <returns></returns>
        private static IEnumerator<EntityGraph> TraverseGraph(EntityGraph entityGraph)
        {
            Stack<EntityGraph> stack = new Stack<EntityGraph>();
            stack.Push(entityGraph);

            while (stack.Count > 0)
            {
                EntityGraph node = stack.Pop();
                yield return node;

                foreach (EntityGraph childGraph in node.Children)
                {
                    stack.Push(childGraph);
                }
            }
        }


        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
