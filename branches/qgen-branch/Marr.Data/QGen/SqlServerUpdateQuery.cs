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
        private const string _paramPrefix = "@";
        private ColumnMapCollection _columns;
        private DbParameterCollection _parameters;

        public SqlServerUpdateQuery(ColumnMapCollection columns, DbParameterCollection parameters)
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
            StringBuilder where = new StringBuilder(" WHERE ");

            sql.AppendFormat("UPDATE [{0}].[{1}] SET", schema, target);

            int startIndex = sql.Length;

            for (int i = 0; i < _parameters.Count; i++)
            {
                var p = _parameters[i];
                var c = _columns[i];

                if (sql.Length > startIndex)
                    sql.Append(",");

                if (!c.ColumnInfo.IsAutoIncrement)
                {
                    sql.AppendFormat("[{0}]={1}{2}", c.ColumnInfo.Name, _paramPrefix, p.ParameterName);
                }

                if (c.ColumnInfo.IsPrimaryKey)
                {
                    where.AppendFormat("[{0}]={1}{2}", c.ColumnInfo.Name, _paramPrefix, p.ParameterName);
                }
            }

            sql.Append(where);

            return sql.ToString();
        }

        
    }
}
