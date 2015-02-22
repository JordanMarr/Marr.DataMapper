using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.UnitTests.Entities;
using Marr.Data.UnitTests;
using Marr.Data.TestHelper;
using Rhino.Mocks;
using Marr.Data.Mapping;
using System.Reflection;

namespace Marr.Data.UnitTests
{
    [TestClass]
    public class LazyLoadedTest : TestBase
    {
        [TestMethod]
        public void LazyLoaded_Implicit_Conversion_When_IsLoad_Should_Return_Value()
        {
            Building building = new Building();
            Office office1 = new Office { Number = 1 };
            Office office2 = new Office { Number = 2 };
            building._offices = new LazyLoaded<Building, List<Office>>(new List<Office> { office1, office2 });
            int count = building.Offices.Count;
            Assert.IsTrue(count == 2);
        }

        [TestMethod]
        public void LazyLoaded_Implicit_Conversion_When_Not_IsLoad_Should_Call_DB_Once()
        {
			// Arrange
			StubResultSet rsOffices = new StubResultSet("Number");
			rsOffices.AddRow(1);
			rsOffices.AddRow(2);
			
			var db = CreateDB_ForQuery(rsOffices);

			Building building = new Building();
			int calls = 0;
			var lazyProxy = new LazyLoaded<Building, List<Office>>((d, b) => {
				calls++;
				return d.Query<Office>().ToList();
			});
			lazyProxy.Prepare(() => db, building, "Offices");
			building._offices = lazyProxy;

			// Act
			int count = building.Offices.Count;

			// Assert
			Assert.AreEqual(2, count);
			Assert.AreEqual(1, calls);

			// Act again (should not hit db)
			count = building.Offices.Count;

			// Assert
			Assert.AreEqual(2, count);
			Assert.AreEqual(1, calls);
        }

		[TestMethod]
		[ExpectedException(typeof(RelationshipLoadException))]
		public void LazyLoadedException_ShouldThrowDataMappingException()
		{
			// Arrange
			StubResultSet rsOffices = new StubResultSet("Number");
			rsOffices.AddRow(1);
			rsOffices.AddRow(2);

			var db = CreateDB_ForQuery(rsOffices);

			Building building = new Building();
			var lazyProxy = new LazyLoaded<Building, List<Office>>((d, b) =>
			{
				throw new Exception("Oops!");
				//return d.Query<Office>().ToList();
			});
			lazyProxy.Prepare(() => db, building, "Offices");
			building._offices = lazyProxy;

			// Act
			int count = building.Offices.Count;
		}
    }

    #region - Lazy Load Test Entities -

    public class Building
    {
        public dynamic _offices;
        
        public string Name { get; set; }
        public List<Office> Offices
        {
            get
            {
                return _offices;
            }
            set
            {
                _offices = value;
            }
        }
    }

    public class Office
    {
        public int Number { get; set; }
    }

    #endregion
}
