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
        private MapRepository _repos;
        private IDataMapper _db;

        public MappingHelper(IDataMapper db)
        {
            _repos = MapRepository.Instance;
            _db = db;
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
            object ent = _repos.ReflectionStrategy.CreateInstance(entityType);
            return LoadExistingEntity(mappings, reader, ent, useAltName);
        }

        public object LoadExistingEntity(ColumnMapCollection mappings, DbDataReader reader, object ent, bool useAltName)
        {
            // Populate entity fields from data reader
            foreach (ColumnMap dataMap in mappings)
            {
                try
                {
                    string colName = dataMap.ColumnInfo.GetColumName(useAltName);
                    int ordinal = reader.GetOrdinal(colName);
                    object dbValue = reader.GetValue(ordinal);

                    // Handle data type conversions
                    if (dataMap.Converter != null)
                    {
                        dbValue = dataMap.Converter.FromDB(dataMap, dbValue);
                    }

                    // Handle column conversions (specified during fluent mapping)
                    if (dataMap.FromDB != null)
                    {
                        dbValue = dataMap.FromDB(dbValue);
                    }

                    if (dbValue != DBNull.Value && dataMap.CanWrite)
                    {
                        dataMap.Setter(ent, dbValue);
                    }
                }
                catch (Exception ex)
                {
                    string msg = string.Format("The DataMapper was unable to load the following field: '{0}'.",
                        dataMap.ColumnInfo.Name);

                    throw new DataMappingException(msg, ex);
                }
            }

			LoadEagerLoadedProperties(ent);
            PrepareLazyLoadedProperties(ent);

            return ent;
        }

		private void LoadEagerLoadedProperties(object ent)
		{
			// Load eager loaded properties
			Type entType = ent.GetType();
			if (_repos.Relationships.ContainsKey(entType))
			{
				var relationships = _repos.Relationships[entType];
				foreach (var rel in relationships.Where(r => r.IsEagerLoaded))
				{
					using (var db = new DataMapper(_db.ProviderFactory, _db.ConnectionString))
					{
						// NOTE: If parent _db is in a transaction, new db will be outside of that transaction.
						object eagerLoadedValue = rel.EagerLoaded.Load(db, ent);
						rel.Setter(ent, eagerLoadedValue);
					}
				}
			}
		}

        private void PrepareLazyLoadedProperties(object ent)
        {
            // Handle lazy loaded properties
            Type entType = ent.GetType();
            if (_repos.Relationships.ContainsKey(entType))
            {
                Func<IDataMapper> dbCreate = () =>
                {
                    var db = new DataMapper(_db.ProviderFactory, _db.ConnectionString);
                    db.SqlMode = SqlModes.Text;
                    return db;
                };

                var relationships = _repos.Relationships[entType];
                foreach (var rel in relationships.Where(r => r.IsLazyLoaded))
                {
                    var lazyLoadedProxy = (ILazyLoaded)rel.LazyLoaded.Clone();
                    lazyLoadedProxy.Prepare(dbCreate, ent);
                    rel.Setter(ent, lazyLoadedProxy);
                }
            }
        }

        public T LoadSimpleValueFromFirstColumn<T>(DbDataReader reader)
        {
            try
            {
                return (T)reader.GetValue(0);
            }
            catch (Exception ex)
            {
                string firstColumnName = reader.GetName(0);
                string msg = string.Format("The DataMapper was unable to create a value of type '{0}' from the first column '{1}'.",
                    typeof(T).Name, firstColumnName);

                throw new DataMappingException(msg, ex);
            }
        }

        /// <summary>
        /// Creates all parameters for a SP based on the mappings of the entity,
        /// and assigns them values based on the field values of the entity.
        /// </summary>
        public void CreateParameters<T>(T entity, ColumnMapCollection columnMapCollection, bool isAutoQuery)
        {
            ColumnMapCollection mappings = columnMapCollection;

            if (!isAutoQuery)
            {
                // Order columns (applies to Oracle and OleDb only)
                mappings = columnMapCollection.OrderParameters(_db.Command);
            }

            foreach (ColumnMap columnMap in mappings)
            {
                if (columnMap.ColumnInfo.IsAutoIncrement || !columnMap.CanRead)
                    continue;

                var param = _db.Command.CreateParameter();
                param.ParameterName = columnMap.ColumnInfo.Name;
                param.Size = columnMap.ColumnInfo.Size;
                param.Direction = columnMap.ColumnInfo.ParamDirection;

                object val = columnMap.Getter(entity);

                param.Value = val ?? DBNull.Value; // Convert nulls to DBNulls

                // Handle data type conversions
                if (columnMap.Converter != null)
                {
                    param.Value = columnMap.Converter.ToDB(param.Value);
                }

                // Handle column conversions (specified during fluent mapping)
                if (columnMap.ToDB != null)
                {
                    param.Value = columnMap.ToDB(param.Value);
                }

                // Set the appropriate DbType property depending on the parameter type
                // Note: the columnMap.DBType property was set when the ColumnMap was created
                MapRepository.Instance.DbTypeBuilder.SetDbType(param, columnMap.DBType);

                _db.Command.Parameters.Add(param);
            }
        }

        /// <summary>
        /// Assigns the SP result columns to the passed in 'mappings' fields.
        /// </summary>
        public void SetOutputValues<T>(T entity, IEnumerable<ColumnMap> mappings)
        {
            foreach (ColumnMap dataMap in mappings)
            {
                if (dataMap.CanWrite)
                {
                    object output = _db.Command.Parameters[dataMap.ColumnInfo.Name].Value;
                    dataMap.Setter(entity, output);
                }
            }
        }

        /// <summary>
        /// Assigns the passed in 'value' to the passed in 'mappings' fields.
        /// </summary>
        public void SetOutputValues<T>(T entity, IEnumerable<ColumnMap> mappings, object value)
        {
            foreach (ColumnMap dataMap in mappings)
            {
                if (dataMap.CanWrite)
                {
                    dataMap.Setter(entity, Convert.ChangeType(value, dataMap.FieldType));
                }
            }
        }

    }
}
