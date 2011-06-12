using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using Marr.Data.IntegrationTests.DB_SqlServerCe.Entities;
using Marr.Data.QGen;

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
                try
                {
                    db.SqlMode = SqlModes.Text;
                    db.BeginTransaction();

                    Order order1 = new Order { OrderName = "Test1" };
                    db.Insert(order1);

                    Order order2 = new Order { OrderName = "Test2" };
                    db.Insert(order2);

                    var orderItems = new List<OrderItem>();

                    var results = (from o in db.Query<Order>().Graph()
                                   where o.OrderName == "Test1"
                                   orderby o.OrderName
                                   select o).ToList();

                    db.Query<Order>()
                        .Where(o => o.ID == 5);


                    //Assert.IsTrue(results.Count == 2);
                    //Assert.AreEqual(results[0].OrderName, "Test1");
                    //Assert.AreEqual(results[1].OrderName, "Test2");

                    db.Commit();
                }
                catch
                {
                    db.RollBack();
                }
            }
            
        }

        [TestMethod]
        public void TestJoin()
        {
            using (var db = CreateSqlServerCeDB())
            {
                try
                {
                    db.SqlMode = SqlModes.Text;
                    db.BeginTransaction();

                    var order54 = db.Query<Order>().Where(o => o.ID == 54).Single();

                    Assert.IsNotNull(order54);

                    OrderItem orderItem = new OrderItem { OrderID = 54, ItemDescription = "Test item", Price = 5.5m };
                    db.Insert<OrderItem>(orderItem);

                    var orderWithItem = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, (o, oi) => o.ID == oi.OrderID)
                        .Where(o => o.ID == 54)
                        .FirstOrDefault();

                    //var results = db.Query<Order>()
                    //    .Join<Order, OrderItem>(JoinType.Left, (o, oi) => o.ID == oi.OrderID)
                    //    .Join<OrderItem, Receipt>(JoinType.Left, (oi, r) => oi.ID == r.OrderItemID)
                    //    .Where(o => o.OrderName == "Test1").ToList();

                    var notFree = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, (o, oi) => o.ID == oi.OrderID)
                        .Where<OrderItem>(oi => oi.Price > 0).ToList();

                    Assert.IsTrue(notFree.Count > 0);

                    Assert.IsNotNull(orderWithItem);
                    Assert.IsTrue(orderWithItem.OrderItems.Count == 1);

                    int orderItemID = orderWithItem.OrderItems[0].ID;
                    db.Delete<OrderItem>(oi => oi.ID == orderItemID);

                    //Assert.IsTrue(results.Count > 0);

                    db.Commit();
                }
                catch
                {
                    db.RollBack();
                }                
            }

        }
    }
}
