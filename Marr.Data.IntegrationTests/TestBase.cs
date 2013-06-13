using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Marr.Data.IntegrationTests
{
    public class TestBase
    {
        public TestBase()
        {
            ResetMapRepository();
        }

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

        /// <summary>
        /// Ensures that the MapRepository singleton state is reset.
        /// This prevents unit test from affecting each other by changing shared state.
        /// </summary>
        protected void ResetMapRepository()
        {
            MapRepository.Instance.Tables.Clear();
            MapRepository.Instance.Columns.Clear();
            MapRepository.Instance.Relationships.Clear();
            MapRepository.Instance.DbTypeBuilder = new Marr.Data.Parameters.DbTypeBuilder();
            MapRepository.Instance.TypeConverters.Clear();
        }
    }
}
