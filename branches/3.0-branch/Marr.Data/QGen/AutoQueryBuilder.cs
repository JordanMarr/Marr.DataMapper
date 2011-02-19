using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Marr.Data.Mapping;

namespace Marr.Data.QGen
{
    public class AutoQueryBuilder<T>
    {
        private IDataMapper _db;
        private string _target;
        private WhereBuilder<T> _whereBuilder;
        private SortBuilder<T> _sortBuilder;
        private Func<string, List<T>> _runQueryMethod;
        private bool _useAltName;

        public AutoQueryBuilder(IDataMapper db, string target, Func<string, List<T>> runQueryMethod)
        {
            _db = db;
            _target = target;

            // Only use alt name if querying a graph
            _useAltName = runQueryMethod == db.QueryToGraph<T>;

            _sortBuilder = new SortBuilder<T>(this, _useAltName);
            _runQueryMethod = runQueryMethod;
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

            // Generate a parameterized where clause
            var columns = GetColumns(typeof(T), _useAltName);
            string where = _whereBuilder != null ? _whereBuilder.ToString() : string.Empty;
            string sort = _sortBuilder.ToString();
            IQuery query = QueryFactory.CreateSelectQuery(columns, _target, where, sort, _useAltName);
            var results = _runQueryMethod(query.Generate());

            // Return to previous sql mode
            _db.SqlMode = previousSqlMode;

            return results;
        }

        private ColumnMapCollection GetColumns(Type entityType, bool loadRelationshipColumns)
        {
            if (loadRelationshipColumns)
            {
                ColumnMapCollection columns = new ColumnMapCollection();

                EntityGraph graph = new EntityGraph(entityType, null);
                foreach (var entity in graph)
                {
                    columns.AddRange(entity.Columns);
                }

                return columns;
            }
            else
            {
                return MapRepository.Instance.GetColumns(entityType);
            }
        }

        public static implicit operator List<T>(AutoQueryBuilder<T> builder)
        {
            return builder.ToList();
        }
    }
}
