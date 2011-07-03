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

                    var orders = db.Query<Order>().Where(o => o.ID > 0).ToList();
                    int count = orders.Count;
                    db.Delete<Order>(o => o.ID > 0);

                    Order order1 = new Order { OrderName = "Test1" };
                    db.Insert(order1);

                    Order order2 = new Order { OrderName = "Test2" };
                    db.Insert(order2);

                    var orderItems = new List<OrderItem>();

                    var results = (from o in db.Query<Order>()
                                   where o.OrderName == "Test1"
                                   orderby o.OrderName
                                   select o).ToList();

                    Assert.IsTrue(results.Count == 1);
                    Assert.AreEqual(results[0].OrderName, "Test1");

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

                    // Insert a new order
                    Order newOrder = new Order();
                    newOrder.OrderName = "new order";
                    int orderID = Convert.ToInt32(db.Insert<Order>().Entity(newOrder).GetIdentity().Execute());
                    Assert.IsTrue(orderID > 0);

                    // Update order name to use the generated ID autoincremented value
                    newOrder.OrderName = string.Concat(newOrder.OrderName, " ", newOrder.ID);
                    db.Update<Order>(newOrder, o => o.ID == newOrder.ID);

                    // Add an order item associated to the newly added order
                    OrderItem orderItem = new OrderItem { OrderID = newOrder.ID, ItemDescription = "Test item", Price = 5.5m };
                    int orderItemID = Convert.ToInt32(db.Insert<OrderItem>().Entity(orderItem).GetIdentity().Execute());
                    Assert.IsTrue(orderItemID > 0);

                    // Add a receipt associated to the new ordeer / order item
                    Receipt receipt = new Receipt { OrderItemID = orderItem.ID, AmountPaid = 5.5m };
                    db.Insert<Receipt>(receipt);

                    // Query the newly added order with its order item (do not query receipt)
                    var orderWithItem = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)
                        .Where(o => o.ID == newOrder.ID)
                        .FirstOrDefault();

                    // Query the newly added order with associated order item and receipt
                    var orderWithItemAndReceipt = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)
                        .Join<OrderItem, Receipt>(JoinType.Left, oi => oi.ItemReceipt, (oi, r) => oi.ID == r.OrderItemID)
                        .Where(o => o.ID == newOrder.ID).FirstOrDefault();

                    Assert.IsNotNull(orderWithItem);
                    Assert.IsTrue(orderWithItem.OrderItems.Count == 1);
                    Assert.IsNull(orderWithItem.OrderItems[0].ItemReceipt);

                    Assert.IsNotNull(orderWithItemAndReceipt.OrderItems[0].ItemReceipt);

                    // Delete all added items
                    db.Delete<Order>(o => o.ID == orderID);
                    db.Delete<OrderItem>(oi => oi.ID == orderItemID);
                    db.Delete<Receipt>(r => r.OrderItemID == orderItemID);

                    // Verify items are deleted
                    var receipts = db.Query<Receipt>().Where(r => r.OrderItemID == orderItemID).ToList();
                    Assert.IsTrue(receipts.Count == 0);

                    var orderItems = db.Query<OrderItem>().Where(oi => oi.ID == orderItemID).ToList();
                    Assert.IsTrue(orderItems.Count == 0);

                    var orders = db.Query<Order>().Where(o => o.ID == orderID).ToList();
                    Assert.IsTrue(orders.Count == 0);

                    db.Commit();
                }
                catch
                {
                    db.RollBack();
                    throw;
                }                
            }
        }

        [TestMethod]
        public void TestQueryBuilders()
        {
            using (var db = CreateSqlServerCeDB())
            {
                db.SqlMode = SqlModes.Text;
                db.BeginTransaction();

                string query = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)
                        .Join<OrderItem, Receipt>(JoinType.Left, oi => oi.ItemReceipt, (oi, r) => oi.ID == r.OrderItemID)
                        .Where(o => o.OrderName == "Test1").BuildQuery();

                db.Parameters.Clear();

                string insertQuery = db.Insert<Order>()
                    .Entity(new Order())
                    .TableName("ORDERS_TABLE")
                    .GetIdentity()
                    .BuildQuery();


                Assert.IsNotNull(query);
                Assert.IsNotNull(insertQuery);
                db.RollBack();
            }
        }
    }
}
