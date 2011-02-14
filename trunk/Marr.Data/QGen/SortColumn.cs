using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Marr.Data.QGen
{
    public class SortColumn<T>
    {
        public SortColumn(Expression<Func<T, object>> sortExpression, SortDirection direction)
        {
            SortExpression = sortExpression;
            Direction = direction;
        }

        public SortDirection Direction { get; private set; }
        public Expression<Func<T, object>> SortExpression { get; private set; }
    }
    
    public enum SortDirection
    {
        Asc,
        Desc
    }
}
