using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using System.Data.Common;

namespace Marr.Data.QGen
{
    public class SqlServerSelectQuery : IQuery
    {
        private string _target;
        private string _whereClause;
        private ColumnMapCollection _columns;

        public SqlServerSelectQuery(ColumnMapCollection columns, string target, string whereClause)
        {
            _target = target;
            _whereClause = whereClause;
            _columns = columns;
        }

        public string Generate()
        {
            StringBuilder sql = new StringBuilder("SELECT ");

            int startIndex = sql.Length;

            for (int i = 0; i < _columns.Count; i++)
            {
                var c = _columns[i];

                if (sql.Length > startIndex)
                    sql.Append(",");

                sql.AppendFormat("[{0}]", c.ColumnInfo.Name);
            }

            sql.AppendFormat(" FROM {0} ", _target);

            sql.Append(_whereClause);

            return sql.ToString();
        }


    }
}
