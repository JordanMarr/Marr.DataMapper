using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Marr.Data.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Marr.Data.UnitTests
{
	[TestClass]
	public class FluentMappingTests
	{
		[TestMethod]
		public void ForEachEntity_ShouldApplyMappingsToAllSubclassesInAssembly()
		{
			var fluentMappings = new FluentMappings();
			fluentMappings
				.ForEachEntity<IEntityBase>(entity => entity
					.Columns.AutoMapSimpleTypeProperties()
						.For(e => e.ID)
							.SetPrimaryKey()
							.SetAutoIncrement()
							.SetReturnValue()
					.Relationships.AutoMapICollectionOrComplexProperties()
					.Tables.AutoMapTable()
				);

			var repos = Marr.Data.MapRepository.Instance;
			var buildingColumns = repos.GetColumns(typeof(Building));
			var buildingRelationships = repos.GetRelationships(typeof(Building));
			var buildingTable = repos.GetTableName(typeof(Building));
			var roomColumns = repos.GetColumns(typeof(Room));
			var roomRelationships = repos.GetRelationships(typeof(Room));
			var roomTable = repos.GetTableName(typeof(Room));

			// Check columns
			Assert.IsNotNull(buildingColumns);
			Assert.IsNotNull(roomColumns);

			Assert.AreEqual(2, buildingColumns.Count);
			Assert.AreEqual(3, roomColumns.Count);

			// Check PKs
			Assert.IsTrue(buildingColumns.GetByColumnName("ID").ColumnInfo.IsPrimaryKey);
			Assert.IsTrue(roomColumns.GetByColumnName("ID").ColumnInfo.IsPrimaryKey);

			// Check relationships
			Assert.AreEqual(1, buildingRelationships.Count);
			Assert.IsTrue(buildingRelationships.First().RelationshipInfo.RelationType == RelationshipTypes.Many);
			Assert.AreEqual(1, roomRelationships.Count);
			Assert.IsTrue(roomRelationships.First().RelationshipInfo.RelationType == RelationshipTypes.One);

			// Check tables
			Assert.AreEqual("Building", buildingTable);
			Assert.AreEqual("Room", roomTable);
		}

		#region - Test Entity Classes -

		public interface IEntityBase
		{
			int ID { get; set; }
		}

		public class Building : IEntityBase
		{
			public int ID { get; set; }
			public string Name { get; set; }
			public List<Room> Rooms { get; set; }
		}

		public class Room : IEntityBase
		{
			public int ID { get; set; }
			public int BuildingID { get; set; }
			public string Name { get; set; }
			public Building Building { get; set; }
		}

		#endregion
	}


}
