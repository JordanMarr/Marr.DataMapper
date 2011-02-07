using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace Marr.Data.QGen
{
    internal class QueryFactory
    {
        public static IQuery CreateUpdateQuery(Mapping.ColumnMapCollection columns, DbCommand command, string target, string whereClause)
        {
            return new UpdateQuery(columns, command, target, whereClause);
        }

        public static IQuery CreateInsertQuery(Mapping.ColumnMapCollection columns, DbCommand command, string target)
        {
            if (command is System.Data.SqlClient.SqlCommand)
            {
                return new SqlServerInsertQuery(columns, command, target);
            }
            else
            {
                return new InsertQuery(columns, command, target);
            }
        }

        public static IQuery CreateDeleteQuery(string target, string whereClause)
        {
            return new DeleteQuery(target, whereClause);
        }

        public static IQuery CreateSelectQuery(Mapping.ColumnMapCollection columns, string target, string where)
        {
            return new SelectQuery(columns, target, where);
        }
    }
}
