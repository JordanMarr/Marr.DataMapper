using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping.Strategies;

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
        /// Captures all properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ColumnMapCollection BuildColumns<T>()
        {
            ColumnMapCollection maps = LoadAllColumns<T>();
            return maps;
        }

        /// <summary>
        /// Creates column mappings for the given type.  
        /// Captures only the properties that are listed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertiesToInclude"></param>
        /// <returns></returns>
        public ColumnMapCollection BuildColumns<T>(params string[] propertiesToInclude)
        {
            ColumnMapCollection maps = LoadAllColumns<T>();
            maps.RemoveAll(p => !propertiesToInclude.Contains(p.FieldName));
            return maps;
        }
                       
        /// <summary>
        /// Creates column mappings for the given type.
        /// Captures all properties except those that are listed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="properties"></param>
        /// <returns></returns>
        public ColumnMapCollection BuildColumnsExcept<T>(params string[] properties)
        {
            ColumnMapCollection maps = LoadAllColumns<T>();
            maps.RemoveAll(p => properties.Contains(p.FieldName));
            return maps;
        }

        private ColumnMapCollection LoadAllColumns<T>()
        {
            var mapStrategy = new PropertyMapStrategy(_publicOnly);
            return mapStrategy.MapColumns(typeof(T));
        }

        #endregion

        #region - Relationships -

        /// <summary>
        /// Creates relationship mappings for the given type.
        /// Captures relationship attributes and ICollection fields.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public RelationshipCollection BuildRelationships<T>()
        {
            var maps = LoadAllRelationships<T>();
            return maps;
        }

        /// <summary>
        /// Creates relationship mappings for the given type.
        /// Only captures the properties that are listed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertiesToInclude"></param>
        /// <returns></returns>
        public RelationshipCollection BuildRelationships<T>(params string[] propertiesToInclude)
        {
            var maps = LoadAllRelationships<T>();
            maps.RemoveAll(r => !propertiesToInclude.Contains(r.Member.Name));
            return maps;
        }

        private RelationshipCollection LoadAllRelationships<T>()
        {
            var mapStrategy = new PropertyMapStrategy(_publicOnly);
            return mapStrategy.MapRelationships(typeof(T));
        }

        #endregion
    }
}
