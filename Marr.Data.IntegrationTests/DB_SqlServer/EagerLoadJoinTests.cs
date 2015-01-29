using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.IntegrationTests.DB_SqlServer.Entities;
using Marr.Data.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Marr.Data.IntegrationTests.DB_SqlServer
{
	[TestClass]
	public class EagerLoadJoinTests : TestBase
	{
		IDataMapper _db;

		[TestInitialize]
		public void TestInitialize()
		{
			CreateMappings();
			_db = CreateSqlServerDB();
			SeedDB();
			MapRepository.Instance.EnableTraceLogging = true;
		}

		[TestCleanup]
		public void TestCleanup()
		{
			try
			{
				MapRepository.Instance.EnableTraceLogging = false;
				_db.Delete<FluentMappedReceipt>(r => r.OrderItemID > 0);
				_db.Delete<FluentMappedOrderItem>(oi => oi.ID > 0);
				_db.Delete<FluentMappedOrder>(o => o.ID > 0);
			}
			finally
			{
				_db.Dispose();
			}
		}

		#region - Mappings / Seed DB -

		private void CreateMappings()
		{
			var mappings = new FluentMappings();
			mappings
				.Entity<FluentMappedOrder>()
					.Table.MapTable("Order")
					.Columns.AutoMapSimpleTypeProperties()
						.PrefixAltNames("o")
						.For(e => e.ID).SetPrimaryKey().SetAutoIncrement().SetReturnValue()
					.Relationships.MapProperties()
						.For(e => e.OrderItems).JoinMany<FluentMappedOrderItem>(o => o.OrderItems, (o, oi) => o.ID == oi.OrderID)

				.Entity<FluentMappedOrderItem>()
					.Table.MapTable("OrderItem")
					.Columns.AutoMapSimpleTypeProperties()
						.PrefixAltNames("oi")
						.For(oi => oi.ID).SetPrimaryKey().SetAutoIncrement().SetReturnValue()
					.Relationships.MapProperties()
						.For(oi => oi.ItemReceipt).JoinOne<FluentMappedReceipt>(oi => oi.ItemReceipt, (oi, r) => oi.ID == r.OrderItemID)

				.Entity<FluentMappedReceipt>()
					.Table.MapTable("Receipt")
					.Columns.AutoMapSimpleTypeProperties()
						.PrefixAltNames("r");
		}

		private void SeedDB()
		{
			var order1 = new FluentMappedOrder
			{
				OrderName = "Order 1"
			};

			var order2 = new FluentMappedOrder
			{
				OrderName = "Order 2"
			};

			_db.Insert<FluentMappedOrder>(order1);

			_db.Insert<FluentMappedOrder>(order2);

			foreach (var order in (new[] { order1, order2 }))
			{
				for (var i = 1; i < 3; i++)
				{
					var oi = new FluentMappedOrderItem
					{
						ItemDescription = order.OrderName + " - Item " + i.ToString(),
						OrderID = order.ID,
						Price = 5.5m
					};

					_db.Insert<FluentMappedOrderItem>(oi);

					var receipt = new FluentMappedReceipt
					{
						OrderItemID = oi.ID,
						AmountPaid = 5.5m
					};

					_db.Insert(receipt);
				}
			}
		}

		#endregion

		[TestMethod]
		public void WhenLoadingASingleOrder_ShouldEagerLoadChildren()
		{
			var order = _db.Query<FluentMappedOrder>()
						.Graph()
						.Where(o => o.OrderName == "Order 1")
						.FirstOrDefault();

			Assert.AreEqual(2, order.OrderItems.Count);

			foreach (var oi in order.OrderItems)
			{
				Assert.IsNotNull(oi.ItemReceipt);
				Assert.AreEqual(5.5m, oi.ItemReceipt.AmountPaid);
			}
		}

		[TestMethod]
		public void WhenLoadingMultipleOrders_ShouldEagerLoadChildren()
		{
			var orders = _db.Query<FluentMappedOrder>()
						.Graph()
						.ToArray();

			Assert.AreEqual(2, orders.Length);
			foreach (var order in orders)
			{
				Assert.AreEqual(2, order.OrderItems.Count);
				foreach (var oi in order.OrderItems)
				{
					Assert.IsNotNull(oi.ItemReceipt);
					Assert.AreEqual(5.5m, oi.ItemReceipt.AmountPaid);
				}
			}
		}

		/* * * * * * *
		 * The following test ensure that the eager load mechanism honors the Graph() method.
		 */

		[TestMethod]
		public void WhenGraphIsNotCalled_OnlyRootEntityShouldBeLoaded()
		{
			var orders = _db.Query<FluentMappedOrder>()
						.ToArray();

			Assert.IsTrue(orders.Any());
			var order = orders.First();
			Assert.IsNull(order.OrderItems, "The order items should not have been eager loaded because 'Graph()' wasn't called.");
		}

		[TestMethod]
		public void WhenGraphIsCalledWithOrderItemParameter_ReceiptsShouldNotBeLoaded()
		{
			var orders = _db.Query<FluentMappedOrder>()
						.Graph(o => o.OrderItems)
						.ToArray();

			Assert.IsTrue(orders.Any());
			var order = orders.First();
			Assert.IsNotNull(order.OrderItems, "The order items should have been eager loaded because 'Graph()' was called.");

			var orderItem = order.OrderItems.First();
			Assert.IsNull(orderItem.ItemReceipt, "The receipt should not have been eager loaded because 'Graph('OrderItem')' was called.");
		}

		[TestMethod]
		public void WhenGraphIsCalledWithNoParameters_EntireGraphShouldBeLoaded()
		{
			var orders = _db.Query<FluentMappedOrder>()
						.Graph()
						.ToArray();

			Assert.IsTrue(orders.Any());
			var order = orders.First();
			Assert.IsNotNull(order.OrderItems, "The order items should have been eager loaded because 'Graph()' was called.");

			var orderItem = order.OrderItems.First();
			Assert.IsNotNull(orderItem.ItemReceipt, "The receipt should have been eager loaded because 'Graph()' was called.");
		}
	}
}
