using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.UnitTests.Entities;

namespace Marr.Data.UnitTests
{
    [TestClass]
    public class EntityMergerTest
    {
        [TestMethod]
        public void Should_Merge_Related_Children_To_The_Correct_Location()
        {
            // Arrange
            List<Order> orders = new List<Order>();
            orders.Add(new Order { ID = 1, OrderName = "Test1", OrderItems = new List<OrderItem>() });
            orders.Add(new Order { ID = 2, OrderName = "Test2", OrderItems = new List<OrderItem>() });
            orders.Add(new Order { ID = 3, OrderName = "Test3", OrderItems = new List<OrderItem>() });

            List<OrderItem> items = new List<OrderItem>();
            items.Add(new OrderItem { OrderID = 2, ItemDescription = "Item1" });
            items.Add(new OrderItem { OrderID = 2, ItemDescription = "Item2" });
            items.Add(new OrderItem { OrderID = 3, ItemDescription = "Item3" });
            items.Add(new OrderItem { OrderID = 3, ItemDescription = "Item4" });

            // Act
            EntityMerger.Merge(orders, items, (o, oi) => o.ID == oi.OrderID, (o, oi) => o.OrderItems.Add(oi));

            // Assert
            Assert.AreEqual(0, orders[0].OrderItems.Count);
            Assert.AreEqual(2, orders[1].OrderItems.Count);
            Assert.AreEqual(2, orders[2].OrderItems.Count);

            Assert.AreEqual("Item1", orders[1].OrderItems[0].ItemDescription);
            Assert.AreEqual("Item2", orders[1].OrderItems[1].ItemDescription);
            Assert.AreEqual("Item3", orders[2].OrderItems[0].ItemDescription);
            Assert.AreEqual("Item4", orders[2].OrderItems[1].ItemDescription);
        }
    }
}
