using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using System.Linq.Expressions;

namespace Marr.Data.QGen
{
    public class InsertQueryBuilder<T> : IQueryBuilder
    {
        private DataMapper _db;
        private string _tableName;
        private T _entity;
        private MappingHelper _mappingHelper;
        private ColumnMapCollection _mappings;
        private SqlModes _previousSqlMode;
        private bool _generateQuery = true;
        private bool _getIdentityValue;
        private Dialects.Dialect _dialect;
        private ColumnMapCollection _columnsToInsert;

        public InsertQueryBuilder(DataMapper db)
        {
            _db = db;
            _tableName = MapRepository.Instance.GetTableName(typeof(T));
            _previousSqlMode = _db.SqlMode;
            _mappingHelper = new MappingHelper(_db.Command);
            _mappings = MapRepository.Instance.GetColumns(typeof(T));
            _dialect = QueryFactory.CreateDialect(_db);
        }

        public InsertQueryBuilder<T> TableName(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        public InsertQueryBuilder<T> QueryText(string queryText)
        {
            _generateQuery = false;
            _db.Command.CommandText = queryText;
            return this;
        }

        public InsertQueryBuilder<T> Entity(T entity)
        {
            _entity = entity;
            return this;
        }

        /// <summary>
        /// Runs an identity query to get the value of an autoincrement field.
        /// Note: this does not run when a query has been manually passed in.
        /// </summary>
        /// <returns></returns>
        public InsertQueryBuilder<T> GetIdentity()
        {
            if (string.IsNullOrEmpty(_dialect.IdentityQuery))
            {
                string err = string.Format("The current dialect '{0}' does not have an identity query implemented.", _dialect.ToString());
                throw new DataMappingException(err);
            }

            _getIdentityValue = true;
            return this;
        }

        public InsertQueryBuilder<T> ColumnsIncluding(params Expression<Func<T, object>>[] properties)
        {
            List<string> columnList = new List<string>();

            foreach (var column in properties)
            {
                columnList.Add(column.GetMemberName());
            }

            return ColumnsIncluding(columnList.ToArray());
        }

        public InsertQueryBuilder<T> ColumnsIncluding(params string[] properties)
        {
            _columnsToInsert = new ColumnMapCollection();

            foreach (string propertyName in properties)
            {
                _columnsToInsert.Add(_mappings.GetByFieldName(propertyName));
            }

            return this;
        }

        public InsertQueryBuilder<T> ColumnsExcluding(params Expression<Func<T, object>>[] properties)
        {
            List<string> columnList = new List<string>();

            foreach (var column in properties)
            {
                columnList.Add(column.GetMemberName());
            }

            return ColumnsExcluding(columnList.ToArray());
        }

        public InsertQueryBuilder<T> ColumnsExcluding(params string[] properties)
        {
            _columnsToInsert = new ColumnMapCollection();

            _columnsToInsert.AddRange(_mappings);

            foreach (string propertyName in properties)
            {
                _columnsToInsert.RemoveAll(c => c.FieldName == propertyName);
            }

            return this;
        }

        public string BuildQuery()
        {
            if (_entity == null)
                throw new ArgumentNullException("You must specify an entity to insert.");

            // Override SqlMode since we know this will be a text query
            _db.SqlMode = SqlModes.Text;

            var columns = _columnsToInsert ?? _mappings;

            _mappingHelper.CreateParameters<T>(_entity, columns, _generateQuery);
            IQuery query = QueryFactory.CreateInsertQuery(columns, _db, _tableName);

            _db.Command.CommandText = query.Generate();

            if (_getIdentityValue && _dialect.SupportsBatchQueries)
            {
                // Append a batched identity query
                if (!_db.Command.CommandText.EndsWith(";"))
                {
                    _db.Command.CommandText += ";";
                }
                _db.Command.CommandText += _dialect.IdentityQuery;
            }

            return _db.Command.CommandText;
        }

        public object Execute()
        {
            if (_generateQuery)
            {
                BuildQuery();
            }
            else
            {
                _mappingHelper.CreateParameters<T>(_entity, _mappings.NonReturnValues, _generateQuery);
            }

            object scalar = null;

            try
            {
                _db.OpenConnection();
                scalar = _db.Command.ExecuteScalar();
                if (_generateQuery && !_dialect.SupportsBatchQueries)
                {
                    // Run identity query as a separate query
                    _db.Command.CommandText = _dialect.IdentityQuery;
                    scalar = _db.Command.ExecuteScalar();
                }
                _mappingHelper.SetOutputValues<T>(_entity, _mappings.OutputFields);
                if (scalar != null)
                {
                    _mappingHelper.SetOutputValues<T>(_entity, _mappings.ReturnValues, scalar);
                }
            }
            finally
            {
                _db.CloseConnection();
            }

            
            if (_generateQuery)
            {
                // Return to previous sql mode
                _db.SqlMode = _previousSqlMode;
            }

            return scalar;
        }

    }
}
