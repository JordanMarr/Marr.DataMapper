using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Rhino.Mocks;
using Marr.Data.QGen;
using Marr.Data.UnitTests.Entities;
using Marr.Data.QGen.Dialects;

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
            QueryBuilder<Person> builder = new QueryBuilder<Person>(db, new SqlServerDialect());

            // Act
            builder.Where(p => p.Age > 16 && p.Name.StartsWith("J"));
            
            // Assert
            builder.BuildQuery();
            string generatedSql = builder._queryText;

            Assert.IsTrue(generatedSql.Contains("SELECT [t0].[ID],[t0].[Name],[t0].[Age],[t0].[BirthDate],[t0].[IsHappy] "));
            Assert.IsTrue(generatedSql.Contains("FROM [PersonTable] [t0]"));
            Assert.IsTrue(generatedSql.Contains("((([t0].[Age] > @P0) AND [t0].[Name] LIKE @P1 + '%'))"));
            Assert.IsFalse(generatedSql.Contains("ORDER BY"));
        }

        [TestMethod]
        public void Sort_Only_Query_No_Where()
        {
            // Arrange
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=a;Initial Catalog=a;User Id=a;Password=a;");
            QueryBuilder<Person> builder = new QueryBuilder<Person>(db, new SqlServerDialect());

            // Act
            builder.OrderBy(p => p.ID).OrderBy(p => p.Name);

            // Assert
            builder.BuildQuery();
            string generatedSql = builder._queryText;

            Assert.IsTrue(generatedSql.Contains("SELECT [t0].[ID],[t0].[Name],[t0].[Age],[t0].[BirthDate],[t0].[IsHappy] "));
            Assert.IsTrue(generatedSql.Contains("FROM [PersonTable]"));
            Assert.IsFalse(generatedSql.Contains("WHERE"));
            Assert.IsTrue(generatedSql.Contains("ORDER BY [t0].[ID],[t0].[Name]"));
        }

        [TestMethod]
        public void Complex_Where_Sort_Query()
        {
            // Arrange
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=a;Initial Catalog=a;User Id=a;Password=a;");
            QueryBuilder<Person> builder = new QueryBuilder<Person>(db, new SqlServerDialect());

            // Act
            builder
                .Where(p => p.Age > 16 && p.Name.StartsWith("J"))
                .OrderBy(p => p.Name)
                .OrderByDescending(p => p.ID);

            // Assert
            builder.BuildQuery();
            string generatedSql = builder._queryText;

            Assert.IsTrue(generatedSql.Contains("SELECT [t0].[ID],[t0].[Name],[t0].[Age],[t0].[BirthDate],[t0].[IsHappy] "));
            Assert.IsTrue(generatedSql.Contains("FROM [PersonTable] [t0]"));
            Assert.IsTrue(generatedSql.Contains("((([t0].[Age] > @P0) AND [t0].[Name] LIKE @P1 + '%'))"));
            Assert.IsTrue(generatedSql.Contains("ORDER BY [t0].[Name],[t0].[ID] DESC"));
            Assert.AreEqual(db.Command.Parameters.Count, 2);
        }

        [TestMethod]
        public void Where_Sort_Query_Should_Use_AltName()
        {
            // Arrange
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=a;Initial Catalog=a;User Id=a;Password=a;");
            QueryBuilder<Order> builder = new QueryBuilder<Order>(db, new SqlServerDialect());
            
            // Act
            builder
                .Join<Order, OrderItem>(JoinType.Left, (o, oi) => o.ID == oi.OrderID)
                .Where<OrderItem>(oi => oi.OrderID > 0)
                .OrderBy(o => o.OrderItems[0].ID);

            // Assert
            builder.BuildQuery();
            string generatedSql = builder._queryText;

            Assert.IsTrue(generatedSql.Contains("SELECT [t0].[ID],[t0].[OrderName],[t1].[ID] AS OrderItemID,[t1].[OrderID],[t1].[ItemDescription],[t1].[Price] "));
            Assert.IsTrue(generatedSql.Contains("FROM [Order] [t0] LEFT JOIN [OrderItem] [t1]"));
            Assert.IsTrue(generatedSql.Contains("([t1].[OrderID] > @P0)"));
            Assert.IsTrue(generatedSql.Contains("ORDER BY [t1].[OrderItemID]"));
        }

    }
}
