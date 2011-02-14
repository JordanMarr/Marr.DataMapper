using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Rhino.Mocks;
using Marr.Data.QGen;
using Marr.Data.Tests.Entities;

namespace Marr.Data.Tests
{
    [TestClass]
    public class AutoQueryBuilderTest : TestBase
    {
        [TestMethod]
        public void Complex_Where_Query_No_Sort()
        {
            // Arrange
            IDataMapper db = MockRepository.GenerateMock<IDataMapper>();
            DbCommand cmd = new System.Data.SqlClient.SqlCommand();
            db.Expect(d => d.Command).Return(cmd).Repeat.Any();

            AutoQueryBuilder<Person> builder = new AutoQueryBuilder<Person>(db, "PersonTable");

            // Act
            List<Person> people = builder.Where(p => p.Age > 16 && p.Name.StartsWith("J"));

            // Assert
            var args = db.GetArgumentsForCallsMadeOn(d => d.Query<Person>("sql.."));
            string generatedSql = (string)args[0][0];

            Assert.IsTrue(generatedSql.Contains("SELECT [ID],[Name],[Age],[BirthDate],[IsHappy] "));
            Assert.IsTrue(generatedSql.Contains("FROM PersonTable"));
            Assert.IsTrue(generatedSql.Contains("(([Age] > @P0 AND [Name] LIKE @P1 + '%'))"));
            Assert.IsFalse(generatedSql.Contains("ORDER BY"));
        }

        [TestMethod]
        public void Sort_Only_Query_No_Where()
        {
            // Arrange
            IDataMapper db = MockRepository.GenerateMock<IDataMapper>();
            DbCommand cmd = new System.Data.SqlClient.SqlCommand();
            db.Expect(d => d.Command).Return(cmd).Repeat.Any();

            AutoQueryBuilder<Person> builder = new AutoQueryBuilder<Person>(db, "PersonTable");

            // Act
            List<Person> people = builder.Order(p => p.ID).Order(p => p.Name);

            // Assert
            var args = db.GetArgumentsForCallsMadeOn(d => d.Query<Person>("sql.."));
            string generatedSql = (string)args[0][0];

            Assert.IsTrue(generatedSql.Contains("SELECT [ID],[Name],[Age],[BirthDate],[IsHappy] "));
            Assert.IsTrue(generatedSql.Contains("FROM PersonTable"));
            Assert.IsFalse(generatedSql.Contains("WHERE"));
            Assert.IsTrue(generatedSql.Contains("ORDER BY [ID],[Name]"));
        }

        [TestMethod]
        public void Complex_Where_Sort_Query()
        {
            // Arrange
            IDataMapper db = MockRepository.GenerateMock<IDataMapper>();
            DbCommand cmd = new System.Data.SqlClient.SqlCommand();
            db.Expect(d => d.Command).Return(cmd).Repeat.Any();

            AutoQueryBuilder<Person> builder = new AutoQueryBuilder<Person>(db, "PersonTable");

            // Act
            List<Person> people = builder
                .Where(p => p.Age > 16 && p.Name.StartsWith("J"))
                .Order(p => p.Name)
                .OrderDesc(p => p.ID);

            // Assert
            var args = db.GetArgumentsForCallsMadeOn(d => d.Query<Person>("sql.."));
            string generatedSql = (string)args[0][0];

            Assert.IsTrue(generatedSql.Contains("SELECT [ID],[Name],[Age],[BirthDate],[IsHappy] "));
            Assert.IsTrue(generatedSql.Contains("FROM PersonTable"));
            Assert.IsTrue(generatedSql.Contains("(([Age] > @P0 AND [Name] LIKE @P1 + '%'))"));
            Assert.IsTrue(generatedSql.Contains("ORDER BY [Name],[ID] DESC"));
        }

    }
}
