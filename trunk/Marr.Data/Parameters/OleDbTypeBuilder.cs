using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;
using Marr.Data.Mapping;

namespace Marr.Data.Parameters
{
    public class OleDbTypeBuilder : IDbTypeBuilder
    {
        public Enum GetDbType(Type type)
        {
            if (type == typeof(String))
                return OleDbType.VarChar;

            else if (type == typeof(Int32))
                return OleDbType.Integer;

            else if (type == typeof(Decimal))
                return OleDbType.Decimal;

            else if (type == typeof(DateTime))
                return OleDbType.DBTimeStamp;

            else if (type == typeof(Boolean))
                return OleDbType.Boolean;

            else if (type == typeof(Int16))
                return OleDbType.SmallInt;

            else if (type == typeof(Int64))
                return OleDbType.BigInt;

            else if (type == typeof(Double))
                return OleDbType.Double;

            else if (type == typeof(Byte))
                return OleDbType.Binary;

            else if (type == typeof(Byte[]))
                return OleDbType.VarBinary;

            else if (type == typeof(Guid))
                return OleDbType.Guid;

            else
                return OleDbType.Variant;
        }

        public void SetDbType(System.Data.IDbDataParameter param, ColumnMap column)
        {
            var oleDbParam = (OleDbParameter)param;
            oleDbParam.OleDbType = (OleDbType)column.DBType;
        }
    }
}
