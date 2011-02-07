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
        private string _target;
        private const string _paramPrefix = "@";
        private ColumnMapCollection _columns;
        private DbParameterCollection _parameters;

        public SqlServerInsertQuery(ColumnMapCollection columns, DbParameterCollection parameters, string target)
        {
            _target = target;
            _columns = columns;
            _parameters = parameters;
        }

        public string Generate()
        {
            StringBuilder sql = new StringBuilder();
            StringBuilder values = new StringBuilder(") VALUES (");

            sql.AppendFormat("INSERT INTO {0} (", _target);

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

            values.Append(");");

            sql.Append(values);

            if (_columns.ReturnValues.Count() > 0)
            {
                sql.Append("SELECT SCOPE_IDENTITY();");
            }

            return sql.ToString();
        }
    }
}
