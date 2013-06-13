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
            IDataMapper db = MockRepository.GenerateMock<IDataMapper>();
            Office office1 = new Office { Number = 1 };
            Office office2 = new Office { Number = 2 };
            List<Office> offices = new List<Office> { office1, office2 };
            db.Expect(d => d.Query<Office>().ToList()).Return(offices);

            Building building = new Building();
            var lazyProxy = new LazyLoaded<Building, List<Office>>((d,b) => d.Query<Office>().ToList());
            lazyProxy.Prepare(() => db, building);
            building._offices = lazyProxy;

            // Act
            int count = building.Offices.Count;

            // Assert
            Assert.IsTrue(count == 2);

            // Act
            count = building.Offices.Count;

            // Assert
            db.AssertWasCalled(d => d.Query<Office>(), o => o.Repeat.Once()); // Should hit DB once
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
