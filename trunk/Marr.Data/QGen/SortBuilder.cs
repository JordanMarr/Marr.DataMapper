using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Marr.Data.QGen.Dialects;

namespace Marr.Data.QGen
{
    public class SortBuilder<T> : IEnumerable<T>
    {
        private QueryBuilder<T> _baseBuilder;
        private Dialect _dialect;
        private List<SortColumn<T>> _sortExpressions;
        private bool _useAltName;

        public SortBuilder(QueryBuilder<T> baseBuilder, Dialect dialect, bool useAltName)
        {
            _baseBuilder = baseBuilder;
            _dialect = dialect;
            _sortExpressions = new List<SortColumn<T>>();
            _useAltName = useAltName;
        }

        internal SortBuilder<T> Order(MemberInfo member)
        {
            _sortExpressions.Add(new SortColumn<T>(member, SortDirection.Asc));
            return this;
        }

        internal SortBuilder<T> OrderByDescending(MemberInfo member)
        {
            _sortExpressions.Add(new SortColumn<T>(member, SortDirection.Desc));
            return this;
        }

        public SortBuilder<T> OrderBy(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Asc));
            return this;
        }

        public SortBuilder<T> OrderByDescending(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Desc));
            return this;
        }

        public SortBuilder<T> ThenBy(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Asc));
            return this;
        }

        public SortBuilder<T> ThenByDescending(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Desc));
            return this;
        }

        public List<T> ToList()
        {
            return _baseBuilder.ToList();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var sort in _sortExpressions)
            {
                if (sb.Length > 0)
                    sb.Append(",");

                string columnName = sort.Member.GetColumnName(_useAltName);
                sb.Append(_dialect.CreateToken(columnName));

                if (sort.Direction == SortDirection.Desc)
                    sb.Append(" DESC");
            }

            if (sb.Length > 0)
                sb.Insert(0, " ORDER BY ");

            return sb.ToString();
        }

        public static implicit operator List<T>(SortBuilder<T> builder)
        {
            return builder.ToList();
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            var list = this.ToList();
            return list.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
