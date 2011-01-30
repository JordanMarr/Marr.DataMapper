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
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Collections;
using System.Linq;
using Marr.Data.Mapping;
using Marr.Data.Converters;
using Marr.Data.Parameters;

namespace Marr.Data
{
    public class DataMapper : IDataMapper
    {

        #region - Contructor, Members -

        private DbProviderFactory _dbProviderFactory;
        private string _connectionString;
        private DbCommand _command;

        /// <summary>
        /// Initializes a DataMapper for the given provider type and connection string.
        /// </summary>
        /// <param name="providerName">Ex: </param>
        /// <param name="connectionString">The database connection string.</param>
        public DataMapper(string providerName, string connectionString)
            : this(DbProviderFactories.GetFactory(providerName), connectionString)
        { }

        /// <summary>
        /// A database provider agnostic initialization.
        /// </summary>
        /// <param name="connection">The database connection string.</param>
        public DataMapper(DbProviderFactory dbProviderFactory, string connectionString)
        {
            if (dbProviderFactory == null)
                throw new ArgumentNullException("dbProviderFactory instance cannot be null.");

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString cannot be null or empty.");

            _dbProviderFactory = dbProviderFactory;
            _connectionString = connectionString;
        }

        /// <summary>
        /// Creates a new command utilizing the connection string.
        /// </summary>
        private DbCommand CreateNewCommand()
        {
            DbConnection conn = _dbProviderFactory.CreateConnection();
            conn.ConnectionString = _connectionString;
            DbCommand cmd = conn.CreateCommand();
            SetSqlMode(cmd);
            return cmd;
        }

        /// <summary>
        /// Creates a new command utilizing the connection string with a given SQL command.
        /// </summary>
        private DbCommand CreateNewCommand(string sql)
        {
            DbCommand cmd = CreateNewCommand();
            cmd.CommandText = sql;
            return cmd;
        }

        /// <summary>
        /// Gets or creates a DbCommand object.
        /// </summary>
        public DbCommand Command
        {
            get
            {
                // Lazy load
                if (_command == null)
                    _command = CreateNewCommand();
                else
                    SetSqlMode(_command); // Set SqlMode every time.

                return _command;
            }
        }

        #endregion

        #region - Parameters -

        public DbParameterCollection Parameters
        {
            get
            {
                return Command.Parameters;
            }
        }

        public ParameterChainMethods AddParameter(string name, object value)
        {
            return new ParameterChainMethods(Command, name, value);
        }

        public IDbDataParameter AddParameter(IDbDataParameter parameter)
        {
            // Convert null values to DBNull.Value
            if (parameter.Value == null)
                parameter.Value = DBNull.Value;

            this.Parameters.Add(parameter);
            return parameter;
        }

        #endregion

        #region - SP / SQL Mode -

        private SqlModes _sqlMode = SqlModes.StoredProcedure; // Defaults to SP.
        /// <summary>
        /// Gets or sets a value that determines whether the DataMapper will 
        /// use a stored procedure or a sql text command to access 
        /// the database.  The default is stored procedure.
        /// </summary>
        public SqlModes SqlMode
        {
            get
            {
                return _sqlMode;
            }
            set
            {
                _sqlMode = value;
            }
        }

        /// <summary>
        /// Sets the DbCommand objects CommandType to the current SqlMode.
        /// </summary>
        /// <param name="command">The DbCommand object we are modifying.</param>
        /// <returns>Returns the same DbCommand that was passed in.</returns>
        private DbCommand SetSqlMode(DbCommand command)
        {
            if (SqlMode == SqlModes.StoredProcedure)
                command.CommandType = CommandType.StoredProcedure;
            else
                command.CommandType = CommandType.Text;

            return command;
        }

        #endregion

        #region - Execute Command / Get a Scalar Value -

