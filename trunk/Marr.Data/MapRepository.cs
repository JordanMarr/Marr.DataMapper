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
using Marr.Data.Mapping;
using System.Data.Common;
using Marr.Data.Converters;
using Marr.Data.Parameters;

namespace Marr.Data
{
    public class MapRepository
    {
        private Dictionary<Type, ColumnMapCollection> _columns;
        private Dictionary<Type, RelationshipCollection> _relationships;
        private FastReflection.CachedReflector _reflector;
        private IDbTypeBuilder _dbTypeBuilder;
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

        #region - Columns repository -

        internal ColumnMapCollection GetColumns(Type type)
        {
            if (_columns.ContainsKey(type))
            {
                return _columns[type];
            }
            else
            {
                ColumnMapCollection columnMaps = ReflectColumns(type);
                _columns.Add(type, columnMaps);
                return columnMaps;
            }
        }

        private ColumnMapCollection ReflectColumns(Type type)
        {
            ColumnMapCollection mappings = new ColumnMapCollection();
            MemberInfo[] members = type.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            foreach (MemberInfo member in members)
            {
                if (!member.IsDefined(typeof(ColumnAttribute), false))
                    continue;

                ColumnAttribute column = (ColumnAttribute)member.GetCustomAttributes(typeof(ColumnAttribute), false)[0];

                // If the column name is not specified, the field name will be used.
                if (string.IsNullOrEmpty(column.Name))
                    column.Name = member.Name;

                // Determine property/field type
                Type memberType = ReflectionHelper.GetMemberType(member);

                Type paramNetType = memberType;

                // Handle conversions
                if (column.Converter != ConverterType.None)
                {
                    IConverter converter = ConverterFactory.Create(column.Converter);
                    paramNetType = converter.DbType;
                }
                else if (TypeConverters.ContainsKey(memberType))
                {
                    paramNetType = TypeConverters[memberType].DbType;
                }

                try
                {
                    // Get database specific DbType and store with column map in cache    
                    Enum dbType = DbTypeBuilder.GetDbType(paramNetType);
                    ColumnMap columnMap = new ColumnMap(member.Name, memberType, dbType, column);
                    mappings.Add(columnMap);
                }
                catch (ArgumentException)
                {
                    Enum dbType = DbTypeBuilder.GetDbType(paramNetType);
                    string msg = string.Format(
                        "The DataMapper failed to load '{0}.{1}'.  The configured db data type '{2}' is incompatible with the current database provider.",
                        type.Name, member.Name, dbType.ToString());
                    throw new ArgumentException(msg);
                }
            }

            return mappings;
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
                RelationshipCollection relationships = ReflectRelationships(type);
                _relationships.Add(type, relationships);
                return relationships;
            }
        }

        private RelationshipCollection ReflectRelationships(Type type)
        {
            RelationshipCollection relationships = new RelationshipCollection();
            MemberInfo[] members = type.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            foreach (MemberInfo member in members)
            {
                if (!member.IsDefined(typeof(RelationshipAttribute), false))
                    continue;

                RelationshipAttribute rInfo = (RelationshipAttribute)member.GetCustomAttributes(typeof(RelationshipAttribute), false)[0];

                Type memberType = ReflectionHelper.GetMemberType(member);

                // Try to determine the RelationshipType
                if (rInfo.RelationType == RelationshipTypes.AutoDetect)
                {
                    if (typeof(System.Collections.ICollection).IsAssignableFrom(memberType))
                    {
                        rInfo.RelationType = RelationshipTypes.Many;
                    }
                    else
                    {
                        rInfo.RelationType = RelationshipTypes.One;
                    }
                }

                // Try to determine the EntityType
                if (rInfo.EntityType == null)
                {
                    if (rInfo.RelationType == RelationshipTypes.Many)
                    {
                        if (memberType.IsGenericType)
                        {
                            // Assume a Collection<T> or List<T> and return T
                            rInfo.EntityType = memberType.GetGenericArguments()[0];
                        }
                        else
                        {
                            throw new ArgumentException(string.Format(
                                "The DataMapper could not determine the RelationshipAttribute EntityType for {0}.{1}",
                                type.Name, memberType.Name));
                        }
                    }
                    else
                    {
                        rInfo.EntityType = memberType;
                    }
                }
                relationships.Add(new Relationship(rInfo, member));
            }

            return relationships;
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
        /// 1) Checks for a converter specified for a specific column.
        /// 2) Checks for a converter registered for the current columns data type.
        /// 3) Checks to see if a converter is registered for all enums (type of Enum) if the current column is an enum.
        /// 4) Checks to see if a converter is registered for all objects (type of Object).
        /// </summary>
        /// <param name="dataMap">The current data map.</param>
        /// <returns>Returns an IConverter object or null if one does not exist.</returns>
        internal IConverter GetConverter(ColumnMap dataMap)
        {
            if (dataMap.ColumnInfo.Converter != ConverterType.None)
            {
                // Column specific converter
                return ConverterFactory.Create(dataMap.ColumnInfo.Converter);
            }
            else
            {
                return GetConverter(dataMap.FieldType);
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