using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Marr.Data.QGen.Dialects;

namespace Marr.Data.QGen
{
    internal class QueryFactory
    {
        private const string DB_SqlClient = "System.Data.SqlClient";
        private const string DB_SqlCe = "System.Data.SqlServerCe.SqlCeProviderFactory";
        private const string DB_SystemDataOracleClient = "System.Data.OracleClient";
        private const string DB_OracleDataAccessClient = "Oracle.DataAccess.Client";

        private static Dialect _dialect;

        public static IQuery CreateUpdateQuery(Mapping.ColumnMapCollection columns, IDataMapper dataMapper, string target, string whereClause)
        {
            Dialect dialect = CreateDialect(dataMapper);
            return new UpdateQuery(dialect, columns, dataMapper.Command, target, whereClause);
        }

        public static IQuery CreateInsertQuery(Mapping.ColumnMapCollection columns, IDataMapper dataMapper, string target)
        {
            Dialect dialect = CreateDialect(dataMapper);
            return new InsertQuery(dialect, columns, dataMapper.Command, target);
        }

        public static IQuery CreateDeleteQuery(string target, string whereClause)
        {
            return new DeleteQuery(target, whereClause);
        }

        public static IQuery CreateSelectQuery(Mapping.ColumnMapCollection columns, IDataMapper dataMapper, string target, string where, string orderBy, bool useAltName)
        {
            Dialect dialect = CreateDialect(dataMapper);
            return new SelectQuery(dialect, columns, target, where, orderBy, useAltName);
        }

        public static Dialects.Dialect CreateDialect(IDataMapper dataMapper)
        {
            if (_dialect == null)
            {
                string providerString = dataMapper.ProviderString;

                switch (providerString)
                {
                    case DB_SqlClient:
                        _dialect = new SqlServerDialect();
                        break;

                    case DB_OracleDataAccessClient:
                        _dialect = new OracleDialect();
                        break;

                    case DB_SystemDataOracleClient:
                        _dialect = new OracleDialect();
                        break;

                    case DB_SqlCe:
                        _dialect = new SqlServerDialect();
                        break;

                    default:
                        _dialect = new Dialect();
                        break;
                }
            }

            return _dialect;
        }
    }
}