        /// <summary>
        /// Executes a stored procedure that returns a scalar value.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <returns>A scalar value</returns>
        public object ExecuteScalar(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name is required.");
            else
                Command.CommandText = sql;

            try
            {
                OpenConnection();
                return Command.ExecuteScalar();
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Executes a non query that returns an integer.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <returns>An integer value</returns>
        public int ExecuteNonQuery(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name is required.");
            else
                Command.CommandText = sql;

            try
            {
                OpenConnection();
                return Command.ExecuteNonQuery();
            }
            finally
            {
                CloseConnection();
            }
        }

        #endregion

        #region - DataSets -

        public DataSet GetDataSet(string sql)
        {
            return GetDataSet(sql, new DataSet(), null);
        }

        public DataSet GetDataSet(string sql, DataSet ds, string tableName)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name is required.");

            try
            {
                using (DbDataAdapter adapter = _dbProviderFactory.CreateDataAdapter())
                {
                    Command.CommandText = sql;
                    adapter.SelectCommand = Command;

                    if (ds == null)
                        ds = new DataSet();

                    OpenConnection();

                    if (string.IsNullOrEmpty(tableName))
                        adapter.Fill(ds);
                    else
                        adapter.Fill(ds, tableName);

                    return ds;
                }
            }
            finally
            {
                CloseConnection();  // Clears parameters
            }
        }

        public DataTable GetDataTable(string sql)
        {
            return GetDataTable(sql, null, null);
        }

        public DataTable GetDataTable(string sql, DataTable dt, string tableName)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name is required.");

            try
            {
                using (DbDataAdapter adapter = _dbProviderFactory.CreateDataAdapter())
                {
                    Command.CommandText = sql;
                    adapter.SelectCommand = Command;

                    if (dt == null)
                        dt = new DataTable();

                    adapter.Fill(dt);

                    if (!string.IsNullOrEmpty(tableName))
                        dt.TableName = tableName;

                    return dt;
                }
            }
            finally
            {
                CloseConnection();  // Clears parameters
            }
        }

        public int UpdateDataSet(DataSet ds, string updateSP)
        {
            if (string.IsNullOrEmpty(updateSP))
                throw new ArgumentNullException("A stored procedure name is required.");

            if (ds == null)
                throw new ArgumentNullException("DataSet cannot be null.");

            DbDataAdapter adapter = null;

            try
            {
                adapter = _dbProviderFactory.CreateDataAdapter();

                adapter.UpdateCommand = Command;
                adapter.UpdateCommand.CommandText = updateSP;

                return adapter.Update(ds);
            }
            finally
            {
                if (adapter.UpdateCommand != null)
                    adapter.UpdateCommand.Dispose();

                adapter.Dispose();
            }
        }

        public int InsertDataTable(DataTable table, string insertSP)
        {
            return this.InsertDataTable(table, insertSP, UpdateRowSource.None);
        }

        public int InsertDataTable(DataTable table, string insertSP, UpdateRowSource updateRowSource)
        {
            if (string.IsNullOrEmpty(insertSP))
                throw new ArgumentNullException("A stored procedure name is required.");

            if (table == null)
                throw new ArgumentNullException("DataTable cannot be null.");

            DbDataAdapter adapter = null;

            try
            {
                adapter = _dbProviderFactory.CreateDataAdapter();

                adapter.InsertCommand = Command;
                adapter.InsertCommand.CommandText = insertSP;

                adapter.InsertCommand.UpdatedRowSource = updateRowSource;

                return adapter.Update(table);
            }
            finally
            {
                if (adapter.InsertCommand != null)
                    adapter.InsertCommand.Dispose();

                adapter.Dispose();
            }
        }

        public int DeleteDataTable(DataTable dt, string deleteSP)
        {
            if (string.IsNullOrEmpty(deleteSP))
                throw new ArgumentNullException("A stored procedure name is required.");

            if (dt == null)
                throw new ArgumentNullException("DataSet cannot be null.");

            DbDataAdapter adapter = null;

            try
            {
                adapter = _dbProviderFactory.CreateDataAdapter();

                adapter.DeleteCommand = Command;
                adapter.DeleteCommand.CommandText = deleteSP;

                return adapter.Update(dt);
            }
            finally
            {
                if (adapter.DeleteCommand != null)
                    adapter.DeleteCommand.Dispose();

                adapter.Dispose();
            }
        }

        #endregion

        #region - Find -

        /// <summary>
        /// Runs a query.  Use this overload when you want to manage instantiating and loading an entity.
        /// </summary>
        public void Find(string sql)
        {
            if (LoadEntity == null)
                throw new NullReferenceException("This overload of Find requires a subscriber to the LoadEntity event.");

            // Note: It doesn't matter which generic parameter is passed here.
            Find<string>(sql);
        }

        public T Find<T>(string sql)
        {
            return this.Find<T>(sql, default(T));
        }

        /// <summary>
        /// Returns an entity of type T.
        /// </summary>
        /// <typeparam name="T">The type of entity that is to be instantiated and loaded with values.</typeparam>
        /// <param name="sql">The SQL command to execute.</param>
        /// <returns>An instantiated and loaded entity of type T.</returns>
        public T Find<T>(string sql, T ent)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name has not been specified for 'Find'.");

            Type entityType = typeof(T);
            Command.CommandText = sql;

            MapRepository repository = MapRepository.Instance;
            ColumnMapCollection mappings = repository.GetColumns(entityType);

