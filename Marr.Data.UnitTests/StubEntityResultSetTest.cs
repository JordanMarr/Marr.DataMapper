using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.UnitTests;
using Marr.Data.TestHelper;
using Marr.Data.UnitTests.Entities;

namespace Marr.Data.Tests
{
    [TestClass]
    public class StubEntityResultSetTest : TestBase
    {
        [TestInitialize]
        public void Init()
        {
            InitMappings();
        }

        [TestMethod]
        public void SingleEntityTest()
        {
            StubEntityResultSet rs = new StubEntityResultSet();

            rs.AddEntity(new Order { ID = 1, OrderName = "Name1" });
            rs.AddEntity(new Order { ID = 2, OrderName = "Name2" });

            var db = CreateDB_ForQuery(rs);

            List<Order> orders = db.Query<Order>().ToList();

            Assert.AreEqual(2, orders.Count);
            Assert.AreEqual("Name1", orders[0].OrderName);
            Assert.AreEqual(1, orders[0].ID);
            Assert.AreEqual("Name2", orders[1].OrderName);
            Assert.AreEqual(2, orders[1].ID);
        }

        [TestMethod]
        public void ParentChildEntityTest()
        {
            StubEntityResultSet rs = new StubEntityResultSet();
            
            rs.AddEntityWithChildren(
                new Order { ID = 1, OrderName = "Name11" },
                new OrderItem { ID = 1001, ItemDescription = "Desc11", OrderID = 1, Price = 1.00m }
            );

            rs.AddEntityWithChildren(
                new Order { ID = 1, OrderName = "Name11" },
                new OrderItem { ID = 1002, ItemDescription = "Desc12", OrderID = 1, Price = 1.00m }
            );
            
            rs.AddEntityWithChildren(
                new Order { ID = 2, OrderName = "Name21" },
                new OrderItem { ID = 2001, ItemDescription = "Desc21", OrderID = 2, Price = 2.00m }
            );

            var db = CreateDB_ForQuery(rs);

            List<Order> orders = db.Query<Order>().Graph(o => o.OrderItems).ToList();

            // 1st order
            Assert.AreEqual(2, orders.Count);
            Assert.AreEqual("Name11", orders[0].OrderName);
            Assert.AreEqual(1, orders[0].ID);

            // 1st order's items
            Assert.AreEqual(2, orders[0].OrderItems.Count);

            // 1st orders item 1
            Assert.AreEqual(1001, orders[0].OrderItems[0].ID);
            Assert.AreEqual("Desc11", orders[0].OrderItems[0].ItemDescription);
            Assert.AreEqual(1, orders[0].OrderItems[0].OrderID);
            Assert.AreEqual(1.00m, orders[0].OrderItems[0].Price);

            // 1st orders item 2
            Assert.AreEqual(1002, orders[0].OrderItems[1].ID);
            Assert.AreEqual("Desc12", orders[0].OrderItems[1].ItemDescription);
            Assert.AreEqual(1, orders[0].OrderItems[1].OrderID);
            Assert.AreEqual(1.00m, orders[0].OrderItems[1].Price);

            // 2nd order
            Assert.AreEqual("Name21", orders[1].OrderName);
            Assert.AreEqual(2, orders[1].ID);

            // 2nd order's items
            Assert.AreEqual(1, orders[1].OrderItems.Count);
            Assert.AreEqual(2001, orders[1].OrderItems[0].ID);
            Assert.AreEqual("Desc21", orders[1].OrderItems[0].ItemDescription);
            Assert.AreEqual(2, orders[1].OrderItems[0].OrderID);
            Assert.AreEqual(2.00m, orders[1].OrderItems[0].Price);
        }

        [TestMethod]
        public void ParentChildChildEntityTest()
        {
            StubEntityResultSet rs = new StubEntityResultSet();

            rs.AddEntityWithChildren(
                new Order { ID = 1, OrderName = "Name11" },
                new OrderItem { ID = 1001, ItemDescription = "Desc11", OrderID = 1, Price = 1.00m },
                new Receipt { AmountPaid = 1.00m }
            );

            rs.AddEntityWithChildren(
                new Order { ID = 1, OrderName = "Name11" },
                new OrderItem { ID = 1002, ItemDescription = "Desc12", OrderID = 1, Price = 2.00m },
                new Receipt { AmountPaid = 2.00m }
            );

            rs.AddEntityWithChildren(
                new Order { ID = 2, OrderName = "Name21" },
                new OrderItem { ID = 2001, ItemDescription = "Desc21", OrderID = 2, Price = 3.00m },
                new Receipt { AmountPaid = 3.00m }
            );

            var db = CreateDB_ForQuery(rs);

            List<Order> orders = db.Query<Order>().Graph().ToList();

            // 1st order
            Assert.AreEqual(2, orders.Count);
            Assert.AreEqual("Name11", orders[0].OrderName);
            Assert.AreEqual(1, orders[0].ID);

            // 1st order's items
            Assert.AreEqual(2, orders[0].OrderItems.Count);

            // 1st orders item 1
            Assert.AreEqual(1001, orders[0].OrderItems[0].ID);
            Assert.AreEqual("Desc11", orders[0].OrderItems[0].ItemDescription);
            Assert.AreEqual(1, orders[0].OrderItems[0].OrderID);
            Assert.AreEqual(1.00m, orders[0].OrderItems[0].Price);
            // Receipt 1
            Assert.IsNotNull(orders[0].OrderItems[0].ItemReceipt);
            Assert.AreEqual(1.00m, orders[0].OrderItems[0].ItemReceipt.AmountPaid);

            // 1st orders item 2
            Assert.AreEqual(1002, orders[0].OrderItems[1].ID);
            Assert.AreEqual("Desc12", orders[0].OrderItems[1].ItemDescription);
            Assert.AreEqual(1, orders[0].OrderItems[1].OrderID);
            Assert.AreEqual(2.00m, orders[0].OrderItems[1].Price);
            // Receipt 2
            Assert.IsNotNull(orders[0].OrderItems[1].ItemReceipt);
            Assert.AreEqual(2.00m, orders[0].OrderItems[1].ItemReceipt.AmountPaid);

            // 2nd order
            Assert.AreEqual("Name21", orders[1].OrderName);
            Assert.AreEqual(2, orders[1].ID);

            // 2nd order's items
            Assert.AreEqual(1, orders[1].OrderItems.Count);
            Assert.AreEqual(2001, orders[1].OrderItems[0].ID);
            Assert.AreEqual("Desc21", orders[1].OrderItems[0].ItemDescription);
            Assert.AreEqual(2, orders[1].OrderItems[0].OrderID);
            Assert.AreEqual(3.00m, orders[1].OrderItems[0].Price);
            // Receipt 3
            Assert.IsNotNull(orders[1].OrderItems[0].ItemReceipt);
            Assert.AreEqual(3.00m, orders[1].OrderItems[0].ItemReceipt.AmountPaid);
        }
    }
}
