using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Marr.Data.Mapping;

namespace Marr.Data.Parameters
{
    public class DbTypeBuilder : IDbTypeBuilder
    {
        public Enum GetDbType(Type type)
        {
            if (type == typeof(String))
                return DbType.String;

            else if (type == typeof(Int32))
                return DbType.Int32;

            else if (type == typeof(Decimal))
                return DbType.Decimal;

            else if (type == typeof(DateTime))
                return DbType.DateTime;

            else if (type == typeof(Boolean))
                return DbType.Boolean;

            else if (type == typeof(Int16))
                return DbType.Int16;

            else if (type == typeof(Single))
                return DbType.Single;

            else if (type == typeof(Int64))
                return DbType.Int64;

            else if (type == typeof(Double))
                return DbType.Double;

            else if (type == typeof(Byte))
                return DbType.Byte;

            else if (type == typeof(Byte[]))
                return DbType.Binary;

            else if (type == typeof(Guid))
                return DbType.Guid;

            else
                return DbType.Object;
        }

        public void SetDbType(System.Data.IDbDataParameter param, ColumnMap column)
        {
            param.DbType = (DbType)column.DBType;
        }
    }
}
