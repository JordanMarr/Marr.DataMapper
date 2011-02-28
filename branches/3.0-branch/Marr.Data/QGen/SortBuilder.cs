using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace Marr.Data.QGen
{
    public class SortBuilder<T> : IEnumerable<T>
    {
        private AutoQueryBuilder<T> _baseBuilder;
        private List<SortColumn<T>> _sortExpressions;
        private bool _useAltName;

        public SortBuilder(AutoQueryBuilder<T> baseBuilder, bool useAltName)
        {
            _baseBuilder = baseBuilder;
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

        internal SortBuilder<T> OrderBy(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Asc));
            return this;
        }

        internal SortBuilder<T> OrderByDescending(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Desc));
            return this;
        }

        internal SortBuilder<T> ThenBy(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Asc));
            return this;
        }

        internal SortBuilder<T> ThenByDescending(Expression<Func<T, object>> sortExpression)
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

                sb.AppendFormat("[{0}]", sort.Member.GetColumnName(_useAltName));

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