            try
            {
                OpenConnection();
                using (DbDataReader reader = Command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (LoadEntity != null)
                        {
                            // Delegates the loading task to the caller
                            LoadEntity(this, new LoadEntityEventArgs(reader));
                        }
                        else
                        {
                            if (ent == null)
                                ent = (T)CreateAndLoadEntity<T>(mappings, reader);
                            else
                                LoadExistingEntity(mappings, reader, ent);
                        }
                    }
                }
            }
            finally
            {
                CloseConnection();
            }

            return ent;
        }

        #endregion

        #region - Query -

        /// <summary>
        /// Runs a query.  Use this overload when you want to manage instantiating and adding 
        /// each entity instance to a collection using the LoadEntity event.
        /// </summary>
        public void Query(string sql)
        {
            if (LoadEntity == null)
                throw new NullReferenceException("This overload of Query requires a subscriber to the LoadEntity event.");

            // Note: It doesn't matter which generic parameter is passed here.
            Query<object>(sql);
        }

        /// <summary>
        /// Returns the results of a query.
        /// Uses a List of type T to return the data.
        /// </summary>
        /// <returns>Returns a list of the specified type.</returns>
        public List<T> Query<T>(string sql)
        {
            return (List<T>)Query<T>(sql, new List<T>());
        }

        /// <summary>
        /// Returns the results of a SP query.
        /// </summary>
        /// <returns>Returns a list of the specified type.</returns>
        public ICollection<T> Query<T>(string sql, ICollection<T> entityList)
        {
            if (entityList == null)
                throw new ArgumentNullException("ICollection instance cannot be null.");

            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name has not been specified for 'Query'.");

            Type entityType = typeof(T);
            Command.CommandText = sql;
            ColumnMapCollection mappings = MapRepository.Instance.GetColumns(entityType);

            try
            {
                OpenConnection();
                using (DbDataReader reader = Command.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return entityList;

                    while (reader.Read())
                    {
                        if (LoadEntity != null)
                        {
                            // Delegates the loading task to the caller
                            LoadEntity(this, new LoadEntityEventArgs(reader));
                        }
                        else
                        {
                            // Create new entity
                            object ent = CreateAndLoadEntity<T>(mappings, reader);

                            // Add entity to return list
                            entityList.Add((T)ent);
                        }
                    }
                }
            }
            finally
            {
                CloseConnection();
            }

            return entityList;
        }

        #endregion

        #region - Query View to Object Graph -

        public List<T> QueryViewToObjectGraph<T>(string sql)
        {
            return (List<T>)QueryViewToObjectGraph<T>(sql, new List<T>());
        }

        /// <summary>
        /// Queries a view that joins multiple tables and returns an object graph.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="entityList"></param>
        /// <returns></returns>
        public ICollection<T> QueryViewToObjectGraph<T>(string sql, ICollection<T> entityList)
        {
            if (entityList == null)
                throw new ArgumentNullException("ICollection instance cannot be null.");

            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name has not been specified for 'Query'.");

            Type parentType = typeof(T);
            Command.CommandText = sql;

            try
            {
                EntityGraph entityGraph = new EntityGraph(parentType, (IList)entityList);

                OpenConnection();
                using (DbDataReader reader = Command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // The entire EntityGraph is traversed for each record, 
                        // and multiple entities are created from each view record.
                        foreach (EntityGraph entity in entityGraph)
                        {
                            if (entity.IsNewGroup(reader))
                            {
                                // Add entity to the appropriate place in the object graph
                                entity.AddEntity(CreateAndLoadEntity(entity.EntityType, entity.Columns, reader));
                            }
                        }
                    }
                }
            }
            finally
            {
                CloseConnection();
            }

            return entityList;
        }

        #endregion

        #region - Update -

        public int Update<T>(T entity, string sql)
        {
            if (entity == null)
                throw new ArgumentNullException("The entity cannot be null.");

            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name has not been specified for 'Update'.");

            Command.CommandText = sql;
            ColumnMapCollection mappings = MapRepository.Instance.GetColumns(typeof(T));
            CreateParameters<T>(entity, mappings, false);
            int rowsAffected = 0;

            try
            {
                OpenConnection();
                rowsAffected = Command.ExecuteNonQuery();
                SetOutputValues<T>(entity, mappings.OutputFields);
            }
            finally
            {
                CloseConnection();
            }

            return rowsAffected;
        }

        #endregion

        #region - Insert -

        public int Insert<T>(T entity, string sql)
        {
            if (entity == null)
                throw new ArgumentNullException("The entity cannot be null.");

            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name has not been specified for 'Insert'.");

            Command.CommandText = sql;
            Type entityType = typeof(T);
            ColumnMapCollection mappings = MapRepository.Instance.GetColumns(entityType);
            CreateParameters<T>(entity, mappings.NonReturnValues, true);

            int rowsAffected = 0;

            try
            {
                OpenConnection();
                object returnValue = Command.ExecuteScalar();
                SetOutputValues<T>(entity, mappings.OutputFields);
                SetOutputValues<T>(entity, mappings.ReturnValues, returnValue);
            }
            finally
            {
                CloseConnection();
            }

            return rowsAffected;
        }

        #endregion

        #region - Events -

        public event EventHandler<LoadEntityEventArgs> LoadEntity;

        public event EventHandler OpeningConnection;

        #endregion

        #region - Mapping Helpers -

        /// <summary>
        /// Instantiates an entity and loads its mapped fields with the data from the reader.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mappings"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private object CreateAndLoadEntity<T>(ColumnMapCollection mappings, DbDataReader reader)
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
        protected object CreateAndLoadEntity(Type entityType, ColumnMapCollection mappings, DbDataReader reader)
        {
            // Create new entity
            object ent = ReflectionHelper.CreateInstance(entityType);
            return LoadExistingEntity(mappings, reader, ent);
        }

        protected object LoadExistingEntity(ColumnMapCollection mappings, DbDataReader reader, object ent)
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
        private void CreateParameters<T>(T entity, ColumnMapCollection columnMapCollection, bool isInsert)
        {
            // Order columns (applies to Oracle and OleDb only)
            ColumnMapCollection mappings = columnMapCollection.OrderParameters(Command);

            foreach (ColumnMap columnMap in mappings)
            {
                if (isInsert && columnMap.ColumnInfo.IsAutoIncrement)
                    continue;

                var param = Command.CreateParameter();
                param.ParameterName = columnMap.ColumnInfo.Name;
                param.Size = columnMap.ColumnInfo.Size;
                param.Direction = columnMap.ColumnInfo.ParamDirection;

                object val = ReflectionHelper.GetFieldValue(entity, columnMap.FieldName);

                param.Value = val == null ? DBNull.Value : val; // Convert nulls to DBNulls

                Type fieldType = columnMap.FieldType;

                var repos = MapRepository.Instance;

                IConverter conversion = repos.GetConverter(columnMap);
                if (conversion != null)
                {
                    fieldType = conversion.DbType;
                    param.Value = conversion.ToDB(param.Value);
                }

                repos.DbTypeBuilder.SetDbType(param, columnMap);

                Parameters.Add(param);
            }
        }

        /// <summary>
        /// Assigns the SP result columns to the passed in 'mappings' fields.
        /// </summary>
        private void SetOutputValues<T>(T entity, IEnumerable<ColumnMap> mappings)
        {
            foreach (ColumnMap dataMap in mappings)
            {
                object output = Parameters[dataMap.ColumnInfo.Name].Value;
                ReflectionHelper.SetFieldValue(entity, dataMap.FieldName, output);
            }
        }

        /// <summary>
        /// Assigns the passed in 'value' to the passed in 'mappings' fields.
        /// </summary>
        private void SetOutputValues<T>(T entity, IEnumerable<ColumnMap> mappings, object value)
        {
            foreach (ColumnMap dataMap in mappings)
            {
                ReflectionHelper.SetFieldValue(entity, dataMap.FieldName, value);
            }
        }
        
        #endregion

        #region - Connections / Transactions -

        protected virtual void OnOpeningConnection()
        {
            if (OpeningConnection != null)
                OpeningConnection(this, EventArgs.Empty);
        }

        protected void OpenConnection()
        {
            OnOpeningConnection();

            if (Command.Connection.State != ConnectionState.Open)
                Command.Connection.Open();
        }

        protected void CloseConnection()
        {
            this.Parameters.Clear();
            Command.CommandText = string.Empty;

            if (Command.Transaction == null)
                Command.Connection.Close(); // Only close if no transaction is present

            if (LoadEntity != null)
                LoadEntity = null;
        }

        public void BeginTransaction()
        {
            OpenConnection();
            DbTransaction trans = Command.Connection.BeginTransaction();
            Command.Transaction = trans;
        }

        public void RollBack()
        {
            try
            {
                if (Command.Transaction != null)
                    Command.Transaction.Rollback();
            }
            finally
            {
                Command.Connection.Close();
            }
        }

        public void Commit()
        {
            try
            {
                if (Command.Transaction != null)
                    Command.Transaction.Commit();
            }
            finally
            {
                Command.Connection.Close();
            }
        }

        #endregion

        #region - IDisposable Members -

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // In case a derived class implements a finalizer
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Command.Transaction != null)
                {
                    Command.Transaction.Dispose();
                    Command.Transaction = null;
                }

                if (Command.Connection != null)
                {
                    Command.Connection.Dispose();
                    Command.Connection = null;
                }

                if (Command != null)
                {
                    Command.Dispose();
                    _command = null;
                }
            }
        }

        #endregion

    }
}