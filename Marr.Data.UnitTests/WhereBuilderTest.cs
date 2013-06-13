using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.UnitTests.Entities;
using Marr.Data.UnitTests;

namespace Marr.Data.UnitTests
{
    [TestClass]
    public class WhereBuilderTest : TestBase
    {
        [TestInitialize]
        public void Init()
        {
            InitMappings();
        }

        #region - Nulls in Where -

        [TestMethod]
        public void WhereIsNull()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where(p => p.Name == null)
                .BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE ([t0].[Name] IS NULL)"));
        }

        [TestMethod]
        public void WhereIsNotNull()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where(p => p.Name != null)
                .BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE ([t0].[Name] IS NOT NULL)"));
        }

        [TestMethod]
        public void WhereIsNullWithOtherParameters()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where(p => p.ID == 1 || p.Name == null || p.ID == 2)
                .BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("[t0].[ID] = @P0"));
            Assert.IsTrue(sqlQuery.Contains("[t0].[Name] IS NULL"));
            Assert.IsTrue(sqlQuery.Contains("[t0].[ID] = @P1"));
        }

        #endregion

        #region - OrWhere -

        [TestMethod]
        public void OrWhere_Lambda_Lambda()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where(p => p.Name == "Bob")
                .OrWhere(p => p.Name == "Robert")
                .OrderBy(p => p.Name).BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE ([t0].[Name] = @P0) OR ([t0].[Name] = @P1)"));
        }

        [TestMethod]
        public void OrWhere_Lambda_Text()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where(p => p.Name == "Bob")
                .OrWhere("[Name] = 'Robert'")
                .OrderBy(p => p.Name).BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE ([t0].[Name] = @P0) OR [Name] = 'Robert'"));
        }

        [TestMethod]
        public void OrWhere_Text_Lambda()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where("[Name] = 'Robert'")
                .OrWhere(p => p.Name == "Bob")
                .OrderBy(p => p.Name).BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE [Name] = 'Robert' OR ([t0].[Name] = @P0)"));
        }

        [TestMethod]
        public void OrWhere_Text_Text()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where("[Name] = 'Bob'")
                .OrWhere("[Name] = 'Robert'")
                .OrderBy(p => p.Name).BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE [Name] = 'Bob' OR [Name] = 'Robert'"));
        }

        #endregion

        #region - AndWhere -

        [TestMethod]
        public void AndWhere_Lambda_Lambda()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where(p => p.Name == "Bob")
                .AndWhere(p => p.Name == "Robert")
                .OrderBy(p => p.Name).BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE ([t0].[Name] = @P0) AND ([t0].[Name] = @P1)"));
        }

        [TestMethod]
        public void AndWhere_Lambda_Text()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where(p => p.Name == "Bob")
                .AndWhere("[Name] = 'Robert'")
                .OrderBy(p => p.Name).BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE ([t0].[Name] = @P0) AND [Name] = 'Robert'"));
        }

        [TestMethod]
        public void AndWhere_Text_Lambda()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where("[Name] = 'Robert'")
                .AndWhere(p => p.Name == "Bob")
                .OrderBy(p => p.Name).BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE [Name] = 'Robert' AND ([t0].[Name] = @P0)"));
        }

        [TestMethod]
        public void AndWhere_Text_Text()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where("[Name] = 'Bob'")
                .AndWhere("[Name] = 'Robert'")
                .OrderBy(p => p.Name).BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE [Name] = 'Bob' AND [Name] = 'Robert'"));
        }

        #endregion

        #region - Misc Where Clauses -

        [TestMethod]
        public void Where_And_Or_Mix()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where(p => p.Name == "Bob" || p.Name == "Robert")
                .AndWhere(p => p.Age > 30 && p.Age < 40).BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE (([t0].[Name] = @P0) OR ([t0].[Name] = @P1)) AND (([t0].[Age] > @P2) AND ([t0].[Age] < @P3))"));
        }

        [TestMethod]
        public void Where_3_Clauses()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlQuery = db.Query<Person>()
                .Where(p => p.Name == "Bob")
                .OrWhere(p => p.Name == "Robert")
                .OrWhere(p => p.Name == "John").BuildQuery();

            Assert.IsTrue(sqlQuery.Contains("WHERE ([t0].[Name] = @P0) OR ([t0].[Name] = @P1) OR ([t0].[Name] = @P2)"));
        }

        #endregion
    }
}
