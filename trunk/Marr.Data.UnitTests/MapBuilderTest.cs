using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.UnitTests.Entities;
using Marr.Data.Mapping;
using System.Reflection;

namespace Marr.Data.UnitTests
{
    /// <summary>
    /// Summary description for MapBuilderTest
    /// </summary>
    [TestClass]
    public class MapBuilderTest
    {
        private Type _personType;
        private MapRepository _mapRepository;

        [TestInitialize]
        public void Init()
        {
            _mapRepository = MapRepository.Instance;
            _personType = typeof(UnmappedPerson);

            InitMappings();
        }

        public void InitMappings()
        {
            MapBuilder builder = new MapBuilder();

            builder.SetTableName<Person>("PersonTable");

            builder.BuildColumns<Person>()
                .SetReturnValue("ID")
                .SetPrimaryKey("ID")
                .SetAutoIncrement("ID");

            builder.BuildRelationships<Person>();

            builder.BuildColumns<Pet>()
                .SetPrimaryKey("ID")
                .SetAltName("ID", "Pet_ID")
                .SetAltName("Name", "Pet_Name");
        }

        #region - Columns -

        [TestMethod]
        public void MapBuilder_ShouldMapPublicProperties()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildColumns<UnmappedPerson>();
            Assert.IsTrue(maps.Count == 5);
            Assert.IsTrue(_mapRepository.Columns[_personType].Count == 5);
        }

        [TestMethod]
        public void MapBuilder_ShouldMapPublicAndPrivateProperties()
        {
            var mapBuilder = new MapBuilder(false);
            var maps = mapBuilder.BuildColumns<UnmappedPerson>();
            Assert.IsTrue(maps.Count == 6);
            Assert.IsTrue(_mapRepository.Columns[_personType].Count == 6);
        }

        [TestMethod]
        public void MapBuilder_ShouldMapPublicProperties_MinusExclusions()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildColumnsExcept<UnmappedPerson>("ID", "Name");
            Assert.IsTrue(maps.Count == 4);
            Assert.IsNotNull(maps["Age"]);
            Assert.IsNotNull(maps["BirthDate"]);
            Assert.IsNotNull(maps["IsHappy"]);
            Assert.IsNotNull(maps["Pets"]);
            Assert.IsNotNull(_mapRepository.Columns[_personType]["Age"]);
            Assert.IsNotNull(_mapRepository.Columns[_personType]["BirthDate"]);
            Assert.IsNotNull(_mapRepository.Columns[_personType]["IsHappy"]);
            Assert.IsNotNull(_mapRepository.Columns[_personType]["Pets"]);
        }

        [TestMethod]
        public void MapBuilder_ShouldMapPublicProperties_ThatAreSpecified()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildColumns<UnmappedPerson>("Name");
            Assert.IsTrue(maps.Count == 1);
            Assert.IsNotNull(maps["Name"]);
        }

        [TestMethod]
        public void MapBuilder_ShouldMapPublicProperties_ThatAreOfTypeDateTime()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildColumns<UnmappedPerson>(m => 
                m.MemberType == MemberTypes.Property && (m as PropertyInfo).PropertyType == typeof(DateTime));
            
            Assert.IsTrue(maps.Count == 1);
            Assert.IsTrue(maps[0].FieldType == typeof(DateTime));
        }

        #endregion

        #region - Relationships -

        [TestMethod]
        public void MapBuilder_Relationships_ShouldMapPublicProperties_ThatAreICollection()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildRelationships<UnmappedPerson>();
            Assert.IsTrue(maps.Count == 1);
            Assert.IsNotNull(maps["Pets"]);
            Assert.IsTrue(_mapRepository.Relationships[_personType].Count == 1);
            Assert.IsNotNull(_mapRepository.Relationships[_personType]["Pets"]);
        }

        [TestMethod]
        public void MapBuilder_Relationships_ShouldMapPublicProperties_ThatAreSpecified()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildRelationships<UnmappedPerson>("Pets");
            Assert.IsTrue(maps.Count == 1);
            Assert.IsNotNull(maps["Pets"]);
        }

        #endregion

    }
}
