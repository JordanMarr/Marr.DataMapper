using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using System.Data.Common;

namespace Marr.Data.QGen
{
    public class SqlServerInsertQuery : InsertQuery
    {
        public SqlServerInsertQuery(ColumnMapCollection columns, DbCommand command, string target)
            : base(columns, command, target)
        { }

        public override string Generate()
        {
            StringBuilder sql = new StringBuilder(base.Generate());

            if (Columns.ReturnValues.Count() > 0)
            {
                sql.Append("SELECT SCOPE_IDENTITY();");
            }

            return sql.ToString();
        }
    }
}
