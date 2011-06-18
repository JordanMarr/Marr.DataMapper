using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.QGen;
using System.Linq.Expressions;
using System.Reflection;
using Marr.Data.Mapping;
using System.Data.Common;
using System.Collections;

namespace Marr.Data.QGen
{
    /// <summary>
    /// This class is responsible for building a select query.
    /// It uses chaining methods to provide a fluent interface for creating select queries.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueryBuilder<T> : ExpressionVisitor, IEnumerable<T>
    {
        #region - Private Members -

        private Dialects.Dialect _dialect;
        private DataMapper _db;
        private TableCollection _tables;
        private WhereBuilder<T> _whereBuilder;
        private SortBuilder<T> _sortBuilder;
        private bool _useAltName = false;
        internal string _queryText;
        private List<string> _childrenToLoad;
        private SortBuilder<T> SortBuilder
        {
            get
            {
                // Lazy load
                if (_sortBuilder == null)
                    _sortBuilder = new SortBuilder<T>(this, _dialect, _tables, _useAltName);

                return _sortBuilder;
            }
        }

        #endregion

        #region - Constructor -

        internal QueryBuilder(DataMapper db, Dialects.Dialect dialect)
        {
            _db = db;
            _dialect = dialect;
            _tables = new TableCollection();
            _tables.Add(new Table(typeof(T)));
            _childrenToLoad = new List<string>();
        }

        #endregion

        #region - Fluent Methods -

        /// <summary>
        /// Overrides the table name that will be used in the query.
        /// </summary>
        public QueryBuilder<T> Table(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new DataMappingException("A target table must be passed in or set in a TableAttribute.");

            // Override the base table name
            _tables[0].Name = tableName;
            return this;
        }

        /// <summary>
        /// Allows you to manually specify the query text.
        /// </summary>
        public QueryBuilder<T> QueryText(string queryText)
        {
            _queryText = queryText;
            return this;
        }

        /// <summary>
        /// If no parameters are passed in, this method instructs the DataMapper to load all related entities in the graph.
        /// If specific entities are passed in, only these relationships will be loaded.
        /// </summary>
        /// <param name="childrenToLoad">A list of related child entites to load (passed in as properties / lambda expressions).</param>
        public QueryBuilder<T> Graph(params Expression<Func<T, object>>[] childrenToLoad)
        {
            return Graph(ParseChildrenToLoad(childrenToLoad));
        }

        /// <summary>
        /// If no parameters are passed in, this method instructs the DataMapper to load all related entities in the graph.
        /// If specific entities are passed in, only these relationships will be loaded.
        /// </summary>
        /// <param name="childrenToLoad">A list of related child entites to load (passed in as property names).</param>
        public QueryBuilder<T> Graph(params string[] childrenToLoad)
        {
            EntityGraph graph = new EntityGraph(typeof(T), null);
            TableCollection tablesInView = new TableCollection();

            if (childrenToLoad.Length > 0)
            {
                // Add base table
                tablesInView.Add(_tables[0]);

                // Add user specified child tables
                foreach (string child in childrenToLoad)
                {
                    var node = graph.Where(g => g.Member != null && g.Member.Name == child).FirstOrDefault();
                    if (node != null)
                    {
                        tablesInView.Add(new Table(node.EntityType, JoinType.None));
                    }

                    if (!_childrenToLoad.Contains(child))
                    {
                        _childrenToLoad.Add(child);
                    }
                }
            }
            else
            {
                // Add all tables in the graph
                foreach (var node in graph)
                {
                    tablesInView.Add(new Table(node.EntityType, JoinType.None));
                }
            }

            // Replace the base table with a view with tables
            View view = new View(_tables[0].Name, tablesInView.ToArray());
            _tables.ReplaceBaseTable(view);

            _useAltName = true;
            return this;
        }

        private string[] ParseChildrenToLoad(Expression<Func<T, object>>[] childrenToLoad)
        {
            List<string> entitiesToLoad = new List<string>();

            // Parse relationship member names from expression array
            foreach (var exp in childrenToLoad)
            {
                MemberInfo member = (exp.Body as MemberExpression).Member;
                entitiesToLoad.Add(member.Name);
                
            }

            return entitiesToLoad.ToArray();
        }

        /// <summary>
        /// Allows you to interact with the DbDataReader to manually load entities.
        /// </summary>
        /// <param name="readerAction">An action that takes a DbDataReader.</param>
        public void DataReader(Action<DbDataReader> readerAction)
        {
            if (string.IsNullOrEmpty(_queryText))
                throw new ArgumentNullException("The query text cannot be blank.");

            var mappingHelper = new MappingHelper(_db.Command);
            _db.Command.CommandText = _queryText;

            try
            {
                _db.OpenConnection();
                using (DbDataReader reader = _db.Command.ExecuteReader())
                {
                    readerAction.Invoke(reader);
                }
            }
            finally
            {
                _db.CloseConnection();
            }
        }

        /// <summary>
        /// Executes the query and returns a list of results.
        /// </summary>
        /// <returns>A list of query results of type T.</returns>
        public List<T> ToList()
        {
            // Remember sql mode
            var previousSqlMode = _db.SqlMode;

            BuildQueryOrAppendClauses();

            List<T> results = new List<T>();

            if (_useAltName) // _useAltName is only set to true for graphs
            {
                EntityGraph graph = new EntityGraph(typeof(T), results);
                results = (List<T>)_db.QueryToGraph<T>(_queryText, graph, _childrenToLoad);
            }
            else
            {
                results = (List<T>)_db.Query<T>(_queryText, results);

            }

            // Return to previous sql mode
            _db.SqlMode = previousSqlMode;

            return results;
        }

        private void BuildQueryOrAppendClauses()
        {
            if (_queryText == null)
            {
                // Build entire query
                _db.SqlMode = SqlModes.Text;
                BuildQuery();
            }
            else if (_whereBuilder != null || _sortBuilder != null)
            {
                _db.SqlMode = SqlModes.Text;
                if (_whereBuilder != null)
                {
                    // Append a where clause to an existing query
                    _queryText = string.Concat(_queryText, " ", _whereBuilder.ToString());
                }

                if (_sortBuilder != null)
                {
                    // Append an order clause to an existing query
                    _queryText = string.Concat(_queryText, " ", _sortBuilder.ToString());
                }
            }

        }

        internal void BuildQuery()
        {
            // Generate a query
            string where = _whereBuilder != null ? _whereBuilder.ToString() : string.Empty;
            string sort = SortBuilder.ToString();
            IQuery query = QueryFactory.CreateSelectQuery(_tables, _db, where, sort, _useAltName);
            _queryText = query.Generate();
        }

        #endregion

        #region - Helper Methods -

        private ColumnMapCollection GetColumns(IEnumerable<string> entitiesToLoad)
        {
            // If QueryToGraph<T> and no child load entities are specified, load all children
            bool loadAllChildren = _useAltName && entitiesToLoad == null;

            // If Query<T>
            if (!_useAltName)
            {
                return MapRepository.Instance.GetColumns(typeof(T));
            }

            ColumnMapCollection columns = new ColumnMapCollection();

            Type baseEntityType = typeof(T);
            EntityGraph graph = new EntityGraph(baseEntityType, null);

            foreach (var lvl in graph)
            {
                if (loadAllChildren || lvl.IsRoot || entitiesToLoad.Contains(lvl.Member.Name))
                {
                    columns.AddRange(lvl.Columns);
                }
            }

            return columns;
        }

        public static implicit operator List<T>(QueryBuilder<T> builder)
        {
            return builder.ToList();
        }

        #endregion

        #region - Linq Support -

        public SortBuilder<T> Where<TObj>(Expression<Func<TObj, bool>> filterExpression)
        {
            _whereBuilder = new WhereBuilder<T>(_db.Command, _dialect, filterExpression, _tables, _useAltName, true);
            return SortBuilder;
        }

        public SortBuilder<T> Where(Expression<Func<T, bool>> filterExpression)
        {
            _whereBuilder = new WhereBuilder<T>(_db.Command, _dialect, filterExpression, _tables, _useAltName, true);
            return SortBuilder;
        }

        public SortBuilder<T> OrderBy(Expression<Func<T, object>> sortExpression)
        {
            SortBuilder.OrderBy(sortExpression);
            return SortBuilder;
        }

        public SortBuilder<T> ThenBy(Expression<Func<T, object>> sortExpression)
        {
            SortBuilder.OrderBy(sortExpression);
            return SortBuilder;
        }

        public SortBuilder<T> OrderByDescending(Expression<Func<T, object>> sortExpression)
        {
            SortBuilder.OrderByDescending(sortExpression);
            return SortBuilder;
        }

        public SortBuilder<T> ThenByDescending(Expression<Func<T, object>> sortExpression)
        {
            SortBuilder.OrderByDescending(sortExpression);
            return SortBuilder;
        }

        /// <summary>
        /// Handles all.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected override System.Linq.Expressions.Expression Visit(System.Linq.Expressions.Expression expression)
        {
            return base.Visit(expression);
        }

        /// <summary>
        /// Handles Where.
        /// </summary>
        /// <param name="lambdaExpression"></param>
        /// <returns></returns>
        protected override System.Linq.Expressions.Expression VisitLamda(System.Linq.Expressions.LambdaExpression lambdaExpression)
        {
            _sortBuilder = this.Where(lambdaExpression as Expression<Func<T, bool>>);
            return base.VisitLamda(lambdaExpression);
        }

        /// <summary>
        /// Handles OrderBy.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected override System.Linq.Expressions.Expression VisitMethodCall(MethodCallExpression expression)
        {
            if (expression.Method.Name == "OrderBy" || expression.Method.Name == "ThenBy")
            {
                var memberExp = ((expression.Arguments[1] as UnaryExpression).Operand as System.Linq.Expressions.LambdaExpression).Body as System.Linq.Expressions.MemberExpression;
                _sortBuilder.Order(memberExp.Member);
            }
            if (expression.Method.Name == "OrderByDescending" || expression.Method.Name == "ThenByDescending")
            {
                var memberExp = ((expression.Arguments[1] as UnaryExpression).Operand as System.Linq.Expressions.LambdaExpression).Body as System.Linq.Expressions.MemberExpression;
                _sortBuilder.OrderByDescending(memberExp.Member);
            }

            return base.VisitMethodCall(expression);
        }

        public QueryBuilder<T> Join<TLeft, TRight>(JoinType joinType, Expression<Func<TLeft, TRight, bool>> filterExpression)
        {
            Graph(typeof(TLeft).Name);
            Graph(typeof(TRight).Name);
            Table table = new Table(typeof(TRight), joinType);
            _tables.Add(table);
            var builder = new JoinBuilder<TLeft,TRight>(_db.Command, _dialect, filterExpression, _tables);
            table.JoinClause = builder.ToString();
            return this;
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var list = this.ToList();
            return list.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            var list = this.ToList();
            return list.GetEnumerator();
        }

        #endregion
    }
}
