using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.IntegrationTests.DB_SqlServerCe.Entities;

namespace Marr.Data.IntegrationTests.Misc
{
    [TestClass]
    public class InsertQueryTests : TestBase
    {
        [TestMethod]
        public void Insert_ShouldInclude_AllColumns_ByDefault()
        {
            using (var db = CreateSqlServerCeDB())
            {
                db.SqlMode = SqlModes.Text;
                db.BeginTransaction();

                OrderItem item = new OrderItem { ID = 1, OrderID = 500, ItemDescription = "desc", Price = 5.5m };

                string sql = db.Insert<OrderItem>()
                    .Entity(item)
                    .BuildQuery();

                Assert.IsTrue(sql.Contains("[Price]"));
                Assert.IsTrue(sql.Contains("[OrderID]"));
                Assert.IsTrue(sql.Contains("[ItemDescription]"));

                db.RollBack();
            }
        }

        [TestMethod]
        public void Insert_ExcludeColumns_ShouldFilterColumns()
        {
            using (var db = CreateSqlServerCeDB())
            {
                db.SqlMode = SqlModes.Text;
                db.BeginTransaction();

                OrderItem item = new OrderItem { ID = 1, OrderID = 500, ItemDescription = "desc", Price = 5.5m };

                string sql = db.Insert<OrderItem>()
                    .Entity(item)
                    .ColumnsExcluding(oi => oi.Price)
                    .BuildQuery();

                Assert.IsFalse(sql.Contains("[Price]"));

                Assert.IsTrue(sql.Contains("[OrderID]"));
                Assert.IsTrue(sql.Contains("[ItemDescription]"));

                db.RollBack();
            }
        }

        [TestMethod]
        public void Insert_IncludeColumns_ShouldFilterColumns()
        {
            using (var db = CreateSqlServerCeDB())
            {
                db.SqlMode = SqlModes.Text;
                db.BeginTransaction();

                OrderItem item = new OrderItem { ID = 1, OrderID = 500, ItemDescription = "desc", Price = 5.5m };

                string sql = db.Insert<OrderItem>()
                    .Entity(item)
                    .ColumnsIncluding(oi => oi.Price)
                    .BuildQuery();

                Assert.IsTrue(sql.Contains("[Price]"));

                Assert.IsFalse(sql.Contains("[OrderID]"));
                Assert.IsFalse(sql.Contains("[ItemDescription]"));

                db.RollBack();
            }
        }

    }
}
