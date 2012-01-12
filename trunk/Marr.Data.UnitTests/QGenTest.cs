using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.QGen;
using Marr.Data.UnitTests.Entities;
using Rhino.Mocks;
using Marr.Data.Mapping;
using Marr.Data;
using System.Data.Common;
using Marr.Data.QGen.Dialects;
using Marr.Data.Tests.Entities;
using System.Linq.Expressions;

namespace Marr.Data.UnitTests
{
    /// <summary>
    /// Summary description for QGenTest
    /// </summary>
    [TestClass]
    public class QGenTest : TestBase
    {
        [TestInitialize]
        public void Init()
        {
            InitMappings();
        }

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

            mappingHelper.CreateParameters<Person>(person, columns, true);

            int idValue = 7;
            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter =  p => p.ID == person.ID || p.ID == idValue || p.Name == person.Name && p.Name == "Bob";
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);

            IQuery query = new UpdateQuery(new SqlServerDialect(), columns, command, "dbo.People", where.ToString());

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.IsTrue(queryText.Contains("UPDATE [dbo].[People]"));
            Assert.IsTrue(queryText.Contains("[Name]"));
            Assert.IsTrue(queryText.Contains("[Age]"));
            Assert.IsTrue(queryText.Contains("[IsHappy]"));
            Assert.IsTrue(queryText.Contains("[BirthDate]"));
            Assert.IsTrue(queryText.Contains("[ID] = @P4"));
            Assert.IsTrue(queryText.Contains("[ID] = @P5"));
            Assert.IsTrue(queryText.Contains("[Name] = @P6"));
            Assert.IsTrue(queryText.Contains("[Name] = @P7"));
            Assert.AreEqual(command.Parameters["@P4"].Value, 1);
            Assert.AreEqual(command.Parameters["@P5"].Value, 7);
            Assert.AreEqual(command.Parameters["@P6"].Value, "Jordan");
            Assert.AreEqual(command.Parameters["@P7"].Value, "Bob");
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

            mappingHelper.CreateParameters<Person>(person, columns, true);

