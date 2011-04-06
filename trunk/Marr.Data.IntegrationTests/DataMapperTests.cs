using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.IntegrationTests.Entities;
using System.Configuration;

namespace Marr.Data.IntegrationTests
{
    /// <summary>
    /// Contains integration tests for the data mapper.
    /// </summary>
    [TestClass]
    public class DataMapperTests
    {
        [TestMethod]
        public void TestLinq()
        {
            var db = new DataMapper(System.Data.SqlServerCe.SqlCeProviderFactory.Instance, ConfigurationManager.ConnectionStrings["TestDB"].ConnectionString);

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
