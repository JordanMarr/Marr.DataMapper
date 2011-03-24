using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using System.Data.Common;

namespace Marr.Data.QGen
{
    public class InsertQuery : IQuery
    {
        protected string Target { get; set; }
        protected ColumnMapCollection Columns { get; set; }
        protected DbCommand Command { get; set; }

        public InsertQuery(ColumnMapCollection columns, DbCommand command, string target)
        {
            Target = target;
            Columns = columns;
            Command = command;
        }

        public virtual string Generate()
        {
            StringBuilder sql = new StringBuilder();
            StringBuilder values = new StringBuilder(") VALUES (");

            sql.AppendFormat("INSERT INTO {0} (", Target);

            int sqlStartIndex = sql.Length;
            int valuesStartIndex = values.Length;

            foreach (DbParameter p in Command.Parameters)
            {
                var c = Columns[p.ParameterName];

                if (c == null)
                    break; // All insert columns have been added

                if (sql.Length > sqlStartIndex)
                    sql.Append(",");

                if (values.Length > valuesStartIndex)
                    values.Append(",");

                if (!c.ColumnInfo.IsAutoIncrement)
                {
                    string columnName = c.ColumnInfo.Name;
                    bool hasSpaces = columnName.Contains(' ');

                    if (hasSpaces)
                        sql.AppendFormat("[{0}]", columnName);
                    else
                        sql.AppendFormat("{0}", columnName);

                    values.AppendFormat("{0}{1}", Command.ParameterPrefix(), p.ParameterName);
                }
            }

            values.Append(")");

            sql.Append(values);
            
            return sql.ToString();
        }
    }
}
