using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Marr.Data.QGen.Dialects;

namespace Marr.Data.QGen
{
    /// <summary>
    /// This class contains the factory logic that determines which type of IQuery object should be created.
    /// </summary>
    internal class QueryFactory
    {
        private const string DB_SqlClient = "System.Data.SqlClientFactory";
        private const string DB_OleDb = "System.Data.OleDb.OleDbFactory";
        private const string DB_SqlCe = "System.Data.SqlServerCe.SqlCeProviderFactory";
        private const string DB_SystemDataOracleClient = "System.Data.OracleClientFactory";
        private const string DB_OracleDataAccessClient = "Oracle.DataAccess.Client.OracleClientFactory";

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

        public static IQuery CreateDeleteQuery(Dialects.Dialect dialect, Table targetTable, string whereClause)
        {
            return new DeleteQuery(dialect, targetTable, whereClause);
        }

        public static IQuery CreateSelectQuery(TableCollection tables, IDataMapper dataMapper, string where, string orderBy, bool useAltName)
        {
            Dialect dialect = CreateDialect(dataMapper);
            return new SelectQuery(dialect, tables, where, orderBy, useAltName);
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
                        _dialect = new SqlServerCeDialect();
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
