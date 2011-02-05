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
        /// <typeparam name="T"></typeparam>
        /// <param name="mappings"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public object CreateAndLoadEntity<T>(ColumnMapCollection mappings, DbDataReader reader)
        {
            return CreateAndLoadEntity(typeof(T), mappings, reader);
        }

        /// <summary>
        /// Instantiates an entity and loads its mapped fields with the data from the reader.
        /// </summary>
        /// <param name="entityType">The entity being created and loaded.</param>
        /// <param name="mappings">The field mappings for the passed in entity.</param>
        /// <param name="reader">The open data reader.</param>
        /// <returns>Returns an entity loaded with data.</returns>
        public object CreateAndLoadEntity(Type entityType, ColumnMapCollection mappings, DbDataReader reader)
        {
            // Create new entity
            object ent = ReflectionHelper.CreateInstance(entityType);
            return LoadExistingEntity(mappings, reader, ent);
        }

        public object LoadExistingEntity(ColumnMapCollection mappings, DbDataReader reader, object ent)
        {
            List<ColumnMap> mappingsToRemove = new List<ColumnMap>();

            MapRepository repository = MapRepository.Instance;

            // Populate entity fields from data reader
            foreach (ColumnMap dataMap in mappings)
            {
                try
                {
                    int ordinal = reader.GetOrdinal(dataMap.ColumnInfo.Name);
                    object dbValue = reader.GetValue(ordinal);

                    // Handle conversions
                    IConverter conversion = repository.GetConverter(dataMap);
                    if (conversion != null)
                    {
                        dbValue = conversion.FromDB(dataMap, dbValue);
                    }

                    ReflectionHelper.SetFieldValue(ent, dataMap.FieldName, dbValue);
                }
                catch (Exception ex)
                {
                    if (dataMap.ColumnInfo is OptionalColumn)
                    {
                        // Mark the missing mapping for removal and continue
                        mappingsToRemove.Add(dataMap);
                        continue;
                    }
                    else
                    {
                        string msg = string.Format("The DataMapper was unable to load the following field: '{0}'.",
                            dataMap.ColumnInfo.Name);
                        throw new Exception(msg, ex);
                    }
                }
            }

            // Modify cache to remove optional mappings that were not found
            foreach (ColumnMap missingMap in mappingsToRemove)
            {
                mappings.Remove(missingMap);
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

                IConverter conversion = repos.GetConverter(columnMap);
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
                ReflectionHelper.SetFieldValue(entity, dataMap.FieldName, value);
            }
        }

    }
}
