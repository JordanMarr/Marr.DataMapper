using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.IntegrationTests.DB_SqlServer.Entities;
using Marr.Data.QGen;

namespace Marr.Data.IntegrationTests.DB_SqlServer
{
    /// <summary>
    /// Contains integration tests for the data mapper.
    /// </summary>
    [TestClass]
    public class SqlServerTests : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            MapRepository.Instance.EnableTraceLogging = true;
        }

        [TestMethod]
        public void TestSql_Insert_Query()
        {
            using (var db = CreateSqlServerDB())
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

                    Order order3 = new Order { OrderName = "Prod3" };
                    db.Insert(order3);

                    var orderItems = new List<OrderItem>();

                    var results = (from o in db.Query<Order>()
                                   where o.OrderName == "Test1"
                                   orderby o.OrderName
                                   select o).ToList();

                    Assert.IsTrue(results.Count == 1);
                    Assert.AreEqual(results[0].OrderName, "Test1");

                    var results2 = (from o in db.Query<Order>()
                                    where o.OrderName.StartsWith("Test")
                                    select o).ToList();

                    Assert.IsTrue(results2.Count == 2);
                    Assert.AreEqual(results2[0].OrderName, "Test1");
                    Assert.AreEqual(results2[1].OrderName, "Test2");
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }

        }

        [TestMethod]
        public void TestJoin()
        {
            using (var db = CreateSqlServerDB())
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
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_GetRowCount()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>(order);
                    }

                    // Row count without a where statement
                    var orders = db.Query<Order>().ToList();
                    Assert.IsTrue(orders.Count == 10);
                    int count = db.Query<Order>().GetRowCount();
                    Assert.AreEqual(orders.Count, count);

                    // Row count with a where statement
                    var filteredOrders = db.Query<Order>().Where(o => o.OrderName == "Order03").ToList();
                    int filteredCount = db.Query<Order>().Where(o => o.OrderName == "Order03").GetRowCount();
                    Assert.AreEqual(filteredOrders.Count, filteredCount);

                    // Row count with paged results
                    var pagedOrders = db.Query<Order>()
                        .Page(1, 2)
                        .OrderBy(o => o.OrderName)
                        .ToList();
                    Assert.AreEqual(2, pagedOrders.Count);
                    int totalCount = db.Query<Order>().GetRowCount();
                    Assert.IsTrue(totalCount > pagedOrders.Count);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_GetRowCount_WithTableJoins()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>()
                            .Entity(order)
                            .GetIdentity()
                            .Execute();

                        Assert.IsTrue(order.ID > 0, "Identity value should have been returned.");

                        OrderItem orderItem1 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc1", Price = 5.5m };
                        OrderItem orderItem2 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc2", Price = 6.6m };
                        db.Insert(orderItem1);
                        db.Insert(orderItem2);
                    }

                    // First verify that there are 20 order item records
                    var orderItems = db.Query<OrderItem>()
                        .From("V_Orders")
                        .ToList();

                    Assert.AreEqual(20, orderItems.Count);

                    // Row count with a join
                    var orders = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)
                        .ToList();
                    Assert.IsTrue(orders.Count == 10);

                    int count = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)
                        .GetRowCount();
                    Assert.AreEqual(orders.Count, count);

                    // Rowcount from a view
                    var orders2 = db.Query<Order>()
                        .From("V_Orders")
                        .Graph(o => o.OrderItems)
                        .ToList();
                    Assert.IsTrue(orders2.Count == 10);

                    int count2 = db.Query<Order>()
                        .From("V_Orders")
                        .Graph(o => o.OrderItems)
                        .GetRowCount();
                    Assert.AreEqual(orders2.Count, count2);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_Simple_Paging_WithNoJoins()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>(order);
                    }

                    // Get page 1 with 2 records
                    var page1 = db.Query<Order>()
                        .OrderBy(o => o.OrderName)
                        .Page(1, 2)
                        .ToList();

                    Assert.AreEqual(2, page1.Count, "Page size should be 2.");
                    Assert.AreEqual("Order01", page1[0].OrderName);
                    Assert.AreEqual("Order02", page1[1].OrderName);

                    // Get page 1 with 2 records
                    var page2 = db.Query<Order>()
                        .OrderBy(o => o.OrderName)
                        .Page(2, 2)
                        .ToList();

                    Assert.AreEqual(2, page2.Count, "Page size should be 2.");
                    Assert.AreEqual("Order03", page2[0].OrderName);
                    Assert.AreEqual("Order04", page2[1].OrderName);
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_Simple_Paging_WithNoJoins_WithWhereClause()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>(order);
                    }

                    // Get page 1 with up to 2 records
                    var page1 = db.Query<Order>()
                        .Where(o => o.OrderName == "Order09")
                        .OrderBy(o => o.OrderName)
                        .Page(1, 2)
                        .ToList();

                    Assert.AreEqual(1, page1.Count, "Page should only have one record.");
                    Assert.AreEqual("Order09", page1[0].OrderName);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_Simple_Paging_WithNoJoins_WithMultipleOrderByClauses()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>(order);
                    }

                    // Get page 1 with up to 2 records
                    var page1 = db.Query<Order>()
                        .Where(o => o.OrderName == "Order09")
                        .OrderBy(o => o.OrderName)
                        .ThenByDescending(o => o.ID)
                        .Page(1, 2)
                        .ToList();

                    Assert.AreEqual(1, page1.Count, "Page should only have one record.");
                    Assert.AreEqual("Order09", page1[0].OrderName);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_Complex_Paging_WithJoins()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>()
                            .Entity(order)
                            .GetIdentity()
                            .Execute();

                        OrderItem orderItem1 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc1", Price = 5.5m };
                        OrderItem orderItem2 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc2", Price = 6.6m };
                        db.Insert(orderItem1);
                        db.Insert(orderItem2);
                    }

                    string query1 = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)
                        .OrderBy(o => o.OrderName)
                        .Page(1, 2)
                        .BuildQuery();

                    Assert.IsNotNull(query1);

                    // Get page 1 with 2 records
                    var page1 = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o,oi) => o.ID == oi.OrderID)
                        .OrderBy(o => o.OrderName)
                        .Page(1, 2)
                        .ToList();

                    Assert.AreEqual(2, page1.Count, "Page size should be 2.");
                    Assert.AreEqual("Order01", page1[0].OrderName);
                    Assert.AreEqual("Order02", page1[1].OrderName);
                    Assert.AreEqual(2, page1[0].OrderItems.Count);

                    // Get page 1 with 2 records
                    var page2 = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)
                        .OrderBy(o => o.OrderName)
                        .Page(2, 2)
                        .ToList();

                    Assert.AreEqual(2, page2.Count, "Page size should be 2.");
                    Assert.AreEqual("Order03", page2[0].OrderName);
                    Assert.AreEqual("Order04", page2[1].OrderName);
                    Assert.AreEqual(2, page2[0].OrderItems.Count);
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_Complex_Paging_WithJoins_WithWhereClause()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>()
                            .Entity(order)
                            .GetIdentity()
                            .Execute();

                        OrderItem orderItem1 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc1", Price = 5.5m };
                        OrderItem orderItem2 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc2", Price = 6.6m };
                        db.Insert(orderItem1);
                        db.Insert(orderItem2);
                    }
                    
                    // Get page 1 with 2 records
                    var page1 = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)
                        .Where(o => o.OrderName == "Order09")
                        .OrderBy(o => o.OrderName)
                        .Page(1, 2)
                        .ToList();

                    Assert.AreEqual(1, page1.Count, "Page size should be 1.");
                    Assert.AreEqual("Order09", page1[0].OrderName);
                    Assert.AreEqual(2, page1[0].OrderItems.Count);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_Complex_Paging_WithJoins_WithWhereClause_WithMultipleOrderClauses()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>()
                            .Entity(order)
                            .GetIdentity()
                            .Execute();

                        OrderItem orderItem1 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc1", Price = 5.5m };
                        OrderItem orderItem2 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc2", Price = 6.6m };
                        db.Insert(orderItem1);
                        db.Insert(orderItem2);
                    }

                    // Get page 1 with 2 records
                    var page1 = db.Query<Order>()
                        .Join<Order, OrderItem>(JoinType.Left, o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)
                        .Where(o => o.OrderName == "Order09")
                        .OrderBy(o => o.OrderName)
                        .ThenByDescending(o => o.ID)
                        .Page(1, 2)
                        .ToList();

                    Assert.AreEqual(1, page1.Count, "Page size should be 1.");
                    Assert.AreEqual("Order09", page1[0].OrderName);
                    Assert.AreEqual(2, page1[0].OrderItems.Count);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_Paging_With_View()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>()
                            .Entity(order)
                            .GetIdentity()
                            .Execute();

                        Assert.IsTrue(order.ID > 0, "Identity value should have been returned.");

                        OrderItem orderItem1 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc1", Price = 5.5m };
                        OrderItem orderItem2 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc2", Price = 6.6m };
                        db.Insert(orderItem1);
                        db.Insert(orderItem2);
                    }

                    db.SqlMode = SqlModes.Text;

                    // Get page 1 with up to 2 records
                    var page1 = db.Query<Order>()
                        .From("V_Orders")
                        .Graph(o => o.OrderItems)
                        .OrderBy(o => o.OrderName)
                        .Page(1, 2)
                        .ToList();

                    Assert.AreEqual(2, page1.Count, "Page should have two records.");
                    Assert.AreEqual(2, page1[0].OrderItems.Count);
                    Assert.AreEqual(2, page1[1].OrderItems.Count);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void TestQueryBuilders()
        {
            using (var db = CreateSqlServerDB())
            {
                try
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
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }

        [TestMethod]
        public void Test_Filtering_ChildrenToload_With_Graph()
        {
            using (var db = CreateSqlServerDB())
            {
                try
                {
                    db.SqlMode = SqlModes.Text;
                    db.BeginTransaction();

                    // Insert 10 orders
                    for (int i = 1; i < 11; i++)
                    {
                        Order order = new Order { OrderName = "Order" + (i.ToString().PadLeft(2, '0')) };
                        db.Insert<Order>()
                            .Entity(order)
                            .GetIdentity()
                            .Execute();

                        OrderItem orderItem1 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc1", Price = 5.5m };
                        OrderItem orderItem2 = new OrderItem { OrderID = order.ID, ItemDescription = "Desc2", Price = 6.6m };
                        db.Insert<OrderItem>().Entity(orderItem1).GetIdentity().Execute();
                        db.Insert<OrderItem>().Entity(orderItem2).GetIdentity().Execute();

                        Receipt receipt1 = new Receipt { OrderItemID = orderItem1.ID, AmountPaid = orderItem1.Price };
                        Receipt receipt2 = new Receipt { OrderItemID = orderItem2.ID, AmountPaid = orderItem2.Price };
                        db.Insert<Receipt>(receipt1);
                        db.Insert<Receipt>(receipt2);
                    }
                    
                    // Try to get entire graph, including orderitems and receipts
                    var orders = db.Query<Order>().From("V_OrdersReceipts").Graph(o => o.OrderItems, o => o.OrderItems[0].ItemReceipt).ToList();
                    Assert.IsTrue(orders.Count > 0);
                    Assert.IsNotNull(orders[0].OrderItems);
                    Assert.IsNotNull(orders[0].OrderItems[0].ItemReceipt);

                    // Try to get filtered graph without receipt
                    var orders2 = db.Query<Order>().From("V_OrdersReceipts").Graph(o => o.OrderItems).ToList();
                    Assert.IsTrue(orders2.Count > 0);
                    Assert.IsNotNull(orders2[0].OrderItems);
                    Assert.IsNull(orders2[0].OrderItems[0].ItemReceipt);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    db.RollBack();
                }
            }
        }
    }
}
