using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.QGen;
using Marr.Data.Tests.Entities;
using Rhino.Mocks;
using Marr.Data.Mapping;

namespace Marr.Data.Tests
{
    /// <summary>
    /// Summary description for QGenTest
    /// </summary>
    [TestClass]
    public class QGenTest : TestBase
    {
        [TestMethod]
        public void SqlServerUpdateQuery_ShouldGenQuery()
        {
            // Arrange
            var command = new System.Data.SqlClient.SqlCommand();
            ColumnMapCollection columns = MapRepository.Instance.GetColumns(typeof(Person));
            MappingHelper mappingHelper = new MappingHelper(command);

            Person person = new Person();
            person.ID = 1;
            person.Name = "Jordan";
            person.Age = 33;
            person.IsHappy = true;
            person.BirthDate = new DateTime(1977, 1, 22);

            mappingHelper.CreateParameters<Person>(person, columns, false, true);

            IQuery query = new SqlServerUpdateQuery(columns, command.Parameters, "dbo", "People");

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.IsTrue(queryText.Contains("UPDATE [dbo].[People]"));
            Assert.IsTrue(queryText.Contains("[Name]"));
            Assert.IsTrue(queryText.Contains("[Age]"));
            Assert.IsTrue(queryText.Contains("[IsHappy]"));
            Assert.IsTrue(queryText.Contains("[BirthDate]"));
            Assert.IsTrue(queryText.Contains("WHERE [ID]=@ID"));
        }

        [TestMethod]
        public void SqlServerInsertQuery_ShouldGenQuery()
        {
            // Arrange
            var command = new System.Data.SqlClient.SqlCommand();
            ColumnMapCollection columns = MapRepository.Instance.GetColumns(typeof(Person));
            MappingHelper mappingHelper = new MappingHelper(command);

            Person person = new Person();
            person.ID = 1;
            person.Name = "Jordan";
            person.Age = 33;
            person.IsHappy = true;
            person.BirthDate = new DateTime(1977, 1, 22);

            mappingHelper.CreateParameters<Person>(person, columns, false, true);

            IQuery query = new SqlServerInsertQuery(columns, command.Parameters, "dbo", "People");

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.IsTrue(queryText.Contains("INSERT INTO [dbo].[People]"));
            Assert.IsFalse(queryText.Contains("[ID]"), "Should not contain [ID] column since it is marked as AutoIncrement");
            Assert.IsTrue(queryText.Contains("[Name]"), "Should contain the name column");
        }

        [TestMethod]
        public void SqlServerDeleteQuery_ShouldGenQuery()
        {
            // Arrange
            var command = new System.Data.SqlClient.SqlCommand();
            ColumnMapCollection columns = MapRepository.Instance.GetColumns(typeof(Person));
            MappingHelper mappingHelper = new MappingHelper(command);

            Person person = new Person();
            person.ID = 1;
            person.Name = "Jordan";
            person.Age = 33;
            person.IsHappy = true;
            person.BirthDate = new DateTime(1977, 1, 22);

            mappingHelper.CreateParameters<Person>(person, columns, false, true);

            IQuery query = new SqlServerDeleteQuery(columns, command.Parameters, "dbo", "People");

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.IsTrue(queryText.Contains("DELETE FROM [dbo].[People]"));
            Assert.IsTrue(queryText.Contains("WHERE [ID]="), "Should contain [ID] column since it is marked as AutoIncrement");
        }

        [TestMethod]
        public void SqlServerSelectQuery_ShouldGenQuery()
        {
            // Arrange
            var command = new System.Data.SqlClient.SqlCommand();
            ColumnMapCollection columns = MapRepository.Instance.GetColumns(typeof(Person));
            MappingHelper mappingHelper = new MappingHelper(command);

            Person person = new Person();
            person.ID = 1;
            person.Name = "Jordan";
            person.Age = 33;
            person.IsHappy = true;
            person.BirthDate = new DateTime(1977, 1, 22);

            var where = new WhereCondition<Person>(command, p => p.Name == "John", p => p.Age > 15);
            IQuery query = new SqlServerSelectQuery(columns, command.Parameters, "dbo", "People", where.ToString());

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["Name"].Value, "John");
            Assert.IsTrue(queryText.Contains("WHERE ([Name] = @Name AND [Age] > @Age)"));
        }

    }
}
