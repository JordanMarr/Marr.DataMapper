using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping.Strategies;
using System.Reflection;
using System.Collections;

namespace Marr.Data.Mapping
{
    public class MapBuilder
    {
        private bool _publicOnly;

        public MapBuilder()
            : this(true)
        { }

        public MapBuilder(bool publicOnly)
        {
            _publicOnly = publicOnly;
        }

        #region - Columns -

        /// <summary>
        /// Creates column mappings for the given type.
        /// Maps all properties except ICollection properties.
        /// </summary>
        /// <typeparam name="T">The type that is being built.</typeparam>
        /// <returns><see cref="ColumnMapCollection"/></returns>
        public ColumnMapCollection BuildColumns<T>()
        {
            return BuildColumns<T>(m => m.MemberType == MemberTypes.Property &&
                !typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType));
        }

        /// <summary>
        /// Creates column mappings for the given type.  
        /// Maps properties that are included in the include list.
        /// </summary>
        /// <typeparam name="T">The type that is being built.</typeparam>
        /// <param name="propertiesToInclude"></param>
        /// <returns><see cref="ColumnMapCollection"/></returns>
        public ColumnMapCollection BuildColumns<T>(params string[] propertiesToInclude)
        {
            return BuildColumns<T>(m =>
                m.MemberType == MemberTypes.Property &&
                propertiesToInclude.Contains(m.Name));
        }

        /// <summary>
        /// Creates column mappings for the given type.
        /// Maps all properties except the ones in the exclusion list.
        /// </summary>
        /// <typeparam name="T">The type that is being built.</typeparam>
        /// <param name="propertiesToExclude"></param>
        /// <returns><see cref="ColumnMapCollection"/></returns>
        public ColumnMapCollection BuildColumnsExcept<T>(params string[] propertiesToExclude)
        {
            return BuildColumns<T>(m => 
                m.MemberType == MemberTypes.Property &&
                !propertiesToExclude.Contains(m.Name));
        }

        /// <summary>
        /// Creates column mappings for the given type if they match the predicate.
        /// </summary>
        /// <typeparam name="T">The type that is being built.</typeparam>
        /// <param name="predicate">Determines whether a mapping should be created based on the member info.</param>
        /// <returns><see cref="ColumnMapCollection"/></returns>
        public ColumnMapCollection BuildColumns<T>(Func<MemberInfo, bool> predicate)
        {
            Type entityType = typeof(T);
            ConventionMapStrategy strategy = new ConventionMapStrategy(_publicOnly);
            strategy.ColumnPredicate = predicate;
            ColumnMapCollection columns = strategy.MapColumns(entityType);
            MapRepository.Instance.Columns[entityType] = columns;
            return columns;
        }
        
        #endregion

        #region - Relationships -

        /// <summary>
        /// Creates relationship mappings for the given type.
        /// Maps all properties that implement ICollection.
        /// </summary>
        /// <typeparam name="T">The type that is being built.</typeparam>
        /// <returns><see cref="ColumnMapCollection"/></returns>
        public RelationshipCollection BuildRelationships<T>()
        {
            return BuildRelationships<T>(m => 
                m.MemberType == MemberTypes.Property && 
                typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType));
        }

        /// <summary>
        /// Creates relationship mappings for the given type.
        /// Maps all properties that are listed in the include list.
        /// </summary>
        /// <typeparam name="T">The type that is being built.</typeparam>
        /// <param name="propertiesToInclude"></param>
        /// <returns><see cref="ColumnMapCollection"/></returns>
        public RelationshipCollection BuildRelationships<T>(params string[] propertiesToInclude)
        {
            return BuildRelationships<T>(m => 
                m.MemberType == MemberTypes.Property && 
                typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType) &&
                propertiesToInclude.Contains(m.Name));
        }

        /// <summary>
        /// Creates relationship mappings for the given type if they match the predicate.
        /// </summary>
        /// <typeparam name="T">The type that is being built.</typeparam>
        /// <param name="predicate">Determines whether a mapping should be created based on the member info.</param>
        /// <returns><see cref="ColumnMapCollection"/></returns>
        public RelationshipCollection BuildRelationships<T>(Func<MemberInfo, bool> predicate)
        {
            Type entityType = typeof(T);
            ConventionMapStrategy strategy = new ConventionMapStrategy(_publicOnly);
            strategy.RelationshipPredicate = predicate;
            RelationshipCollection relationships = strategy.MapRelationships(entityType);
            MapRepository.Instance.Relationships[entityType] = relationships;
            return relationships;
        }
        
        #endregion

        #region - Tables -

        /// <summary>
        /// Sets the table name for a given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName"></param>
        public void SetTableName<T>(string tableName)
        {
            MapRepository.Instance.Tables[typeof(T)] = tableName;
        }

        #endregion
    }
}
