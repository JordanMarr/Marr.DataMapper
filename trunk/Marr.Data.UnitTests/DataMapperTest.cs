using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Rhino.Mocks;
using Marr.Data.UnitTests.Entities;

namespace Marr.Data.UnitTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class DataMapperTest : TestBase
    {
        [TestMethod]
        public void Find_ShouldMapToEntity()
        {
            // Arrange
            StubResultSet rs = new StubResultSet("ID", "Name", "Age", "IsHappy", "BirthDate");
            rs.AddRow(1, "Jordan", 33, true, new DateTime(1977, 1, 22));

            // Act
            var db = CreateDB_ForQuery(rs);
            Person person = db.Find<Person>("sql...");

            // Assert
            Assert.IsNotNull(person);

            Assert.AreEqual(1, person.ID);
            Assert.AreEqual("Jordan", person.Name);
            Assert.AreEqual(33, person.Age);
            Assert.AreEqual(true, person.IsHappy);
            Assert.AreEqual(new DateTime(1977, 1, 22), person.BirthDate);
        }

        [TestMethod]
        public void Find_WithNoRows_ShouldReturnNull()
        {
            // Arrange
            StubResultSet rs = new StubResultSet("ID", "Name", "Age", "IsHappy", "BirthDate");

            // Act
            var db = CreateDB_ForQuery(rs);
            Person person = db.Find<Person>("sql...");

            // Assert
            Assert.IsNull(person);
        }

        [TestMethod]
        public void Find_WithNoRows_PassingInObject_ShouldReturnObject()
        {
            // Arrange
            StubResultSet rs = new StubResultSet("ID", "Name", "Age", "IsHappy", "BirthDate");

            // Act
            var db = CreateDB_ForQuery(rs);
            Person person = new Person { ID = 5 };
            db.Find<Person>("sql...", person);

            // Assert
            Assert.IsNotNull(person);
            Assert.AreEqual(5, person.ID);
        }

        [TestMethod]
        public void Query_ShouldMapToList()
        {
            // Arrange
            StubResultSet rs = new StubResultSet("ID", "Name", "Age", "IsHappy", "BirthDate");
            rs.AddRow(1, "Jordan", 33, true, new DateTime(1977, 1, 22));
            rs.AddRow(2, "Amyme", 31, false, new DateTime(1979, 10, 19));

            // Act
            var db = CreateDB_ForQuery(rs);
            var people = db.Query<Person>("sql...");

            // Assert
            Assert.IsTrue(people.Count == 2);

            Person jordan = people[0];
            Assert.AreEqual(1, jordan.ID);
            Assert.AreEqual("Jordan", jordan.Name);
            Assert.AreEqual(33, jordan.Age);
            Assert.AreEqual(true, jordan.IsHappy);
            Assert.AreEqual(new DateTime(1977, 1, 22), jordan.BirthDate);

            Person amyme = people[1];
            Assert.AreEqual(2, amyme.ID);
            Assert.AreEqual("Amyme", amyme.Name);
            Assert.AreEqual(31, amyme.Age);
            Assert.AreEqual(false, amyme.IsHappy);
            Assert.AreEqual(new DateTime(1979, 10, 19), amyme.BirthDate);
        }

        [TestMethod]
        public void QueryToGraph_WithNestedRelationships_ShouldMapToGraph()
        {
            // Arrange
            StubResultSet rs = new StubResultSet("ID", "OrderName", "OrderItemID", "OrderID", "ItemDescription", "Price", "AmountPaid");
            rs.AddRow(1, "Order1", 50, 1, "Red car", 100.35m, DBNull.Value);
            rs.AddRow(1, "Order1", 51, 1, "Blue wagon", 44.87m, DBNull.Value);
            rs.AddRow(2, "Order2", 60, 2, "Guitar", 1500.50m, 1500.50m);
            rs.AddRow(2, "Order2", 61, 3, "Bass", 2380.00m, 50.00m);
            rs.AddRow(3, "Order3", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
            
            // Act
            var db = CreateDB_ForQuery(rs);
            List<Order> orders = db.Query<Order>().Graph().QueryText("sql...");

            // Assert
            Assert.IsTrue(orders.Count == 3);
            Order order1 = orders[0];
            Order order2 = orders[1];
            Order order3 = orders[2];
            Assert.IsTrue(order1.OrderItems.Count == 2);
            Assert.IsTrue(order2.OrderItems.Count == 2);
            Assert.IsTrue(order3.OrderItems.Count == 0);

            // Order 1
            Assert.AreEqual(1, order1.ID);
            Assert.AreEqual("Order1", order1.OrderName);

            // Order 1 -> Item 1
            Assert.AreEqual(50, order1.OrderItems[0].ID);
            Assert.AreEqual("Red car", order1.OrderItems[0].ItemDescription);
            Assert.AreEqual(100.35m, order1.OrderItems[0].Price);
            Assert.IsNull(order1.OrderItems[0].ItemReceipt.AmountPaid);

            // Order 1 -> Item 2
            Assert.AreEqual(51, order1.OrderItems[1].ID);
            Assert.AreEqual("Blue wagon", order1.OrderItems[1].ItemDescription);
            Assert.AreEqual(44.87m, order1.OrderItems[1].Price);
            Assert.IsNull(order1.OrderItems[1].ItemReceipt.AmountPaid);

            // Order 2 -> Item 1
            Assert.AreEqual(60, order2.OrderItems[0].ID);
            Assert.AreEqual("Guitar", order2.OrderItems[0].ItemDescription);
            Assert.AreEqual(1500.50m, order2.OrderItems[0].Price);
            Assert.AreEqual(1500.50m, order2.OrderItems[0].ItemReceipt.AmountPaid);

            // Order 2 -> Item 2
            Assert.AreEqual(61, order2.OrderItems[1].ID);
            Assert.AreEqual("Bass", order2.OrderItems[1].ItemDescription);
            Assert.AreEqual(2380.00m, order2.OrderItems[1].Price);
            Assert.AreEqual(50.00m, order2.OrderItems[1].ItemReceipt.AmountPaid);
        }

        [TestMethod]
        public void QueryToGraph_WithNestedRelationships_UnsortedResults_ShouldMapToGraph()
        {
            // Arrange
            StubResultSet rs = new StubResultSet("ID", "OrderName", "OrderItemID", "OrderID", "ItemDescription", "Price", "AmountPaid");

            // For this test, results are purposefully out of order
            rs.AddRow(3, "Order3", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
            rs.AddRow(1, "Order1", 50, 1, "Red car", 100.35m, DBNull.Value);
            rs.AddRow(2, "Order2", 60, 2, "Guitar", 1500.50m, 1500.50m);
            rs.AddRow(1, "Order1", 51, 1, "Blue wagon", 44.87m, DBNull.Value);
            rs.AddRow(2, "Order2", 61, 2, "Bass", 2380.00m, 50.00m);

            // Act
            var db = CreateDB_ForQuery(rs);
            List<Order> orders = db.Query<Order>().Graph().QueryText("sql...");

            // Assert
            Assert.IsTrue(orders.Count == 3);
            // NOTE: Cannot assume that orders are sorted, so get by OrderName to verify children
            Order order1 = orders.Where(o => o.OrderName == "Order1").FirstOrDefault();
            Order order2 = orders.Where(o => o.OrderName == "Order2").FirstOrDefault();
            Order order3 = orders.Where(o => o.OrderName == "Order3").FirstOrDefault();
            Assert.IsTrue(order1.OrderItems.Count == 2);
            Assert.IsTrue(order2.OrderItems.Count == 2);
            Assert.IsTrue(order3.OrderItems.Count == 0);

            // Order 1
            Assert.AreEqual(1, order1.ID);
            Assert.AreEqual("Order1", order1.OrderName);

            // Order 1 -> Item 1
            Assert.AreEqual(50, order1.OrderItems[0].ID);
            Assert.AreEqual("Red car", order1.OrderItems[0].ItemDescription);
            Assert.AreEqual(100.35m, order1.OrderItems[0].Price);
            Assert.IsNull(order1.OrderItems[0].ItemReceipt.AmountPaid);

            // Order 1 -> Item 2
            Assert.AreEqual(51, order1.OrderItems[1].ID);
            Assert.AreEqual("Blue wagon", order1.OrderItems[1].ItemDescription);
            Assert.AreEqual(44.87m, order1.OrderItems[1].Price);
            Assert.IsNull(order1.OrderItems[1].ItemReceipt.AmountPaid);

            // Order 2 -> Item 1
            Assert.AreEqual(60, order2.OrderItems[0].ID);
            Assert.AreEqual("Guitar", order2.OrderItems[0].ItemDescription);
            Assert.AreEqual(1500.50m, order2.OrderItems[0].Price);
            Assert.AreEqual(1500.50m, order2.OrderItems[0].ItemReceipt.AmountPaid);

            // Order 2 -> Item 2
            Assert.AreEqual(61, order2.OrderItems[1].ID);
            Assert.AreEqual("Bass", order2.OrderItems[1].ItemDescription);
            Assert.AreEqual(2380.00m, order2.OrderItems[1].Price);
            Assert.AreEqual(50.00m, order2.OrderItems[1].ItemReceipt.AmountPaid);
        }

        [TestMethod]
        public void Update_ShouldAddFourParameters_And_ExecuteNonQuery()
        {
            // Arrange
            Person person = new Person();
            person.ID = 1;
            person.Name = "Jordan";
            person.Age = 33;
            person.IsHappy = true;
            person.BirthDate = new DateTime(1977, 1, 22);

            var db = CreateDB_ForUpdate();
            db.Command.Parameters
                .Expect(p => p.Add(null))
                .IgnoreArguments()
                .Repeat.Times(4)
                .Return(0);

            // Act
            db.Update<Person>(person, "sql...");

            // Assert
            db.Command.Parameters.VerifyAllExpectations();
            db.Command.VerifyAllExpectations();
        }

        [TestMethod]
        public void Insert_ShouldAddFiveParameters_And_ExecuteScalar_AndSetReturnValue()
        {
            // Arrange
            Person person = new Person();
            person.Name = "Jordan";
            person.Age = 33;
            person.IsHappy = true;
            person.BirthDate = new DateTime(1977, 1, 22);

            var db = CreateDB_ForInsert();
            db.Command
                .Expect(c => c.ExecuteScalar())
                .Return(55);

            db.Command.Parameters
                .Expect(p => p.Add(null))
                .IgnoreArguments()
                .Repeat.Times(4)
                .Return(0);

            // Act
            db.Insert<Person>(person, "sql...");

            // Assert
            db.Command.Parameters.VerifyAllExpectations();
            db.Command.VerifyAllExpectations();
            Assert.AreEqual(55, person.ID);
        }

        [TestMethod]
        public void QueryToGraph_ShouldMapToList()
        {
            // Arrange
            StubResultSet rs = new StubResultSet("ID", "Name", "Age", "IsHappy", "BirthDate", "Pet_ID", "Pet_Name");
            rs.AddRow(1, "Jordan", 33, true, new DateTime(1977, 1, 22), 1, "Chuy");
            rs.AddRow(1, "Jordan", 33, true, new DateTime(1977, 1, 22), 2, "Bela");
            rs.AddRow(2, "Amyme", 31, false, new DateTime(1979, 10, 19), 3, "Bird");
            rs.AddRow(2, "Amyme", 31, false, new DateTime(1979, 10, 19), 4, "Alligator");

            // Act
            var db = CreateDB_ForQuery(rs);
            var people = db.Query<Person>().Graph().QueryText("sql...").ToList();

            // Assert
            Assert.IsTrue(people.Count == 2);

            Person jordan = people[0];
            Assert.AreEqual(1, jordan.ID);
            Assert.AreEqual("Jordan", jordan.Name);
            Assert.AreEqual(33, jordan.Age);
            Assert.AreEqual(true, jordan.IsHappy);
            Assert.AreEqual(new DateTime(1977, 1, 22), jordan.BirthDate);
            Assert.IsTrue(jordan.Pets.Count == 2);
            Assert.AreEqual("Chuy", jordan.Pets[0].Name);
            Assert.AreEqual("Bela", jordan.Pets[1].Name);

            Person amyme = people[1];
            Assert.AreEqual(2, amyme.ID);
            Assert.AreEqual("Amyme", amyme.Name);
            Assert.AreEqual(31, amyme.Age);
            Assert.AreEqual(false, amyme.IsHappy);
            Assert.AreEqual(new DateTime(1979, 10, 19), amyme.BirthDate);
            Assert.IsTrue(amyme.Pets.Count == 2);
            Assert.AreEqual("Bird", amyme.Pets[0].Name);
            Assert.AreEqual("Alligator", amyme.Pets[1].Name);
        }

        //[TestMethod]
        public void MultiEntityTest()
        {
            // Arrange
            StubResultSet rs = new StubResultSet("ID", "OrderName", "OrderItemID", "ItemDescription", "Price", "AmountPaid");
            rs.AddRow(1, "Order1", 50, "Red car", 100.35m, DBNull.Value);
            rs.AddRow(1, "Order1", 51, "Blue wagon", 44.87m, DBNull.Value);
            rs.AddRow(2, "Order2", 60, "Guitar", 1500.50m, 1500.50m);
            rs.AddRow(2, "Order2", 61, "Bass", 2380.00m, 50.00m);
            rs.AddRow(3, "Order3", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);

            // Act
            var db = CreateDB_ForQuery(rs);
            List<Order> people = db
                .Query<Order>()
                .Graph(o => o.OrderItems);
        }
    }
}
