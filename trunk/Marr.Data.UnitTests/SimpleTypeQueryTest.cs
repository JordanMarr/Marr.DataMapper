using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.TestHelper;

namespace Marr.Data.Tests
{
    [TestClass]
    public class SimpleTypeQueryTest : TestBase
    {
        [TestInitialize]
        public void Init()
        {
            InitMappings();
        }

        [TestMethod]
        public void WhenQueryingManyStrings_ShouldReturnListOfStrings()
        {
            StubResultSet rs = new StubResultSet("Name");
            rs.AddRow("Person1");
            rs.AddRow("Person2");
            rs.AddRow("Person3");
            var db = CreateDB_ForQuery(rs);

            List<string> names = db.Query<string>("SELECT [Name] FROM Person");

            Assert.AreEqual("Person1", names[0]);
            Assert.AreEqual("Person2", names[1]);
            Assert.AreEqual("Person3", names[2]);
        }

        [TestMethod]
        public void WhenQueryingManyIntegers_ShouldReturnListOfIntegers()
        {
            StubResultSet rs = new StubResultSet("ID");
            rs.AddRow(1);
            rs.AddRow(2);
            rs.AddRow(3);
            var db = CreateDB_ForQuery(rs);

            List<int> ids = db.Query<int>("SELECT [ID] FROM Person");

            Assert.AreEqual(1, ids[0]);
            Assert.AreEqual(2, ids[1]);
            Assert.AreEqual(3, ids[2]);
        }

        [TestMethod]
        public void WhenQueryingOneString_ShouldReturnAString()
        {
            StubResultSet rs = new StubResultSet("Name");
            rs.AddRow("Person1");
            var db = CreateDB_ForQuery(rs);

            string name = db.Query<string>("SELECT [Name] FROM Person").FirstOrDefault();

            Assert.AreEqual("Person1", name);
        }

        [TestMethod]
        public void WhenFindingOneString_ShouldReturnAString()
        {
            StubResultSet rs = new StubResultSet("Name");
            rs.AddRow("Person1");
            var db = CreateDB_ForQuery(rs);

            string name = db.Find<string>("SELECT [Name] FROM Person");

            Assert.AreEqual("Person1", name);
        }
    }
}
