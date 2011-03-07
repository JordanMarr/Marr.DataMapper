using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Rhino.Mocks;
using Marr.Data.QGen;
using Marr.Data.UnitTests.Entities;

namespace Marr.Data.UnitTests
{
    [TestClass]
    public class AutoAutoQueryBuilderTest : TestBase
    {
        [TestMethod]
        public void Complex_Where_Query_No_Sort()
        {
            // Arrange
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=a;Initial Catalog=a;User Id=a;Password=a;");
            QueryBuilder<Person> builder = new QueryBuilder<Person>(db);

            // Act
            builder.Where(p => p.Age > 16 && p.Name.StartsWith("J"));
            
            // Assert
            builder.BuildQuery();
            string generatedSql = builder._queryText;

            Assert.IsTrue(generatedSql.Contains("SELECT ID,Name,Age,BirthDate,IsHappy "));
            Assert.IsTrue(generatedSql.Contains("FROM PersonTable"));
            Assert.IsTrue(generatedSql.Contains("((Age > @P0 AND Name LIKE @P1 + '%'))"));
            Assert.IsFalse(generatedSql.Contains("ORDER BY"));
        }

        [TestMethod]
        public void Sort_Only_Query_No_Where()
        {
            // Arrange
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=a;Initial Catalog=a;User Id=a;Password=a;");
            QueryBuilder<Person> builder = new QueryBuilder<Person>(db);

            // Act
            builder.OrderBy(p => p.ID).OrderBy(p => p.Name);

            // Assert
            builder.BuildQuery();
            string generatedSql = builder._queryText;

            Assert.IsTrue(generatedSql.Contains("SELECT ID,Name,Age,BirthDate,IsHappy "));
            Assert.IsTrue(generatedSql.Contains("FROM PersonTable"));
            Assert.IsFalse(generatedSql.Contains("WHERE"));
            Assert.IsTrue(generatedSql.Contains("ORDER BY ID,Name"));
        }

        [TestMethod]
        public void Complex_Where_Sort_Query()
        {
            // Arrange
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=a;Initial Catalog=a;User Id=a;Password=a;");
            QueryBuilder<Person> builder = new QueryBuilder<Person>(db);

            // Act
            builder
                .Where(p => p.Age > 16 && p.Name.StartsWith("J"))
                .OrderBy(p => p.Name)
                .OrderByDescending(p => p.ID);

            // Assert
            builder.BuildQuery();
            string generatedSql = builder._queryText;

            Assert.IsTrue(generatedSql.Contains("SELECT ID,Name,Age,BirthDate,IsHappy "));
            Assert.IsTrue(generatedSql.Contains("FROM PersonTable"));
            Assert.IsTrue(generatedSql.Contains("((Age > @P0 AND Name LIKE @P1 + '%'))"));
            Assert.IsTrue(generatedSql.Contains("ORDER BY Name,ID DESC"));
        }

        [TestMethod]
        public void Where_Sort_Query_Should_Use_AltName()
        {
            // Arrange
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=a;Initial Catalog=a;User Id=a;Password=a;");
            QueryBuilder<Order> builder = new QueryBuilder<Order>(db);
            builder.Graph();
            
            // Act
            builder
                .Where(o => o.OrderItems[0].ID > 0)
                .OrderBy(o => o.OrderItems[0].ID);

            // Assert
            builder.BuildQuery();
            string generatedSql = builder._queryText;

            Assert.IsTrue(generatedSql.Contains("SELECT ID,OrderName,OrderItemID,ItemDescription,Price,AmountPaid "));
            Assert.IsTrue(generatedSql.Contains("FROM Order"));
            Assert.IsTrue(generatedSql.Contains("(OrderItemID > @P0)"));
            Assert.IsTrue(generatedSql.Contains("ORDER BY OrderItemID"));
        }

    }
}
