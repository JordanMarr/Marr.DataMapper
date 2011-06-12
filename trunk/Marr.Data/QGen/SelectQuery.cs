using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data;
using Marr.Data.Mapping;
using System.Data.Common;
using Marr.Data.QGen.Dialects;

namespace Marr.Data.QGen
{
    /// <summary>
    /// This class is responsible for creating a select query.
    /// </summary>
    public class SelectQuery : IQuery
    {
        protected Dialect Dialect { get; set; }
        protected string WhereClause { get; set; }
        protected string OrderBy { get; set; }
        protected TableCollection Tables { get; set; }
        protected bool UseAltName;

        public SelectQuery(Dialect dialect, TableCollection tables, string whereClause, string orderBy, bool useAltName)
        {
            Dialect = dialect;
            Tables = tables;
            WhereClause = whereClause;
            OrderBy = orderBy;
            UseAltName = useAltName;
        }

        public string Generate()
        {
            StringBuilder sql = new StringBuilder("SELECT ");

            int startIndex = sql.Length;

            // COLUMNS
            foreach (Table join in Tables)
            {
                for (int i = 0; i < join.Columns.Count; i++)
                {
                    var c = join.Columns[i];

                    if (sql.Length > startIndex)
                        sql.Append(",");

                    if (join is View)
                    {
                        string token = string.Empty;
                        if (UseAltName && c.ColumnInfo.AltName != null && c.ColumnInfo.AltName != c.ColumnInfo.Name)
                        {
                            token = string.Concat(join.Alias, ".", c.ColumnInfo.AltName);
                        }
                        else
                        {
                            token = string.Concat(join.Alias, ".", c.ColumnInfo.Name);
                        }
                        sql.Append(Dialect.CreateToken(token));
                    }
                    else
                    {
                        string token = string.Concat(join.Alias, ".", c.ColumnInfo.Name);
                        sql.Append(Dialect.CreateToken(token));

                        if (UseAltName && c.ColumnInfo.AltName != null && c.ColumnInfo.AltName != c.ColumnInfo.Name)
                        {
                            string altName = c.ColumnInfo.AltName;
                            sql.AppendFormat(" AS {0}", altName);
                        }
                    }                    
                }
            }

            // BASE TABLE
            Table baseTable = Tables[0];
            sql.AppendFormat(" FROM {0} {1} ", Dialect.CreateToken(baseTable.Name), Dialect.CreateToken(baseTable.Alias));

            // JOINS
            for (int i = 1; i < Tables.Count; i++)
            {
                if (Tables[i].JoinType != JoinType.None)
                {
                    sql.AppendFormat("{0} {1} {2} {3} ",
                        TranslateJoin(Tables[i].JoinType),
                        Dialect.CreateToken(Tables[i].Name),
                        Dialect.CreateToken(Tables[i].Alias),
                        Tables[i].JoinClause);
                }
            }

            sql.Append(WhereClause);

            sql.Append(OrderBy);

            return sql.ToString();
        }

        private string TranslateJoin(JoinType join)
        {
            switch (join)
            {
                case JoinType.Inner:
                    return "INNER JOIN";
                case JoinType.Left:
                    return "LEFT JOIN";
                case JoinType.Right:
                    return "RIGHT JOIN";
                default:
                    return string.Empty;
            }
        }
    }
}
