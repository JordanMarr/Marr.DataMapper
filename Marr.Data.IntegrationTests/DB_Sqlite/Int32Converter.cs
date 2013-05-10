using System;
using Marr.Data.Converters;
using Marr.Data.Mapping;

namespace Marr.Data.IntegrationTests.DB_Sqlite
{
    public class Int32Converter : IConverter
    {
        public object FromDB(ColumnMap map, object dbValue)
        {
            if (dbValue == DBNull.Value)
            {
                return DBNull.Value;
            }

            if (dbValue is Int32)
            {
                return dbValue;
            }

            return Convert.ToInt32(dbValue);
        }

        public object ToDB(object clrValue)
        {
            return clrValue;
        }

        public Type DbType { get; private set; }
    }
}
