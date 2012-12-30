using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Marr.Data.Mapping.Strategies;
using System.Collections;

namespace Marr.Data.Mapping
{
    /// <summary>
    /// Provides a fluent interface for mapping domain entities and properties to database tables and columns.
    /// </summary>
    public class Mappings
    {
        private bool _publicOnly;

        public Mappings()
            : this(true)
        { }

        public Mappings(bool publicOnly)
        {
            _publicOnly = publicOnly;
            Columns = new MapBuilderColumns(publicOnly);
            Tables = new MapBuilderTables();
            Relationships = new MapBuilderRelationships(publicOnly);
        }

        /// <summary>
        /// Contains methods that map entity properties to database table and view column names;
        /// </summary>
        public MapBuilderColumns Columns { get; private set; }

        /// <summary>
        /// Contains methods that map entity classes to database table names.
        /// </summary>
        public MapBuilderTables Tables { get; private set; }

        /// <summary>
        /// Contains methods that map sub-entities with database table and view column names.
        /// </summary>
        public MapBuilderRelationships Relationships { get; private set; }

        public class MapBuilderColumns
        {
            private bool _publicOnly;

            public MapBuilderColumns(bool publicOnly)
            {
                _publicOnly = publicOnly;
            }

            /// <summary>
            /// Creates column mappings for the given type.
            /// Maps all properties except ICollection properties.
            /// </summary>
            /// <typeparam name="T">The type that is being built.</typeparam>
            /// <returns><see cref="ColumnMapCollection"/></returns>
            public ColumnMapBuilder<T> AutoMapAllProperties<T>()
            {
                return AutoMapPropertiesWhere<T>(m => m.MemberType == MemberTypes.Property &&
                    !typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType));
            }

            /// <summary>
            /// Creates column mappings for the given type.
            /// Maps all properties that are simple types (int, string, DateTime, etc).  
            /// ICollection properties are not included.
            /// </summary>
            /// <typeparam name="T">The type that is being built.</typeparam>
            /// <returns><see cref="ColumnMapCollection"/></returns>
            public ColumnMapBuilder<T> AutoMapSimpleTypeProperties<T>()
            {
                return AutoMapPropertiesWhere<T>(m => m.MemberType == MemberTypes.Property &&
                    DataHelper.IsSimpleType((m as PropertyInfo).PropertyType) &&
                    !typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType));
            }

            /// <summary>
            /// Creates column mappings for the given type if they match the predicate.
            /// </summary>
            /// <typeparam name="T">The type that is being built.</typeparam>
            /// <param name="predicate">Determines whether a mapping should be created based on the member info.</param>
            /// <returns><see cref="ColumnMapConfigurator"/></returns>
            public ColumnMapBuilder<T> AutoMapPropertiesWhere<T>(Func<MemberInfo, bool> predicate)
            {
                Type entityType = typeof(T);
                ConventionMapStrategy strategy = new ConventionMapStrategy(_publicOnly);
                strategy.ColumnPredicate = predicate;
                ColumnMapCollection columns = strategy.MapColumns(entityType);
                MapRepository.Instance.Columns[entityType] = columns;
                return new ColumnMapBuilder<T>(columns);
            }

            /// <summary>
            /// Creates a ColumnMapBuilder that starts out with no pre-populated columns.
            /// All columns must be added manually using the builder.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public ColumnMapBuilder<T> MapProperties<T>()
            {
                Type entityType = typeof(T);
                ColumnMapCollection columns = new ColumnMapCollection();
                MapRepository.Instance.Columns[entityType] = columns;
                return new ColumnMapBuilder<T>(columns);
            }
        }

        public class MapBuilderTables
        {
            /// <summary>
            /// Provides a fluent table mapping interface.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public TableBuilder<T> AutoMapTable<T>()
            {
                return new TableBuilder<T>();
            }

            /// <summary>
            /// Sets the table name for a given type.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="tableName"></param>
            public TableBuilder<T> MapTable<T>(string tableName)
            {
                return new TableBuilder<T>().SetTableName(tableName);
            }
        }

        public class MapBuilderRelationships
        {
            private bool _publicOnly;

            public MapBuilderRelationships(bool publicOnly)
            {
                _publicOnly = publicOnly;
            }

            /// <summary>
            /// Creates relationship mappings for the given type.
            /// Maps all properties that implement ICollection or are not "simple types".
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public RelationshipBuilder<T> AutoMapICollectionOrComplexProperties<T>()
            {
                return AutoMapPropertiesWhere<T>(m =>
                    m.MemberType == MemberTypes.Property &&
                    (
                        typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType) || !DataHelper.IsSimpleType((m as PropertyInfo).PropertyType)
                    )
                );

            }

            /// <summary>
            /// Creates relationship mappings for the given type.
            /// Maps all properties that implement ICollection.
            /// </summary>
            /// <typeparam name="T">The type that is being built.</typeparam>
            /// <returns><see cref="RelationshipBuilder"/></returns>
            public RelationshipBuilder<T> AutoMapICollectionProperties<T>()
            {
                return AutoMapPropertiesWhere<T>(m =>
                    m.MemberType == MemberTypes.Property &&
                    typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType));
            }

            /// <summary>
            /// Creates relationship mappings for the given type.
            /// Maps all properties that are not "simple types".
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public RelationshipBuilder<T> AutoMapComplexTypeProperties<T>()
            {
                return AutoMapPropertiesWhere<T>(m =>
                    m.MemberType == MemberTypes.Property &&
                    !DataHelper.IsSimpleType((m as PropertyInfo).PropertyType));
            }

            /// <summary>
            /// Creates relationship mappings for the given type if they match the predicate.
            /// </summary>
            /// <typeparam name="T">The type that is being built.</typeparam>
            /// <param name="predicate">Determines whether a mapping should be created based on the member info.</param>
            /// <returns><see cref="RelationshipBuilder"/></returns>
            public RelationshipBuilder<T> AutoMapPropertiesWhere<T>(Func<MemberInfo, bool> predicate)
            {
                Type entityType = typeof(T);
                ConventionMapStrategy strategy = new ConventionMapStrategy(_publicOnly);
                strategy.RelationshipPredicate = predicate;
                RelationshipCollection relationships = strategy.MapRelationships(entityType);
                MapRepository.Instance.Relationships[entityType] = relationships;
                return new RelationshipBuilder<T>(relationships);
            }

            /// <summary>
            /// Creates a RelationshipBuilder that starts out with no pre-populated relationships.
            /// All relationships must be added manually using the builder.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public RelationshipBuilder<T> MapProperties<T>()
            {
                Type entityType = typeof(T);
                RelationshipCollection relationships = new RelationshipCollection();
                MapRepository.Instance.Relationships[entityType] = relationships;
                return new RelationshipBuilder<T>(relationships);
            }
        }
    }
}
