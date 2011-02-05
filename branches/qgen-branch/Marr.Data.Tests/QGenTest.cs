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

            SqlServerUpdateQuery query = new SqlServerUpdateQuery(columns, command.Parameters);

            // Act
            string queryText = query.Generate("dbo", "People");

            // Assert
            Assert.IsNotNull(queryText);
            Assert.IsTrue(queryText.Contains("UPDATE [dbo].[People]"));
            Assert.IsTrue(queryText.Contains("[Name]"));
            Assert.IsTrue(queryText.Contains("[Age]"));
            Assert.IsTrue(queryText.Contains("[IsHappy]"));
            Assert.IsTrue(queryText.Contains("[BirthDate]"));
            Assert.IsTrue(queryText.Contains("WHERE [ID]=@ID"));
        }
    }
}
