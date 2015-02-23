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

			Assert.AreEqual(1, rtl.TypePath.Count);
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

			Assert.AreEqual(2, rtl.TypePath.Count);
			Assert.AreEqual("OrderItem-Receipt", rtl.BuildEntityTypePath());
		}

		[TestMethod]
		public void RelationshipToLoad_ShouldParseNestedFirstExpressions()
		{
			Expression<Func<Order, object>> loadExp = 
				o => o.OrderItems.First().ItemReceipt;

			var rtl = new RelationshipLoadRequest(loadExp);

			Assert.AreEqual(2, rtl.TypePath.Count);
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
		public void EntityTypePath_ShouldHandle_OneToOne_Relationships()
		{
			Expression<Func<Invoice, object>> loadExp =
				i => i.Header.Customer;

			var rtl = new RelationshipLoadRequest(loadExp);

			Assert.AreEqual(2, rtl.TypePath.Count);
			Assert.AreEqual("InvoiceHeader-Customer", rtl.BuildEntityTypePath());
		}

		private class Invoice
		{
			public int ID { get; set; }
			public int Number { get; set; }

			public InvoiceHeader Header { get; set; }
		}

		private class InvoiceHeader
		{
			public DateTime Date { get; set; }

			public Customer Customer { get; set; }
		}

		public class Customer
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
		}
	}

	
}
