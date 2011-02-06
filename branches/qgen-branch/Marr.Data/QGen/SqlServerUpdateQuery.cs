using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Marr.Data.Mapping;

namespace Marr.Data.QGen
{
    public class SqlServerUpdateQuery : IQuery
    {
        private string _target;
        private const string _paramPrefix = "@";
        private ColumnMapCollection _columns;
        private DbParameterCollection _parameters;
        private string _whereClause;

        public SqlServerUpdateQuery(ColumnMapCollection columns, DbParameterCollection parameters, string target, string whereClause)
        {
            _target = target;
            _columns = columns;
            _parameters = parameters;
            _whereClause = whereClause;
        }

        public string Generate()
        {
            StringBuilder sql = new StringBuilder();

            sql.AppendFormat("UPDATE {0} SET", _target);

            int startIndex = sql.Length;

            for (int i = 0; i < _columns.Count; i++)
            {
                var p = _parameters[i];
                var c = _columns[i];

                if (sql.Length > startIndex)
                    sql.Append(",");

                if (!c.ColumnInfo.IsAutoIncrement)
                {
                    sql.AppendFormat("[{0}]={1}{2}", c.ColumnInfo.Name, _paramPrefix, p.ParameterName);
                }
            }

            sql.AppendFormat(" {0}", _whereClause);

            return sql.ToString();
        }

        
    }
}
