using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using System.Data.Common;

namespace Marr.Data.QGen
{
    public class SqlServerInsertQuery : IQuery
    {
        private const string _paramPrefix = "@";
        private ColumnMapCollection _columns;
        private DbParameterCollection _parameters;

        public SqlServerInsertQuery(ColumnMapCollection columns, DbParameterCollection parameters)
        {
            _columns = columns;
            _parameters = parameters;
        }

        public string Generate(string schema, string target)
        {
            if (_columns.PrimaryKeys.Count == 0)
            {
                throw new Exception("No primary keys have been specified for this entity.");
            }

            StringBuilder sql = new StringBuilder();
            StringBuilder values = new StringBuilder(") VALUES (");

            sql.AppendFormat("INSERT INTO [{0}].[{1}] (", schema, target);

            int sqlStartIndex = sql.Length;
            int valuesStartIndex = values.Length;

            for (int i = 0; i < _parameters.Count; i++)
            {
                var p = _parameters[i];
                var c = _columns[i];

                if (sql.Length > sqlStartIndex)
                    sql.Append(",");

                if (values.Length > valuesStartIndex)
                    values.Append(",");

                if (!c.ColumnInfo.IsAutoIncrement)
                {
                    sql.AppendFormat("[{0}]", c.ColumnInfo.Name);
                    values.AppendFormat("{0}{1}", _paramPrefix, p.ParameterName);
                }
            }

            values.Append(")");

            sql.Append(values);

            return sql.ToString();
        }
    }
}
