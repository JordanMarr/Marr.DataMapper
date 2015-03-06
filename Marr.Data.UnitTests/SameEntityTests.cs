using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using Marr.Data.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Marr.Data.UnitTests
{
	[TestClass]
	public class SameEntityTests : TestBase
	{
		#region - Test Classes -

		public class RootClass
		{
			public int ID { get; set; }

			public int Dupe1ID { get; set; }
			public DupedClass Dupe1 { get; set; }

			public int Dupe2ID { get; set; }
			public DupedClass Dupe2 { get; set; }
		}

		public class DupedClass
		{
			public int ID { get; set; }
			public string Name { get; set; }

			public int ChildID { get; set; }
			public DupedClassChild Child { get; set; }
		}

		public class DupedClassChild
		{
			public int ID { get; set; }
			public string Name { get; set; }
		}

		#endregion

		[TestInitialize]
		public void Init()
		{
			FluentMappings mappings = new FluentMappings();
			mappings
				.Entity<RootClass>()
					.Columns.AutoMapSimpleTypeProperties()
						.For(r => r.ID).SetPrimaryKey()
					.Relationships.MapProperties()
						.For(r => r.Dupe1)
							.EagerLoad((db, r) => db.Query<DupedClass>().Where(d => d.ID == r.Dupe1ID).FirstOrDefault())
						.For(r => r.Dupe2)
							.EagerLoad((db, r) => db.Query<DupedClass>().Where(d => d.ID == r.Dupe2ID).FirstOrDefault())

				.Entity<DupedClass>()
					.Columns.AutoMapSimpleTypeProperties()
						.For(d => d.ID).SetPrimaryKey()
					.Relationships.MapProperties()
						.For(d => d.Child)
							.JoinOne(d => d.Child, (d, dcc) => d.ChildID == dcc.ID)

				.Entity<DupedClassChild>()
					.Columns.AutoMapSimpleTypeProperties()
						.PrefixAltNames("dcc")
						.For(d => d.ID).SetPrimaryKey();
		}

		[TestMethod]
		public void DupesAtSameLevel_WithDifferentIDs()
		{
			// Arrange
			var rs1 = new StubResultSet("ID", "Dupe1ID", "Dupe2ID");
            rs1.AddRow(1, 100, 101);

			var rs2 = new StubResultSet("ID", "Name", "ChildID");
			rs2.AddRow(100, "Dupe_100", 1000);

			var rs3 = new StubResultSet("ID", "Name", "ChildID");
			rs3.AddRow(101, "Dupe_101", 1001);

            var db = CreateDB_ForQuery(rs1, rs2, rs3);

			// Act
			var rootEnt = db.Query<RootClass>()
				.Graph(r => r.Dupe1, r => r.Dupe2)
				.FirstOrDefault();

			// Assert
			Assert.AreEqual(100, rootEnt.Dupe1ID);
			Assert.AreEqual(101, rootEnt.Dupe2ID);

			Assert.IsNotNull(rootEnt.Dupe1);
			Assert.IsNotNull(rootEnt.Dupe2);

			Assert.AreEqual(100, rootEnt.Dupe1.ID);
			Assert.AreEqual(101, rootEnt.Dupe2.ID);
		}

		[TestMethod]
		public void DupesAtSameLevel_WithSameIDs()
		{
			// Arrange
			var rs1 = new StubResultSet("ID", "Dupe1ID", "Dupe2ID");
			rs1.AddRow(1, 100, 100);

			var rs2 = new StubResultSet("ID", "Name", "ChildID");
			rs2.AddRow(100, "Dupe_100", 1000);

			var rs3 = new StubResultSet("ID", "Name", "ChildID");
			rs3.AddRow(100, "Dupe_100", 1000);

			var db = CreateDB_ForQuery(rs1, rs2, rs3);

			// Act
			var rootEnt = db.Query<RootClass>()
				.Graph(r => r.Dupe1, r => r.Dupe2)
				.FirstOrDefault();

			// Assert
			Assert.AreEqual(100, rootEnt.Dupe1ID);
			Assert.AreEqual(100, rootEnt.Dupe2ID);

			Assert.IsNotNull(rootEnt.Dupe1);
			Assert.IsNotNull(rootEnt.Dupe2);

			Assert.AreEqual(100, rootEnt.Dupe1.ID);
			Assert.AreEqual(100, rootEnt.Dupe2.ID);
		}

		[TestMethod]
		public void DupesAtSameLevel_WithSameIDs_OneWithJoin()
		{
			// Arrange
			var rsRoot = new StubResultSet("ID", "Dupe1ID", "Dupe2ID");
			rsRoot.AddRow(1, 100, 100);

			var rsDupe1 = new StubResultSet("ID", "Name", "ChildID", "dccID", "dccName");
			rsDupe1.AddRow(100, "Dupe_100", 1000, 1000, "Child_1000");

			var rsDupe2 = new StubResultSet("ID", "Name", "ChildID");
			rsDupe2.AddRow(100, "Dupe_100", 1000);

			var db = CreateDB_ForQuery(rsRoot, rsDupe1, rsDupe2);

			// Act
			var rootEnt = db.Query<RootClass>()
				.Graph(
					r => r.Dupe1,
					r => r.Dupe2,
					r => r.Dupe1.Child)
				.FirstOrDefault();

			// Assert
			Assert.AreEqual(100, rootEnt.Dupe1ID);
			Assert.AreEqual(100, rootEnt.Dupe2ID);

			Assert.IsNotNull(rootEnt.Dupe1);
			Assert.IsNotNull(rootEnt.Dupe2);

			Assert.AreEqual(100, rootEnt.Dupe1.ID);
			Assert.AreEqual(100, rootEnt.Dupe2.ID);

			Assert.IsNotNull(rootEnt.Dupe1.Child);
			Assert.IsNull(rootEnt.Dupe2.Child);

			Assert.AreEqual(1000, rootEnt.Dupe1.Child.ID);
			Assert.AreEqual("Child_1000", rootEnt.Dupe1.Child.Name);
		}
	}
}
