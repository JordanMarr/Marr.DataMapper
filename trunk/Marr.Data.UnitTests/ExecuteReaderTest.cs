using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.UnitTests;
using Marr.Data.Mapping;
using Marr.Data.UnitTests.Entities;
using Marr.Data.TestHelper;
using System.Data.Common;

namespace Marr.Data.Tests
{
    [TestClass]
    public class ExecuteReaderTest : TestBase
    {
        [TestInitialize]
        public void Init()
        {
            InitMappings();
        }
        
        [TestMethod]
        public void ShouldLoadEntityWithLambdaSyntax()
        {
            StubResultSet rs = new StubResultSet("ID", "Name", "Age");
            rs.AddRow(2, "Person2", 35);
            var db = CreateDB_ForQuery(rs);

            Person p = db.ExecuteReader("SELECT PersonName FROM tbl WHERE ID=2",
                r => new Person { ID = r.GetValue<int>("ID"), Name = r.GetValue<string>("Name"), Age = r.GetValue<int>("Age") }
                ).FirstOrDefault();

            Assert.AreEqual("Person2", p.Name);
        }

        [TestMethod]
        public void ShouldLoadEntityWithDelegateSyntax()
        {
            StubResultSet rs = new StubResultSet("ID", "Name", "Age");
            rs.AddRow(2, "Person2", 35);
            var db = CreateDB_ForQuery(rs);

            Person p = db.ExecuteReader("SELECT PersonName FROM tbl WHERE ID=2", LoadPerson).FirstOrDefault();

            Assert.AreEqual("Person2", p.Name);
        }

        [TestMethod]
        public void ShouldLoadEntityListWithDelegateSyntax()
        {
            StubResultSet rs = new StubResultSet("ID", "Name", "Age");
            rs.AddRow(1, "Person1", 31);
            rs.AddRow(2, "Person2", 32);
            rs.AddRow(3, "Person3", 33);
            var db = CreateDB_ForQuery(rs);

            List<Person> people = db.ExecuteReader("SELECT PersonName FROM tbl WHERE ID=2", LoadPerson).ToList();

            Assert.AreEqual(3, people.Count);
            Assert.AreEqual(1, people[0].ID);
            Assert.AreEqual(2, people[1].ID);
            Assert.AreEqual(3, people[2].ID);
        }

        private Person LoadPerson(DbDataReader reader)
        {
            Person p = new Person();
            p.ID = reader.GetValue<int>("ID");
            p.Name = reader.GetValue<string>("Name");
            p.Age = reader.GetValue<int>("Age");
            return p;
        }

    }
}
