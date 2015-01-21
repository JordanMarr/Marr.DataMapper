using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.IntegrationTests.DB_SqlServer.Entities;

namespace Marr.Data.IntegrationTests.DB_SqlServer
{
	/// <summary>
	/// Tests the IQueryable implementation.
	/// </summary>
	[TestClass]
	public class QueryableTests : TestBase
	{
		[TestInitialize]
		public void Setup()
		{
			MapRepository.Instance.EnableTraceLogging = true;
		}

		[TestMethod]
		public void IQueryableShouldFilterAndSortAsc()
		{
			using (var db = CreateSqlServerDB())
			{
				try
				{
					db.SqlMode = SqlModes.Text;
					db.BeginTransaction();

					var existingOrders = db.Query<Order>().Where(o => o.ID > 0).ToList();
					int count = existingOrders.Count;
					db.Delete<Order>(o => o.ID > 0);

					Order order1 = new Order { OrderName = "Test1" };
					db.Insert(order1);

					Order order2 = new Order { OrderName = "Test2" };
					db.Insert(order2);

					Order order3 = new Order { OrderName = "Test3" };
					db.Insert(order3);

					var orders = db.Querable<Order>()
						.Where(o => o.OrderName == "Test1" || o.OrderName == "Test2")
						.OrderBy(o => o.OrderName)
						.ToArray();

					Assert.AreEqual(2, orders.Length);
					var o1 = orders[0];
					var o2 = orders[1];

					Assert.AreEqual(o1.OrderName, "Test1");
					Assert.AreEqual(o2.OrderName, "Test2");
				}
				catch
				{
					throw;
				}
				finally
				{
					db.RollBack();
				}
			}
		}

		[TestMethod]
		public void IQueryableShouldFilterAndSortDesc()
		{
			using (var db = CreateSqlServerDB())
			{
				try
				{
					db.SqlMode = SqlModes.Text;
					db.BeginTransaction();

					var existingOrders = db.Query<Order>().Where(o => o.ID > 0).ToList();
					int count = existingOrders.Count;
					db.Delete<Order>(o => o.ID > 0);

					Order order1 = new Order { OrderName = "Test1" };
					db.Insert(order1);

					Order order2 = new Order { OrderName = "Test2" };
					db.Insert(order2);

					Order order3 = new Order { OrderName = "Test3" };
					db.Insert(order3);
					
					var orders = db.Querable<Order>()
						.Where(o => o.OrderName == "Test1" || o.OrderName == "Test2")
						.OrderByDescending(o => o.OrderName)
						.ToArray();

					Assert.AreEqual(2, orders.Length);
					var o1 = orders[0];
					var o2 = orders[1];

					Assert.AreEqual(o1.OrderName, "Test2");
					Assert.AreEqual(o2.OrderName, "Test1");
				}
				catch
				{
					throw;
				}
				finally
				{
					db.RollBack();
				}
			}
		}

		[TestMethod]
		public void IQueryableShouldHandleFilterWithTwoSorts()
		{
			using (var db = CreateSqlServerDB())
			{
				try
				{
					db.SqlMode = SqlModes.Text;
					db.BeginTransaction();

					// Clear out any existing records
					var existingOrders = db.Query<Order>().Where(o => o.ID > 0).ToList();
					int count = existingOrders.Count;
					db.Delete<Order>(o => o.ID > 0);

					Order order1 = new Order { OrderName = "GroupA" };
					db.Insert<Order>(order1);

					Order order2 = new Order { OrderName = "GroupB" };
					db.Insert<Order>(order2);

					Order order3 = new Order { OrderName = "GroupA" };
					db.Insert<Order>(order3);

					Order order4 = new Order { OrderName = "Test" };
					db.Insert<Order>(order4);

					var orders = db.Querable<Order>()
						.Where(o => o.OrderName.StartsWith("Group"))
						.OrderBy(o => o.OrderName)
						.ThenByDescending(o => o.ID)
						.ToArray();

					// Test where clause
					Assert.AreEqual(3, orders.Length);
					var r1 = orders[0];
					var r2 = orders[1];
					var r3 = orders[2];

					// Test primary sort
					Assert.AreEqual(r1.OrderName, "GroupA");
					Assert.AreEqual(r2.OrderName, "GroupA");
					Assert.AreEqual(r3.OrderName, "GroupB");

					// Test secondary sort
					Assert.AreEqual(order3.ID, r1.ID);
					Assert.AreEqual(order1.ID, r2.ID);
					Assert.AreEqual(order2.ID, r3.ID);
				}
				catch
				{
					throw;
				}
				finally
				{
					db.RollBack();
				}
			}
		}
	}
}
