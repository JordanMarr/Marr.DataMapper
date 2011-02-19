using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Marr.Data.Converters;

namespace Marr.Data.Mapping
{
    internal class MappingHelper
    {
        private DbCommand _command;

        public MappingHelper(DbCommand command)
        {
            _command = command;
        }

        /// <summary>
        /// Instantiates an entity and loads its mapped fields with the data from the reader.
        /// </summary>
        public object CreateAndLoadEntity<T>(ColumnMapCollection mappings, DbDataReader reader, bool useAltName)
        {
            return CreateAndLoadEntity(typeof(T), mappings, reader, useAltName);
        }

        /// <summary>
        /// Instantiates an entity and loads its mapped fields with the data from the reader.
        /// </summary>
        /// <param name="entityType">The entity being created and loaded.</param>
        /// <param name="mappings">The field mappings for the passed in entity.</param>
        /// <param name="reader">The open data reader.</param>
        /// <param name="useAltNames">Determines if the column AltName should be used.</param>
        /// <returns>Returns an entity loaded with data.</returns>
        public object CreateAndLoadEntity(Type entityType, ColumnMapCollection mappings, DbDataReader reader, bool useAltName)
        {
            // Create new entity
            object ent = ReflectionHelper.CreateInstance(entityType);
            return LoadExistingEntity(mappings, reader, ent, useAltName);
        }

        public object LoadExistingEntity(ColumnMapCollection mappings, DbDataReader reader, object ent, bool useAltName)
        {
            MapRepository repository = MapRepository.Instance;

            // Populate entity fields from data reader
            foreach (ColumnMap dataMap in mappings)
            {
                try
                {
                    string colName = dataMap.ColumnInfo.GetColumName(useAltName);
                    int ordinal = reader.GetOrdinal(colName);
                    object dbValue = reader.GetValue(ordinal);

                    // Handle conversions
                    IConverter conversion = repository.GetConverter(dataMap.FieldType);
                    if (conversion != null)
                    {
                        dbValue = conversion.FromDB(dataMap, dbValue);
                    }

                    ReflectionHelper.SetFieldValue(ent, dataMap.FieldName, dbValue);
                }
                catch (Exception ex)
                {
                    string msg = string.Format("The DataMapper was unable to load the following field: '{0}'.",
                        dataMap.ColumnInfo.Name);
                    throw new DataMappingException(msg, ex);
                }
            }
            
            return ent;
        }

        /// <summary>
        /// Creates all parameters for a SP based on the mappings of the entity,
        /// and assigns them values based on the field values of the entity.
        /// </summary>
        public void CreateParameters<T>(T entity, ColumnMapCollection columnMapCollection, bool isInsert, bool isAutoQuery)
        {
            ColumnMapCollection mappings = columnMapCollection;

            if (!isAutoQuery)
            {
                // Order columns (applies to Oracle and OleDb only)
                mappings = columnMapCollection.OrderParameters(_command);
            }

            foreach (ColumnMap columnMap in mappings)
            {
                if (isInsert && columnMap.ColumnInfo.IsAutoIncrement)
                    continue;

                var param = _command.CreateParameter();
                param.ParameterName = columnMap.ColumnInfo.Name;
                param.Size = columnMap.ColumnInfo.Size;
                param.Direction = columnMap.ColumnInfo.ParamDirection;

                object val = ReflectionHelper.GetFieldValue(entity, columnMap.FieldName);

                param.Value = val == null ? DBNull.Value : val; // Convert nulls to DBNulls

                var repos = MapRepository.Instance;

                IConverter conversion = repos.GetConverter(columnMap.FieldType);
                if (conversion != null)
                {
                    param.Value = conversion.ToDB(param.Value);
                }

                // Set the appropriate DbType property depending on the parameter type
                // Note: the columnMap.DBType property was set when the ColumnMap was created
                repos.DbTypeBuilder.SetDbType(param, columnMap.DBType);

                _command.Parameters.Add(param);
            }
        }

        /// <summary>
        /// Assigns the SP result columns to the passed in 'mappings' fields.
        /// </summary>
        public void SetOutputValues<T>(T entity, IEnumerable<ColumnMap> mappings)
        {
            foreach (ColumnMap dataMap in mappings)
            {
                object output = _command.Parameters[dataMap.ColumnInfo.Name].Value;
                ReflectionHelper.SetFieldValue(entity, dataMap.FieldName, output);
            }
        }

        /// <summary>
        /// Assigns the passed in 'value' to the passed in 'mappings' fields.
        /// </summary>
        public void SetOutputValues<T>(T entity, IEnumerable<ColumnMap> mappings, object value)
        {
            foreach (ColumnMap dataMap in mappings)
            {
                ReflectionHelper.SetFieldValue(entity, dataMap.FieldName, Convert.ChangeType(value, dataMap.FieldType));
            }
        }

    }
}
