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

        [TestMethod]
        public void WhenQuerying_CastingError_ShouldGiveCustomErrorMessage()
        {
            // Create a result set with a long value and then
            // purposefully cast it to a string to create a cast exception
            StubResultSet rs = new StubResultSet("LongValue");
            rs.AddRow(12345678L);
            var db = CreateDB_ForQuery(rs);

            bool exceptionWasThrown = false;

            try
            {
                string name = db.Query<string>("SELECT [LongValue] FROM Tbl").FirstOrDefault();
            }
            catch (Exception ex)
            {
                exceptionWasThrown = true;
                Assert.IsInstanceOfType(ex, typeof(DataMappingException));
            }

            Assert.IsTrue(exceptionWasThrown);
        }

        [TestMethod]
        public void WhenFinding_CastingError_ShouldGiveCustomErrorMessage()
        {
            // Create a result set with a long value and then
            // purposefully cast it to a string to create a cast exception
            StubResultSet rs = new StubResultSet("LongValue");
            rs.AddRow(12345678L);
            var db = CreateDB_ForQuery(rs);

            bool exceptionWasThrown = false;

            try
            {
                string name = db.Find<string>("SELECT [LongValue] FROM Tbl");
            }
            catch (Exception ex)
            {
                exceptionWasThrown = true;
                Assert.IsInstanceOfType(ex, typeof(DataMappingException));
            }

            Assert.IsTrue(exceptionWasThrown);
        }

        [TestMethod]
        public void IsSimpleTypeTest_TheFollowingTypesShouldBeSimpleTypes()
        {
            Type[] types = new[] { 
                typeof(int), typeof(short), typeof(long), typeof(bool), typeof(DateTime), typeof(string), 
                typeof(byte), typeof(decimal), typeof(double), typeof(Marr.Data.SqlModes)
            };

            foreach (Type type in types)
            {
                Assert.IsTrue(DataHelper.IsSimpleType(type), string.Format("{0} is not a simple type.", type.Name));
            }
        }

        [TestMethod]
        public void IsSimpleTypeTest_GenericsShouldBeAbleToBeSimpleTypes()
        {
            Type[] types = new[] { 
                typeof(int?), typeof(short?), typeof(long?), typeof(bool?), typeof(DateTime?), typeof(string), 
                typeof(byte?), typeof(decimal?), typeof(double?)
            };

            foreach (Type type in types)
            {
                Assert.IsTrue(DataHelper.IsSimpleType(type), string.Format("{0} is not a simple type.", type.Name));
            }
        }
    }
}
