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
	public class AutoQueryBuilder<T> : ExpressionVisitor, IEnumerable<T>
    {
        #region - AutoQueryBuilder -

        private DataMapper _db;
        private WhereBuilder<T> _whereBuilder;
        private SortBuilder<T> _sortBuilder;
        private string _tableName;
        private bool _useAltName;
        private bool _isGraph;
        
        public List<QueryQueueItem> QueryQueue { get; private set; }

        internal AutoQueryBuilder(DataMapper db, bool isGraph)
            : this(db, null, isGraph)
        { }

        internal AutoQueryBuilder(DataMapper db, string tableName, bool isGraph)
        {
            QueryQueue = new List<QueryQueueItem>();
            _db = db;
            _tableName = tableName ?? MapRepository.Instance.GetTableName(typeof(T));
            if (string.IsNullOrEmpty(_tableName))
                throw new DataMappingException("A target table must be passed in or set in a TableAttribute.");

            _useAltName = isGraph;
            _isGraph = isGraph;
            _sortBuilder = new SortBuilder<T>(this, _useAltName);
        }

        public AutoQueryBuilder<T> Load(params Expression<Func<T, object>>[] childrenToLoad)
        {
            return Load(null, childrenToLoad);
        }

        public AutoQueryBuilder<T> Load(string queryText, params Expression<Func<T, object>>[] childrenToLoad)
        {
            List<string> entitiesToLoad = new List<string>();

            // Parse relationship member names from expression array
            foreach (var exp in childrenToLoad)
            {
                entitiesToLoad.Add((exp.Body as MemberExpression).Member.Name);
            }

            // Add query path
            if (entitiesToLoad.Count > 0)
            {
                QueryQueue.Add(new QueryQueueItem(queryText, entitiesToLoad));
            }

            return this;
        }

        public SortBuilder<T> Where(Expression<Func<T, bool>> filterExpression)
        {
            _whereBuilder = new WhereBuilder<T>(_db.Command, filterExpression, _useAltName);
            return _sortBuilder;
        }

        public SortBuilder<T> OrderBy(Expression<Func<T, object>> sortExpression)
        {
            _sortBuilder.OrderBy(sortExpression);
            return _sortBuilder;
        }

        public SortBuilder<T> ThenBy(Expression<Func<T, object>> sortExpression)
        {
            _sortBuilder.OrderBy(sortExpression);
            return _sortBuilder;
        }

        public SortBuilder<T> OrderByDescending(Expression<Func<T, object>> sortExpression)
        {
            _sortBuilder.OrderByDescending(sortExpression);
            return _sortBuilder;
        }

        public SortBuilder<T> ThenByDescending(Expression<Func<T, object>> sortExpression)
        {
            _sortBuilder.OrderByDescending(sortExpression);
            return _sortBuilder;
        }

        public List<T> ToList()
        {
            // Remember sql mode
            var previousSqlMode = _db.SqlMode;
            _db.SqlMode = SqlModes.Text;

            List<T> results = new List<T>();

            EntityGraph graph = new EntityGraph(typeof(T), results);

            GenerateQueries();

            try
            {
                if (_isGraph)
                {
                    _db.OpenConnection();
                    foreach (QueryQueueItem queueItem in QueryQueue)
                    {
                        results = (List<T>)_db.QueryToGraph<T>(queueItem.QueryText, graph, queueItem == null ? null : queueItem.EntitiesToLoad);
                    }
                }
                else
                {
                    string query = QueryQueue.First().QueryText;
                    results = (List<T>)_db.Query(query, results);
                }
            }
            finally
            {
                _db.CloseConnection();
            }

            // Return to previous sql mode
            _db.SqlMode = previousSqlMode;

            return results;
        }

        internal void GenerateQueries()
        {
            if (QueryQueue.Count == 0)
                QueryQueue.Add(new QueryQueueItem(null, null));

            foreach (var queueItem in QueryQueue)
            {
                if (queueItem.QueryText == null)
                {
                    // Generate a parameterized where clause
                    var columns = GetColumns(queueItem.EntitiesToLoad);
                    string where = _whereBuilder != null ? _whereBuilder.ToString() : string.Empty;
                    string sort = _sortBuilder.ToString();
                    IQuery query = QueryFactory.CreateSelectQuery(columns, _tableName, where, sort, _useAltName);
                    queueItem.QueryText = query.Generate();
                }
            }
        }
        
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
        
        public static implicit operator List<T>(AutoQueryBuilder<T> builder)
        {
            return builder.ToList();
        }

        #endregion

        #region - Query<T> -

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
