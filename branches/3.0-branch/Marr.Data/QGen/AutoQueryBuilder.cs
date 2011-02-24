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
	public class AutoQueryBuilder<T>
    {
        private DataMapper _db;
        private string _target;
        private WhereBuilder<T> _whereBuilder;
        private SortBuilder<T> _sortBuilder;
        private bool _useAltName;
        private bool _isGraph;
        
        public List<QueryQueueItem> QueryQueue { get; private set; }

        internal AutoQueryBuilder(DataMapper db, string target, bool isGraph)
        {
            QueryQueue = new List<QueryQueueItem>();
            _db = db;
            _target = target;
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

        public SortBuilder<T> Order(Expression<Func<T, object>> sortExpression)
        {
            _sortBuilder.Order(sortExpression);
            return _sortBuilder;
        }

        public SortBuilder<T> OrderDesc(Expression<Func<T, object>> sortExpression)
        {
            _sortBuilder.OrderDesc(sortExpression);
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
                    IQuery query = QueryFactory.CreateSelectQuery(columns, _target, where, sort, _useAltName);
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
    }

    public class QueryQueueItem
    {
        public QueryQueueItem(string queryText, IEnumerable<string> entitiesToLoad)
        {
            QueryText = queryText;
            EntitiesToLoad = entitiesToLoad;
        }

        public string QueryText { get; set; }
        public IEnumerable<string> EntitiesToLoad { get; private set; }
    }

}
