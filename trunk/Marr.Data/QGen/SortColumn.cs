using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace Marr.Data.QGen
{
    public class SortColumn<T>
    {
        public SortColumn(Expression<Func<T, object>> sortExpression, SortDirection direction)
        {
            MemberExpression me = GetMemberExpression(sortExpression.Body);
            Member = me.Member;
            Direction = direction;
        }

        public SortColumn(MemberInfo member, SortDirection direction)
        {
            Member = member;
            Direction = direction;
        }

        public SortDirection Direction { get; private set; }
        public MemberInfo Member { get; private set; }

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
    }
    
    public enum SortDirection
    {
        Asc,
        Desc
    }
}
