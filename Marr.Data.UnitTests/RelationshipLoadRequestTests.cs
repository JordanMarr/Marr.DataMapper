using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Marr.Data.QGen;
using Marr.Data.TestHelper;
using Marr.Data.UnitTests.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Marr.Data.UnitTests
{
	[TestClass]
	public class RelationshipLoadRequestTests : TestBase
	{
		[TestMethod]
		public void RelationshipToLoad_OrderItems()
		{
			Expression<Func<Order, object>> loadExp =
				o => o.OrderItems;

			var rtl = new RelationshipLoadRequest(loadExp);

			Assert.AreEqual(1, rtl.MemberPath.Count);
			Assert.AreEqual("OrderItems", rtl.BuildMemberPath());
			Assert.AreEqual("OrderItem", rtl.BuildEntityTypePath());
		}

		[TestMethod]
		public void EntGraph_OrderItems()
		{
			var entGraph = new EntityGraph(typeof(Order), new List<Order>());
			var orderItemsNode = entGraph.ElementAt(1);

			string path = orderItemsNode.BuildEntityTypePath();

			Assert.AreEqual("OrderItem", path);
		}

		[TestMethod]
		public void RelationshipToLoad_ShouldParseNestedSelectExpressions()
		{
			Expression<Func<Order, object>> loadExp = 
				o => o.OrderItems.Select(oi => oi.ItemReceipt);

			var rtl = new RelationshipLoadRequest(loadExp);

			Assert.AreEqual(2, rtl.MemberPath.Count);
			Assert.AreEqual("OrderItems-ItemReceipt", rtl.BuildMemberPath());
			Assert.AreEqual("OrderItem-Receipt", rtl.BuildEntityTypePath());
		}

		[TestMethod]
		public void RelationshipToLoad_ShouldParseNestedFirstExpressions()
		{
			Expression<Func<Order, object>> loadExp = 
				o => o.OrderItems.First().ItemReceipt;

			var rtl = new RelationshipLoadRequest(loadExp);

			Assert.AreEqual(2, rtl.MemberPath.Count);
			Assert.AreEqual("OrderItems-ItemReceipt", rtl.BuildMemberPath());
			Assert.AreEqual("OrderItem-Receipt", rtl.BuildEntityTypePath());
		}

		[TestMethod]
		public void EntGraph_BuildEntityTypePath_All()
		{
			var entGraph = new EntityGraph(typeof(Order), new List<Order>());

			string[] paths = entGraph.Select(g => g.BuildEntityTypePath()).ToArray();

			Assert.AreEqual(3, paths.Length);
			Assert.AreEqual("", paths[0]);
			Assert.AreEqual("OrderItem", paths[1]);
			Assert.AreEqual("OrderItem-Receipt", paths[2]);
		}
		
		[TestMethod]
		public void RelationshipToLoad_ParseEntireEntGraph_BuildEntityTypePaths()
		{
			var entGraph = new EntityGraph(typeof(Order), new List<Order>());

			var relationshipsToLoad = entGraph
				.Where(g => g.Member != null)				
				.Select(g => new RelationshipLoadRequest(g)).ToArray();
			string[] paths = relationshipsToLoad.Select(rtl => rtl.BuildEntityTypePath()).ToArray();

			Assert.AreEqual(2, paths.Length);
			Assert.AreEqual("OrderItem", paths[0]);
			Assert.AreEqual("OrderItem-Receipt", paths[1]);
		}

		[TestMethod]
		public void RelationshipToLoad_ParseEntireEntGraph_BuildMemberPaths()
		{
			var entGraph = new EntityGraph(typeof(Order), new List<Order>());

			var relationshipsToLoad = entGraph
				.Where(g => g.Member != null)
				.Select(g => new RelationshipLoadRequest(g)).ToArray();
			string[] paths = relationshipsToLoad.Select(rtl => rtl.BuildMemberPath()).ToArray();

			Assert.AreEqual(2, paths.Length);
			Assert.AreEqual("OrderItems", paths[0]);
			Assert.AreEqual("OrderItems-ItemReceipt", paths[1]);
		}

		//[TestMethod]
		//public void QueryBuilder_Graph()
		//{
		//	StubResultSet rs = new StubResultSet("ID", "OrderName", "OrderItemID", "OrderID", "ItemDescription", "Price", "AmountPaid");
		//	rs.AddRow(1, "Order1", 50, 1, "Red car", 100.35m, DBNull.Value);
		//	rs.AddRow(1, "Order1", 51, 1, "Blue wagon", 44.87m, DBNull.Value);
		//	rs.AddRow(2, "Order2", 60, 2, "Guitar", 1500.50m, 1500.50m);
		//	rs.AddRow(2, "Order2", 61, 3, "Bass", 2380.00m, 50.00m);
		//	rs.AddRow(3, "Order3", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);

		//	var db = base.CreateDB_ForQuery(rs);
		//	var query = db.Query<Order>().Graph();

		//	var qbPath = query.BuildEntityTypePath();

		//	Assert.AreEqual("Order-OrderItem-Receipt", qbPath);
		//}
	}
}
