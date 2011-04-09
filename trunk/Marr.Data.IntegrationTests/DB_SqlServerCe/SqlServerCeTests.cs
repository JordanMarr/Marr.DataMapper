using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using Marr.Data.IntegrationTests.DB_SqlServerCe.Entities;

namespace Marr.Data.IntegrationTests.DB_SqlServerCe
{
    /// <summary>
    /// Contains integration tests for the data mapper.
    /// </summary>
    [TestClass]
    public class SqlServerCeTests : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            MapRepository.Instance.EnableTraceLogging = true;
        }

        [TestMethod]
        public void TestSqlCe_Insert_Query()
        {
            using (var db = CreateSqlServerCeDB())
            {
                Order order1 = new Order { OrderName = "Test1" };
                db.Insert(order1);

                Order order2 = new Order { OrderName = "Test2" };
                db.Insert(order2);

                var results = (from o in db.Query<Order>()
                               orderby o.OrderName ascending
                               select o).ToList();

                Assert.IsTrue(results.Count == 2);
                Assert.AreEqual(results[0].OrderName, "Test1");
                Assert.AreEqual(results[1].OrderName, "Test2");
            }
            
        }
    }
}
