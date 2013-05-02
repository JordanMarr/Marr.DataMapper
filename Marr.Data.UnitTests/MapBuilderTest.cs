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

            builder.BuildTable<Person>("PersonTable");

            builder.BuildColumnsFromSimpleTypes<Person>()
                .For(p => p.ID)
                    .SetPrimaryKey()
                    .SetReturnValue()
                    .SetAutoIncrement();

            builder.BuildRelationships<Person>();

            builder.BuildColumns<Pet>()
                .For(p => p.ID)
                    .SetPrimaryKey()
                    .SetAltName("Pet_ID")
                .For(p => p.Name)
                    .SetAltName("Pet_Name");
        }

        #region - Columns -

        [TestMethod]
        public void MapBuilder_ShouldMapPublicProperties()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildColumns<UnmappedPerson>();
            Assert.IsTrue(maps.MappedColumns.Count == 5);
            Assert.IsTrue(_mapRepository.Columns[_personType].Count == 5);
        }

        [TestMethod]
        public void MapBuilder_ShouldMapPublicAndPrivateProperties()
        {
            var mapBuilder = new MapBuilder(false);
            var maps = mapBuilder.BuildColumns<UnmappedPerson>();
            Assert.IsTrue(maps.MappedColumns.Count == 6);
            Assert.IsTrue(_mapRepository.Columns[_personType].Count == 6);
        }

        [TestMethod]
        public void MapBuilder_ShouldMapPublicProperties_MinusExclusions()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildColumnsExcept<UnmappedPerson>("ID", "Name");
            Assert.IsTrue(maps.MappedColumns.Count == 4);
            Assert.IsNotNull(maps.MappedColumns.GetByColumnName("Age"));
            Assert.IsNotNull(maps.MappedColumns.GetByColumnName("BirthDate"));
            Assert.IsNotNull(maps.MappedColumns.GetByColumnName("IsHappy"));
            Assert.IsNotNull(maps.MappedColumns.GetByColumnName("Pets"));
            Assert.IsNotNull(_mapRepository.Columns[_personType].GetByColumnName("Age"));
            Assert.IsNotNull(_mapRepository.Columns[_personType].GetByColumnName("BirthDate"));
            Assert.IsNotNull(_mapRepository.Columns[_personType].GetByColumnName("IsHappy"));
            Assert.IsNotNull(_mapRepository.Columns[_personType].GetByColumnName("Pets"));
        }

        [TestMethod]
        public void MapBuilder_ShouldMapPublicProperties_ThatAreSpecified()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildColumns<UnmappedPerson>("Name");
            Assert.IsTrue(maps.MappedColumns.Count == 1);
            Assert.IsNotNull(maps.MappedColumns.GetByColumnName("Name"));
        }

        [TestMethod]
        public void MapBuilder_ShouldMapPublicProperties_ThatAreOfTypeDateTime()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildColumns<UnmappedPerson>(m => 
                m.MemberType == MemberTypes.Property && (m as PropertyInfo).PropertyType == typeof(DateTime));

            Assert.IsTrue(maps.MappedColumns.Count == 1);
            Assert.IsTrue(maps.MappedColumns[0].FieldType == typeof(DateTime));
        }

        [TestMethod]
        public void MapBuilder_ShouldPrefixAltNames_But_ShouldNotPrefixNames()
        {
            var mapBuilder = new MapBuilder();
            var columns = mapBuilder.BuildColumns<Person>()
                .MappedColumns.PrefixAltNames("p_");

            Assert.IsTrue(columns.All(c => c.ColumnInfo.AltName.StartsWith("p_")));
            Assert.IsFalse(columns.All(c => c.ColumnInfo.Name.StartsWith("p_")));
        }

        [TestMethod]
        public void MapBuilder_ShouldSuffixAltNames_But_ShouldNotSuffixNames()
        {
            var mapBuilder = new MapBuilder();
            var columns = mapBuilder.BuildColumns<Person>()
                .MappedColumns.SuffixAltNames("_p");

            Assert.IsTrue(columns.All(c => c.ColumnInfo.AltName.EndsWith("_p")));
            Assert.IsFalse(columns.All(c => c.ColumnInfo.Name.EndsWith("_p")));
        }

        [TestMethod]
        public void MapBuilder_ShouldOnlyMapSimpleTypes()
        {
            var mapBuilder = new MapBuilder();
            var columns = mapBuilder.BuildColumnsFromSimpleTypes<EntityWithSimpleAndComplexProperties>();

            Assert.AreEqual(2, columns.MappedColumns.Count);
        }

        [TestMethod]
        public void MapBuilder_IgnoringAnAlreadyExcludedColumn_ShouldNotThrowAnException()
        {
            var mapBuilder = new MapBuilder();
            var columns = mapBuilder.BuildColumnsFromSimpleTypes<EntityWithSimpleAndComplexProperties>()
                .Ignore(e => e.OneToOneChild) // Should already be ignored because it is not a simple type
                .Ignore(e => e.Collection); // Should already be ignored because it is an ICollection

            Assert.AreEqual(2, columns.MappedColumns.Count);
        }

        #endregion

        #region - Relationships -

        [TestMethod]
        public void MapBuilder_Relationships_ShouldMapPublicProperties_ThatAreICollection()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildRelationships<UnmappedPerson>();
            Assert.IsTrue(maps.Relationships.Count == 1);
            Assert.IsNotNull(maps.Relationships["Pets"]);
            Assert.IsTrue(_mapRepository.Relationships[_personType].Count == 1);
            Assert.IsNotNull(_mapRepository.Relationships[_personType]["Pets"]);
        }

        [TestMethod]
        public void MapBuilder_Relationships_ShouldMapPublicProperties_ThatAreSpecified()
        {
            var mapBuilder = new MapBuilder();
            var maps = mapBuilder.BuildRelationships<UnmappedPerson>("Pets");
            Assert.IsTrue(maps.Relationships.Count == 1);
            Assert.IsNotNull(maps.Relationships["Pets"]);
        }

        #endregion

    }

    #region - MapBuilder Test Entity -

    public class EntityWithSimpleAndComplexProperties
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public EntityWithSimpleAndComplexPropertiesChild OneToOneChild { get; set; }
        public List<string> Collection { get; set; }
        
    }

    public class EntityWithSimpleAndComplexPropertiesChild
    {
        public int ID { get; set; }
    }

    #endregion
}
