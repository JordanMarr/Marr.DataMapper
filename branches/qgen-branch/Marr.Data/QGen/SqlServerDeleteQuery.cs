using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using System.Data.Common;

namespace Marr.Data.QGen
{
    public class SqlServerDeleteQuery : IQuery
    {
        private string _target;
        private const string _paramPrefix = "@";
        private ColumnMapCollection _columns;
        private DbParameterCollection _parameters;

        public SqlServerDeleteQuery(ColumnMapCollection columns, DbParameterCollection parameters, string target)
        {
            _target = target;
            _columns = columns;
            _parameters = parameters;
        }

        public string Generate()
        {
            if (_columns.PrimaryKeys.Count == 0)
            {
                throw new Exception("No primary keys have been specified for this entity.");
            }

            StringBuilder sql = new StringBuilder();

            sql.AppendFormat("DELETE FROM {0} WHERE ", _target);

            int startIndex = sql.Length;

            for (int i = 0; i < _parameters.Count; i++)
            {
                var p = _parameters[i];
                var c = _columns[i];

                if (c.ColumnInfo.IsPrimaryKey)
                {
                    if (sql.Length > startIndex)
                        sql.Append(" AND ");
               
                    sql.AppendFormat("[{0}]={1}{2}", c.ColumnInfo.Name, _paramPrefix, p.ParameterName);
                }
            }
            
            return sql.ToString();
        }
    }
}
