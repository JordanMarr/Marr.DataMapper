using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data;
using Marr.Data.Mapping;
using System.Data.Common;

namespace Marr.Data.QGen
{
    public class SelectQuery : IQuery
    {
        protected string Target { get; set; }
        protected string WhereClause { get; set; }
        protected string OrderBy { get; set; }
        protected ColumnMapCollection Columns { get; set; }
        protected bool UseAltName;

        public SelectQuery(ColumnMapCollection columns, string target, string whereClause, string orderBy, bool useAltName)
        {
            Columns = columns;
            Target = target;
            WhereClause = whereClause;
            OrderBy = orderBy;
            UseAltName = useAltName;
        }

        public string Generate()
        {
            StringBuilder sql = new StringBuilder("SELECT ");

            int startIndex = sql.Length;

            for (int i = 0; i < Columns.Count; i++)
            {
                var c = Columns[i];

                if (sql.Length > startIndex)
                    sql.Append(",");

                sql.AppendFormat("[{0}]", c.ColumnInfo.GetColumName(UseAltName));
            }

            sql.AppendFormat(" FROM {0} ", Target);

            sql.Append(WhereClause);

            sql.Append(OrderBy);

            return sql.ToString();
        }


    }
}