            IQuery query = new InsertQuery(new SqlServerDialect(), columns, command, "dbo.People");

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.IsTrue(queryText.Contains("INSERT INTO [dbo].[People]"));
            Assert.IsFalse(queryText.Contains("@ID"), "Should not contain ID column since it is marked as AutoIncrement");
            Assert.IsTrue(queryText.Contains("[Name]"), "Should contain the name column");
        }

        [TestMethod]
        public void SqlServerDeleteQuery_ShouldGenQuery()
        {
            // Arrange
            var command = new System.Data.SqlClient.SqlCommand();
            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.ID == 5;
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, false);
            IQuery query = new DeleteQuery(new Dialect(), tables[0], where.ToString());

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.IsTrue(queryText.Contains("DELETE FROM [PersonTable]"));
            Assert.IsTrue(queryText.Contains("WHERE ([ID] = @P0)"));
            Assert.AreEqual(command.Parameters["@P0"].Value, 5);
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

            List<Person> list = new List<Person>();

            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.Name == "John" && p.Age > 15 || p.Age < 5 && p.Age > 1;
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);
            IQuery query = new SelectQuery(new SqlServerDialect(), tables, where.ToString(), "", false);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["@P0"].Value, "John");
            Assert.AreEqual(command.Parameters["@P1"].Value, 15);
            Assert.AreEqual(command.Parameters["@P2"].Value, 5);
            Assert.AreEqual(command.Parameters["@P3"].Value, 1);
            Assert.IsTrue(queryText.Contains("([t0].[Name] = @P0) AND ([t0].[Age] > @P1))"));
            Assert.IsTrue(queryText.Contains("([t0].[Age] < @P2) AND ([t0].[Age] > @P3))"));
        }

        [TestMethod]
        public void SqlServerSelectQuery_Contains_Constant()
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

            List<Person> list = new List<Person>();

            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.Name.Contains("John");
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);
            IQuery query = new SelectQuery(new SqlServerDialect(), tables, where.ToString(), "", false);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["@P0"].Value, "John");
            Assert.IsTrue(queryText.Contains("[Name] LIKE '%' + @P0 + '%'"));
        }

        [TestMethod]
        public void SqlServerSelectQuery_Contains_Variable()
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

            List<Person> list = new List<Person>();

            string john = "John";

            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.Name.Contains(john);
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);
            IQuery query = new SelectQuery(new SqlServerDialect(), tables, where.ToString(), "", false);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["@P0"].Value, "John");
            Assert.IsTrue(queryText.Contains("[Name] LIKE '%' + @P0 + '%'"));
        }


        [TestMethod]
        public void SqlServerSelectQuery_StartsWith_Constant()
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

            List<Person> list = new List<Person>();

            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.Name.StartsWith("John");
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);
            IQuery query = new SelectQuery(new SqlServerDialect(), tables, where.ToString(), "", false);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["@P0"].Value, "John");
            Assert.IsTrue(queryText.Contains("[Name] LIKE @P0 + '%'"));
        }

        [TestMethod]
        public void SqlServerSelectQuery_StartsWith_Variable()
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

            List<Person> list = new List<Person>();

            string john = "John";

            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.Name.StartsWith(john);
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);
            IQuery query = new SelectQuery(new SqlServerDialect(), tables, where.ToString(), "", false);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["@P0"].Value, "John");
            Assert.IsTrue(queryText.Contains("[Name] LIKE @P0 + '%'"));
        }

        [TestMethod]
        public void SqlServerSelectQuery_EndsWith_Constant()
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

            List<Person> list = new List<Person>();

            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.Name.EndsWith("John");
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);
            IQuery query = new SelectQuery(new SqlServerDialect(), tables, where.ToString(), "", false);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["@P0"].Value, "John");
            Assert.IsTrue(queryText.Contains("[Name] LIKE '%' + @P0"));
        }

        [TestMethod]
        public void SqlServerSelectQuery_EndsWith_Variable()
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

            List<Person> list = new List<Person>();

            string john = "John";

            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.Name.EndsWith(john);
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);
            IQuery query = new SelectQuery(new SqlServerDialect(), tables, where.ToString(), "", false);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["@P0"].Value, "John");
            Assert.IsTrue(queryText.Contains("[Name] LIKE '%' + @P0"));
        }

        [TestMethod]
        public void SqlServerSelectQuery_BinaryExpression_MethodExpression()
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

            List<Person> list = new List<Person>();

            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.Age > 5 && p.Name.Contains("John");
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);
            IQuery query = new SelectQuery(new SqlServerDialect(), tables, where.ToString(), "", false);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["@P0"].Value, 5);
            Assert.AreEqual(command.Parameters["@P1"].Value, "John");
            Assert.IsTrue(queryText.Contains("[Age] > @P0"));
            Assert.IsTrue(queryText.Contains("[Name] LIKE '%' + @P1 + '%'"));
        }

        [TestMethod]
        public void SqlServerSelectQuery_MethodExpression_BinaryExpression()
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

            List<Person> list = new List<Person>();

            TableCollection tables = new TableCollection { new Table(typeof(Person)) };
            Expression<Func<Person, bool>> filter = p => p.Name.Contains("John") && p.Age > 5;
            var where = new WhereBuilder<Person>(command, new SqlServerDialect(), filter, tables, false, true);
            IQuery query = new SelectQuery(new SqlServerDialect(), tables, where.ToString(), "", false);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsNotNull(queryText);
            Assert.AreEqual(command.Parameters["@P0"].Value, "John");
            Assert.AreEqual(command.Parameters["@P1"].Value, 5);
            Assert.IsTrue(queryText.Contains("[Name] LIKE '%' + @P0 + '%'"));
            Assert.IsTrue(queryText.Contains("[Age] > @P1"));
        }

        /// <summary>
        /// Ref: bug fix for work item #34 submitted by vitidev
        /// </summary>
        [TestMethod]
        public void WhenColumnNameDiffersFromProperty_InsertQueryShouldUseColumnName()
        {
            // Arrange
            Person2 person = new Person2 { Name = "Bob", Age = 40, BirthDate = DateTime.Now };
            Dialect dialect = new SqlServerDialect();
            ColumnMapCollection mappings = MapRepository.Instance.GetColumns(typeof(Person2));
            DbCommand command = new System.Data.SqlClient.SqlCommand();
            var mappingHelper = new MappingHelper(command);
            mappingHelper.CreateParameters<Person2>(person, mappings, true);
            string targetTable = "PersonTable";
            InsertQuery query = new InsertQuery(dialect, mappings, command, targetTable);

            // Act
            string queryText = query.Generate();

            // Assert
            Assert.IsTrue(queryText.Contains("[PersonName]"), "Query should contain column name");
            Assert.IsTrue(queryText.Contains("[PersonAge]"), "Query should contain column name");
            Assert.IsTrue(queryText.Contains("[BirthDate]"), "Query should contain property name");
            Assert.IsTrue(queryText.Contains("[IsHappy]"), "Query should contain property name");
        }

        [TestMethod]
        public void WhenPageIsCalledBeforeOrderBy_ShouldTranslateToQuery()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlPage1 = db.Query<Person>()
                .Page(1, 20)
                .OrderBy(p => p.Name)
                .BuildQuery();

            Assert.IsTrue(sqlPage1.Contains("WHERE RowNumber BETWEEN 1 AND 20"));

            string sqlPage2 = db.Query<Person>()
                .Page(2, 20)
                .OrderBy(p => p.Name)
                .BuildQuery();

            Assert.IsTrue(sqlPage2.Contains("WHERE RowNumber BETWEEN 21 AND 40"));
        }

        [TestMethod]
        public void WhenPageIsCalledAfterOrderBy_ShouldTranslateToQuery()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlPage1 = db.Query<Person>()
                .OrderBy(p => p.Name)
                .Page(1, 20)
                .BuildQuery();

            Assert.IsTrue(sqlPage1.Contains("WHERE RowNumber BETWEEN 1 AND 20"));

            string sqlPage2 = db.Query<Person>()
                .OrderBy(p => p.Name)
                .Page(2, 20)
                .BuildQuery();

            Assert.IsTrue(sqlPage2.Contains("WHERE RowNumber BETWEEN 21 AND 40"));
        }


        [TestMethod]
        public void WhenSkipTakeIsCalledBeforeOrderBy_ShouldTranslateToQuery()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlPage1 = db.Query<Person>()
                .Skip(0)
                .Take(20)
                .OrderBy(p => p.Name)
                .BuildQuery();

            Assert.IsTrue(sqlPage1.Contains("WHERE RowNumber BETWEEN 1 AND 20"));

            string sqlPage2 = db.Query<Person>()
                .Skip(20)
                .Take(20)
                .OrderBy(p => p.Name)
                .BuildQuery();

            Assert.IsTrue(sqlPage2.Contains("WHERE RowNumber BETWEEN 21 AND 40"));
        }

        [TestMethod]
        public void WhenSkipTakeIsCalledAfterOrderBy_ShouldTranslateToQuery()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, @"Data Source=jordan-pc\sqlexpress;Initial Catalog=MyBigFishBowl;Persist Security Info=True;User ID=jmarr;Password=password");
            string sqlPage1 = db.Query<Person>()
                .OrderBy(p => p.Name)
                .Skip(0)
                .Take(20).BuildQuery();

            Assert.IsTrue(sqlPage1.Contains("WHERE RowNumber BETWEEN 1 AND 20"));

            string sqlPage2 = db.Query<Person>()
                .OrderBy(p => p.Name)
                .Skip(20)
                .Take(20)
                .BuildQuery();

            Assert.IsTrue(sqlPage2.Contains("WHERE RowNumber BETWEEN 21 AND 40"));
        }

    }
}
