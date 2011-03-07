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
    public class QueryBuilder<T> : ExpressionVisitor, IEnumerable<T>
    {
        #region - Private Members -

        private DataMapper _db;
        private WhereBuilder<T> _whereBuilder;
        private SortBuilder<T> _sortBuilder;
        private string _tableName;
        private bool _useAltName = false;
        internal string _queryText;
        private string[] _childrenToLoad;
        private SortBuilder<T> SortBuilder
        {
            get
            {
                // Lazy load
                if (_sortBuilder == null)
                    _sortBuilder = new SortBuilder<T>(this, _useAltName);

                return _sortBuilder;
            }
        }

        #endregion

        #region - Constructor -

        internal QueryBuilder(DataMapper db)
        {
            _db = db;
        }

        #endregion

        #region - Fluent Methods -

        public QueryBuilder<T> Table(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new DataMappingException("A target table must be passed in or set in a TableAttribute.");

            _tableName = tableName;
            return this;
        }

        public QueryBuilder<T> QueryText(string queryText)
        {
            _queryText = queryText;
            return this;
        }

        public QueryBuilder<T> Graph(params Expression<Func<T, object>>[] childrenToLoad)
        {
            return Graph(ParseChildrenToLoad(childrenToLoad));
        }

        public QueryBuilder<T> Graph(params string[] childrenToLoad)
        {
            if (childrenToLoad.Length > 0)
                _childrenToLoad = childrenToLoad;
            else
                _childrenToLoad = null;

            _useAltName = true;
            return this;
        }

        private string[] ParseChildrenToLoad(Expression<Func<T, object>>[] childrenToLoad)
        {
            List<string> entitiesToLoad = new List<string>();

            // Parse relationship member names from expression array
            foreach (var exp in childrenToLoad)
            {
                entitiesToLoad.Add((exp.Body as MemberExpression).Member.Name);
            }

            return entitiesToLoad.ToArray();
        }

        public List<T> ToList()
        {
            // Remember sql mode
            var previousSqlMode = _db.SqlMode;

            List<T> results = new List<T>();

            EntityGraph graph = new EntityGraph(typeof(T), results);

            if (_queryText == null)
            {
                _db.SqlMode = SqlModes.Text;
                BuildQuery();
            }

            if (_useAltName) // _useAltName is only set to true for graphs
            {
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

        internal void BuildQuery()
        {
            if (_tableName == null)
                _tableName = MapRepository.Instance.GetTableName(typeof(T));

            if (string.IsNullOrEmpty(_tableName))
                throw new DataMappingException("A target table must be passed in or set in a TableAttribute.");

            // Generate a query
            var columns = GetColumns(_childrenToLoad);
            string where = _whereBuilder != null ? _whereBuilder.ToString() : string.Empty;
            string sort = SortBuilder.ToString();
            IQuery query = QueryFactory.CreateSelectQuery(columns, _tableName, where, sort, _useAltName);
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

        public SortBuilder<T> Where(Expression<Func<T, bool>> filterExpression)
        {
            _whereBuilder = new WhereBuilder<T>(_db.Command, filterExpression, _useAltName);
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
