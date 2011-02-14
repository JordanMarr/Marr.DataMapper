using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace Marr.Data.QGen
{
    public class AutoQueryBuilder<T>
    {
        private IDataMapper _db;
        private string _target;
        private WhereBuilder<T> _whereBuilder;
        private SortBuilder<T> _sortBuilder;
        private Func<string, List<T>> _runQueryMethod;

        public AutoQueryBuilder(IDataMapper db, string target, Func<string, List<T>> runQueryMethod)
        {
            _db = db;
            _target = target;
            _sortBuilder = new SortBuilder<T>(this);
            _runQueryMethod = runQueryMethod;
        }

        public SortBuilder<T> Where(Expression<Func<T, bool>> filterExpression)
        {
            _whereBuilder = new WhereBuilder<T>(_db.Command, filterExpression);
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
            var columns = MapRepository.Instance.GetColumns(typeof(T));
            string where = _whereBuilder != null ? _whereBuilder.ToString() : string.Empty;
            string sort = _sortBuilder.ToString();
            IQuery query = QueryFactory.CreateSelectQuery(columns, _target, where, sort);
            var results = _runQueryMethod(query.Generate());

            // Return to previous sql mode
            _db.SqlMode = previousSqlMode;

            return results;
        }

        public static implicit operator List<T>(AutoQueryBuilder<T> builder)
        {
            return builder.ToList();
        }
    }
}
