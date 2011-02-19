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
using Marr.Data.QGen;
using System.Linq.Expressions;

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
                var mappingHelper = new MappingHelper(Command);

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
                                ent = (T)mappingHelper.CreateAndLoadEntity<T>(mappings, reader, false);
                            else
                                mappingHelper.LoadExistingEntity(mappings, reader, ent, false);
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

        public AutoQueryBuilder<T> AutoQuery<T>(string target)
        {
            return new AutoQueryBuilder<T>(this, target, Query<T>);
        }

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

            var mappingHelper = new MappingHelper(Command);
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
                            object ent = mappingHelper.CreateAndLoadEntity<T>(mappings, reader, false);

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

        #region - Query to Graph -

        public AutoQueryBuilder<T> AutoQueryToGraph<T>(string target)
        {
            return new AutoQueryBuilder<T>(this, target, QueryToGraph<T>);
        }

        public List<T> QueryToGraph<T>(string sql)
        {
            return (List<T>)QueryToGraph<T>(sql, new List<T>());
        }

        /// <summary>
        /// Queries a view that joins multiple tables and returns an object graph.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="entityList"></param>
        /// <returns></returns>
        public ICollection<T> QueryToGraph<T>(string sql, ICollection<T> entityList)
        {
            if (entityList == null)
                throw new ArgumentNullException("entityList");

            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("sql");

            var mappingHelper = new MappingHelper(Command);
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
                        foreach (EntityGraph lvl in entityGraph)
                        {
                            if (lvl.IsNewGroup(reader))
                            {
                                var newEntity = mappingHelper.CreateAndLoadEntity(lvl.EntityType, lvl.Columns, reader, true);

                                // Add entity to the appropriate place in the object graph
                                lvl.AddEntity(newEntity);
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

        public int AutoUpdate<T>(T entity, string target)
        {
            return AutoUpdate<T>(entity, target, null);
        }

        public int AutoUpdate<T>(T entity, string target, Expression<Func<T, bool>> filter)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            if (string.IsNullOrEmpty(target))
                throw new ArgumentNullException("target");

            // Remember sql mode
            var previousSqlMode = this.SqlMode;
            SqlMode = SqlModes.Text;

            var mappingHelper = new MappingHelper(Command);
            var where = new WhereBuilder<T>(Command, filter, false);
            ColumnMapCollection mappings = MapRepository.Instance.GetColumns(typeof(T));
            mappingHelper.CreateParameters<T>(entity, mappings, false, true);
            IQuery query = QueryFactory.CreateUpdateQuery(mappings, Command, target, where.ToString());
            Command.CommandText = query.Generate();

            int rowsAffected = 0;

            try
            {
                OpenConnection();
                rowsAffected = Command.ExecuteNonQuery();
                mappingHelper.SetOutputValues<T>(entity, mappings.OutputFields);
            }
            finally
            {
                CloseConnection();
            }

            // Return to previous sql mode
            SqlMode = previousSqlMode;

            return rowsAffected;
        }

        public int Update<T>(T entity, string sql)
        {
            if (entity == null)
                throw new ArgumentNullException("The entity cannot be null.");

            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name has not been specified for 'Update'.");

            var mappingHelper = new MappingHelper(Command);
            Command.CommandText = sql;
            ColumnMapCollection mappings = MapRepository.Instance.GetColumns(typeof(T));
            mappingHelper.CreateParameters<T>(entity, mappings, false, false);
            int rowsAffected = 0;

            try
            {
                OpenConnection();
                rowsAffected = Command.ExecuteNonQuery();
                mappingHelper.SetOutputValues<T>(entity, mappings.OutputFields);
            }
            finally
            {
                CloseConnection();
            }

            return rowsAffected;
        }

        #endregion

        #region - Insert -

        public int AutoInsert<T>(T entity, string target)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            if (string.IsNullOrEmpty(target))
                throw new ArgumentNullException("target");

            // Remember sql mode
            var previousSqlMode = this.SqlMode;
            SqlMode = SqlModes.Text;

            var mappingHelper = new MappingHelper(Command);
            ColumnMapCollection mappings = MapRepository.Instance.GetColumns(typeof(T));
            mappingHelper.CreateParameters<T>(entity, mappings, false, true);
            IQuery query = QueryFactory.CreateInsertQuery(mappings, Command, target);
            Command.CommandText = query.Generate();

            int rowsAffected = 0;

            try
            {
                OpenConnection();
                object returnValue = Command.ExecuteScalar();
                mappingHelper.SetOutputValues<T>(entity, mappings.OutputFields);
                mappingHelper.SetOutputValues<T>(entity, mappings.ReturnValues, returnValue);
            }
            finally
            {
                CloseConnection();
            }

            // Return to previous sql mode
            SqlMode = previousSqlMode;

            return rowsAffected;
        }

        public int Insert<T>(T entity, string sql)
        {
            if (entity == null)
                throw new ArgumentNullException("The entity cannot be null.");

            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("A stored procedure name has not been specified for 'Insert'.");

            var mappingHelper = new MappingHelper(Command);
            Command.CommandText = sql;
            Type entityType = typeof(T);
            ColumnMapCollection mappings = MapRepository.Instance.GetColumns(entityType);
            mappingHelper.CreateParameters<T>(entity, mappings.NonReturnValues, true, false);

            int rowsAffected = 0;

            try
            {
                OpenConnection();
                object returnValue = Command.ExecuteScalar();
                mappingHelper.SetOutputValues<T>(entity, mappings.OutputFields);
                mappingHelper.SetOutputValues<T>(entity, mappings.ReturnValues, returnValue);
            }
            finally
            {
                CloseConnection();
            }

            return rowsAffected;
        }

        #endregion

        #region - Delete -

        public int AutoDelete<T>(T entity, string target)
        {
            return AutoDelete<T>(entity, target, null);
        }

        public int AutoDelete<T>(T entity, string target, Expression<Func<T, bool>> filter)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            if (string.IsNullOrEmpty(target))
                throw new ArgumentNullException("target");

            // Remember sql mode
            var previousSqlMode = this.SqlMode;
            SqlMode = SqlModes.Text;

            var mappingHelper = new MappingHelper(Command);
            var where = new WhereBuilder<T>(Command, filter, false);
            IQuery query = QueryFactory.CreateDeleteQuery(target, where.ToString());
            Command.CommandText = query.Generate();

            int rowsAffected = 0;

            try
            {
                OpenConnection();
                rowsAffected = Command.ExecuteNonQuery();
            }
            finally
            {
                CloseConnection();
            }

            // Return to previous sql mode
            SqlMode = previousSqlMode;

            return rowsAffected;
        }

        #endregion

        #region - Events -

        public event EventHandler<LoadEntityEventArgs> LoadEntity;

        public event EventHandler OpeningConnection;

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