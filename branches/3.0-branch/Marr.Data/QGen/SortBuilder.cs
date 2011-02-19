using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Marr.Data.QGen
{
    public class SortBuilder<T>
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

        public SortBuilder<T> Order(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Asc));
            return this;
        }

        public SortBuilder<T> OrderDesc(Expression<Func<T, object>> sortExpression)
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

                MemberExpression me = GetMemberExpression(sort.SortExpression.Body);
                sb.AppendFormat("[{0}]", me.Member.GetColumnName(_useAltName));

                if (sort.Direction == SortDirection.Desc)
                    sb.Append(" DESC");
            }

            if (sb.Length > 0)
                sb.Insert(0, " ORDER BY ");

            return sb.ToString();
        }

        private MemberExpression GetMemberExpression(Expression exp)
        {
            MemberExpression me = exp as MemberExpression;

            if (me == null)
            {
                var ue = exp as UnaryExpression;
                me = ue.Operand as MemberExpression;
            }

            return me;
        }

        public static implicit operator List<T>(SortBuilder<T> builder)
        {
            return builder.ToList();
        }
    }
}
