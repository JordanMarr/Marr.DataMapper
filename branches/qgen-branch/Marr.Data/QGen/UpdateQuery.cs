using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Marr.Data.Mapping;

namespace Marr.Data.QGen
{
    public class UpdateQuery : IQuery
    {
        protected string Target { get; set; }
        protected ColumnMapCollection Columns { get; set; }
        protected DbCommand Command { get; set; }
        protected string WhereClause { get; set; }

        public UpdateQuery(ColumnMapCollection columns, DbCommand command, string target, string whereClause)
        {
            Target = target;
            Columns = columns;
            Command = command;
            WhereClause = whereClause;
        }

        public string Generate()
        {
            StringBuilder sql = new StringBuilder();

            sql.AppendFormat("UPDATE {0} SET", Target);

            int startIndex = sql.Length;

            for (int i = 0; i < Columns.Count; i++)
            {
                var p = Command.Parameters[i];
                var c = Columns[i];

                if (sql.Length > startIndex)
                    sql.Append(",");

                if (!c.ColumnInfo.IsAutoIncrement)
                {
                    sql.AppendFormat("[{0}]={1}{2}", c.ColumnInfo.Name, Command.ParameterPrefix(), p.ParameterName);
                }
            }

            sql.AppendFormat(" {0}", WhereClause);

            return sql.ToString();
        }


    }
}
