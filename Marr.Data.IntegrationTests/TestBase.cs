using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Marr.Data.IntegrationTests
{
    public class TestBase
    {
        protected IDataMapper CreateSqlServerCeDB()
        {
            var db = new DataMapper(System.Data.SqlServerCe.SqlCeProviderFactory.Instance, ConfigurationManager.ConnectionStrings["DB_SqlServerCe"].ConnectionString);
            return db;
        }

        protected IDataMapper CreateSqlServerDB()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, ConfigurationManager.ConnectionStrings["DB_SqlServer"].ConnectionString);
            return db;
        }

        protected IDataMapper CreateAccessDB()
        {
            var db = new DataMapper(System.Data.OleDb.OleDbFactory.Instance, ConfigurationManager.ConnectionStrings["DB_Access"].ConnectionString);
            return db;
        }

        protected IDataMapper CreateSqliteDB()
        {
            var db = new DataMapper(System.Data.SQLite.SQLiteFactory.Instance, ConfigurationManager.ConnectionStrings["DB_Sqlite"].ConnectionString);
            db.SqlMode = SqlModes.Text;

            return db;
        }
    }
}
