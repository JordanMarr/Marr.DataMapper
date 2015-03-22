using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.Mapping;

namespace Marr.Data.IntegrationTests.DB_SqlServerCe
{
    [TestClass]
    public class LazyLoadedIntegrationTest : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            FluentMappings mappings = new FluentMappings();

            mappings
                .Entity<Building>()
                    .Columns.AutoMapSimpleTypeProperties()
						.For(b => b.Name).SetPrimaryKey()
                    .Relationships.AutoMapICollectionOrComplexProperties()
                        .Ignore(b => b.Offices)
                        .Ignore(b => b.OfficesDynamic)
						.For("_offices")
							.LazyLoad((db, building) => db.Query<Office>().Where(o => o.BuildingName == building.Name))
						.For("_officesDynamic")
							.LazyLoad((db, building) => db.Query<Office>().Where(o => o.BuildingName == building.Name))
                .Entity<Office>()
                    .Columns.AutoMapSimpleTypeProperties()
						.For(o => o.BuildingName).SetPrimaryKey()
						.For(o => o.Number).SetPrimaryKey();
        }

        [TestMethod]
        public void TestLazyLoad_UsingGenericLazyLoadedBackingField()
        {
            var db = base.CreateSqlServerCeDB();
            db.SqlMode = SqlModes.Text;

            Building building = db.Query<Building>().Graph().Where(b => b.Name == "Building1").FirstOrDefault();

            int officeCount = building.Offices.Count;
            Assert.AreEqual(3, officeCount);

            building.Offices.Add(new Office { BuildingName = "Building1", Number = 1000 });

            officeCount = building.Offices.Count;
            Assert.AreEqual(4, building.Offices.Count);

            building.Offices.Add(new Office { BuildingName = "Building1", Number = 1001 });
            Assert.AreEqual(5, building.Offices.Count);
        }

        [TestMethod]
        public void TestLazyLoad_Combing_Graphed_With_LazyLoad()
        {
            var db = base.CreateSqlServerCeDB();
            db.SqlMode = SqlModes.Text;

            Building building = db.Query<Building>().Graph().Where(b => b.Name == "Building1").FirstOrDefault();

            int officeCount = building.Offices.Count;
            Assert.AreEqual(3, officeCount);

            building.Offices.Add(new Office { BuildingName = "Building1", Number = 1000 });

            officeCount = building.Offices.Count;
            Assert.AreEqual(4, building.Offices.Count);

            building.Offices.Add(new Office { BuildingName = "Building1", Number = 1001 });
            Assert.AreEqual(5, building.Offices.Count);
        }

        [TestMethod]
        public void TestLazyLoad_UsingDynamicBackingField()
        {
            var db = base.CreateSqlServerCeDB();
            db.SqlMode = SqlModes.Text;

            Building building = db.Query<Building>().Graph().Where(b => b.Name == "Building1").FirstOrDefault();

			int oc = building.Offices.Count;
			Assert.AreEqual(3, oc);

            int officeCount = building.OfficesDynamic.Count;
            Assert.AreEqual(3, officeCount);

            building.OfficesDynamic.Add(new Office { BuildingName = "Building1", Number = 1000 });

            officeCount = building.OfficesDynamic.Count;
            Assert.AreEqual(4, building.OfficesDynamic.Count);

            building.OfficesDynamic.Add(new Office { BuildingName = "Building1", Number = 1001 });
            Assert.AreEqual(5, building.OfficesDynamic.Count);
        }

        [TestMethod]
        public void TestLazyLoad_WithMultipleParents()
        {
            var db = base.CreateSqlServerCeDB();
            db.SqlMode = SqlModes.Text;

            var buildings = db.Query<Building>().Graph().OrderBy(b => b.Name).ToList();

            Assert.AreEqual("Building1", buildings[0].Name);
            Assert.AreEqual("Building2", buildings[1].Name);
            Assert.AreEqual("Building3", buildings[2].Name);

            // Check once to perform the lazy load
            Assert.AreEqual(3, buildings[0].Offices.Count);
            Assert.AreEqual(2, buildings[1].Offices.Count);
            Assert.AreEqual(1, buildings[2].Offices.Count);

            // Check a second time (should not lazy load)
            Assert.AreEqual(3, buildings[0].Offices.Count);
            Assert.AreEqual(2, buildings[1].Offices.Count);
            Assert.AreEqual(1, buildings[2].Offices.Count);
        }
    }


    #region - Lazy Load Test Entities -

    public class Building
    {
        private LazyLoaded<List<Office>> _offices;
        private dynamic _officesDynamic; // For use with .NET 4.0 to eliminate unnecessary dependeny on mapping library

        public string Name { get; set; }
        public List<Office> Offices
        {
            get { return _offices; }
            set { _offices = value; }
        }

        public List<Office> OfficesDynamic
        {
            get { return _officesDynamic; }
            set { _officesDynamic = value; }
        }
    }

    public class Office
    {
        public string BuildingName { get; set; }
        public int Number { get; set; }
    }

    #endregion
}
