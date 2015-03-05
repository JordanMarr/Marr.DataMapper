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
		}

		public class AnotherClass
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
						.For(d => d.ID).SetPrimaryKey();
		}

		[TestMethod]
		public void DupesWithDifferentIDs_NoProblemsExpected()
		{
			// Arrange
			var rs1 = new StubResultSet("ID", "Dupe1ID", "Dupe2ID");
            rs1.AddRow(1, 100, 101);

			var rs2 = new StubResultSet("ID", "Name");
			rs2.AddRow(100, "Dupe_100");

			var rs3 = new StubResultSet("ID", "Name");
			rs3.AddRow(101, "Dupe_101");

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
	}
}
