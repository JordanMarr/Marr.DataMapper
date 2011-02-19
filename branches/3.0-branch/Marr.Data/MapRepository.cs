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
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data.Common;
using Marr.Data.Converters;
using Marr.Data.Parameters;
using Marr.Data.Mapping;
using Marr.Data.Mapping.Strategies;

namespace Marr.Data
{
    public class MapRepository
    {
        private Dictionary<Type, ColumnMapCollection> _columns;
        private Dictionary<Type, RelationshipCollection> _relationships;
        private FastReflection.CachedReflector _reflector;
        private IDbTypeBuilder _dbTypeBuilder;
        private Dictionary<Type, IMapStrategy> _columnMapStrategies;
        internal Dictionary<Type, IConverter> TypeConverters { get; set; }

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static MapRepository()
        { }

        private MapRepository()
        {
            _columns = new Dictionary<Type, ColumnMapCollection>();
            _relationships = new Dictionary<Type, RelationshipCollection>();
            _reflector = new FastReflection.CachedReflector();
            TypeConverters = new Dictionary<Type, IConverter>();
            
            // Register a default type converter for Enums
            TypeConverters.Add(typeof(Enum), new Converters.EnumStringConverter());

            // Register a default IDbTypeBuilder
            _dbTypeBuilder = new Parameters.DbTypeBuilder();

            _columnMapStrategies = new Dictionary<Type, IMapStrategy>();
            RegisterDefaultMapStrategy(new AttributeMapStrategy());
        }

        private readonly static MapRepository _instance = new MapRepository();

        /// <summary>
        /// Gets a reference to the singleton MapRepository.
        /// </summary>
        public static MapRepository Instance
        {
            get
            {
                return _instance;
            }
        }

        #region - Column Map Strategies -

        public void RegisterDefaultMapStrategy(IMapStrategy strategy)
        {
            RegisterMapStrategy(typeof(object), strategy);
        }

        public void RegisterMapStrategy(Type entityType, IMapStrategy strategy)
        {
            if (_columnMapStrategies.ContainsKey(entityType))
                _columnMapStrategies[entityType] = strategy;
            else
                _columnMapStrategies.Add(entityType, strategy);
        }

        private IMapStrategy GetMapStrategy(Type entityType)
        {
            if (_columnMapStrategies.ContainsKey(entityType))
            {
                // Return entity specific column map strategy
                return _columnMapStrategies[entityType];
            }
            else
            {
                // Return the default column map strategy
                return _columnMapStrategies[typeof(object)];
            }
        }

        #endregion

        #region - Columns repository -

        internal ColumnMapCollection GetColumns(Type entityType)
        {
            if (_columns.ContainsKey(entityType))
            {
                return _columns[entityType];
            }
            else
            {
                ColumnMapCollection columnMaps = GetMapStrategy(entityType).MapColumns(entityType);
                _columns.Add(entityType, columnMaps);
                return columnMaps;
            }
        }

        #endregion

        #region - Relationships repository -

        public RelationshipCollection GetRelationships(Type type)
        {
            if (_relationships.ContainsKey(type))
            {
                return _relationships[type];
            }
            else
            {
                RelationshipCollection relationships = GetMapStrategy(type).MapRelationships(type);
                _relationships.Add(type, relationships);
                return relationships;
            }
        }
        
        #endregion

        #region - Cached Reflector -

        /// <summary>
        /// Gets a CachedReflector instance.
        /// </summary>
        internal FastReflection.CachedReflector Reflector
        {
            get { return _reflector; }
        }

        #endregion

        #region - Type Converters -

        /// <summary>
        /// Registers a converter for a given type.
        /// </summary>
        /// <param name="type">The CLR data type that will be converted.</param>
        /// <param name="converter">An IConverter object that will handle the data conversion.</param>
        public void RegisterTypeConverter(Type type, IConverter converter)
        {
            if (TypeConverters.ContainsKey(type))
            {
                TypeConverters[type] = converter;
            }
            else
            {
                TypeConverters.Add(type, converter);
            }
        }

        /// <summary>
        /// Checks for a type converter (if one exists).
        /// 1) Checks for a converter registered for the current columns data type.
        /// 2) Checks to see if a converter is registered for all enums (type of Enum) if the current column is an enum.
        /// 3) Checks to see if a converter is registered for all objects (type of Object).
        /// </summary>
        /// <param name="dataMap">The current data map.</param>
        /// <returns>Returns an IConverter object or null if one does not exist.</returns>
        internal IConverter GetConverter(Type dataType)
        {
            if (TypeConverters.ContainsKey(dataType))
            {
                // User registered type converter
                return TypeConverters[dataType];
            }
            else if (TypeConverters.ContainsKey(typeof(Enum)) && dataType.IsEnum)
            {
                // A converter is registered to handled enums
                return TypeConverters[typeof(Enum)];
            }
            else if (TypeConverters.ContainsKey(typeof(object)))
            {
                // User registered default converter
                return TypeConverters[typeof(object)];
            }
            else
            {
                // No conversion
                return null;
            }
        }

        #endregion

        #region - DbTypeBuilder -

        /// <summary>
        /// Gets or sets the IDBTypeBuilder that is responsible for converting parameter DbTypes based on the parameter value.
        /// Defaults to use the DbTypeBuilder.  
        /// You can replace this with a more specific builder if you want more control over the way the parameter types are set.
        /// </summary>
        public IDbTypeBuilder DbTypeBuilder
        {
            get { return _dbTypeBuilder; }
            set { _dbTypeBuilder = value; }
        }

        #endregion
    }
}