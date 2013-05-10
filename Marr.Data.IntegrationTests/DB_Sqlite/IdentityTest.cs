using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.IntegrationTests.DB_Sqlite.Entities;

namespace Marr.Data.IntegrationTests.DB_Sqlite
{
    [TestClass]
    public class IdentityTest : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            MapRepository.Instance.EnableTraceLogging = true;
        }

        [TestMethod]
        public void GeneratedQueryShouldBeAbleToGetIdentity_UsingSimpleOverload()
        {
            using (var db = CreateSqliteDB())
            {
                try
                {
                    db.BeginTransaction();

                    Order order = new Order { OrderName = "Order1" };

                    db.Insert<Order>(order);

                    Assert.IsTrue(order.ID > 0);
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void GeneratedQueryShouldBeAbleToGetIdentity()
        {
            using (var db = CreateSqliteDB())
            {
                try
                {
                    db.BeginTransaction();

                    Order order = new Order { OrderName = "Order1" };

                    db.Insert<Order>().Entity(order).GetIdentity().Execute();
                    
                    Assert.IsTrue(order.ID > 0);
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void ManualQueryShouldAutomaticallyGetIdentity()
        {
            using (var db = CreateSqliteDB())
            {
                try
                {
                    db.SqlMode = SqlModes.Text;

                    db.BeginTransaction();

                    Order order = new Order { OrderName = "Order1" };

                    var identity = db.Insert(order, "INSERT INTO [Order] (OrderName) VALUES (@OrderName);");

                    Assert.IsTrue(int.Parse(identity.ToString()) > 0);
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void ManualQueryWithManualIdentityStatementShouldBeAbleToGetIdentity()
        {
            using (var db = CreateSqliteDB())
            {
                try
                {
                    db.SqlMode = SqlModes.Text;

                    db.BeginTransaction();

                    Order order = new Order { OrderName = "Order1" };

                    var identity = db.Insert(order, "INSERT INTO [Order] (OrderName) VALUES (@OrderName);SELECT last_insert_rowid();");

                    Assert.IsTrue(int.Parse(identity.ToString()) > 0);
                }
                finally
                {
                    db.RollBack();
                }
            }
        }
    }
}
