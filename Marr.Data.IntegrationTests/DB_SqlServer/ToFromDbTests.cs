﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Marr.Data.IntegrationTests.DB_SqlServer
{
	[TestClass]
	public class ToFromDbTests : TestBase
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
				//_db.Delete<Entities.FluentMappedReceipt>(r => r.OrderItemID > 0);
				//_db.Delete<Entities.FluentMappedOrderItem>(oi => oi.ID > 0);
				_db.Delete<Entities.FluentMappedOrder>(o => o.ID > 0);
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
				.Entity<Entities.FluentMappedOrder>()
					.Table.MapTable("Order")
					.Columns.AutoMapSimpleTypeProperties()
						.For(e => e.ID)
							.SetPrimaryKey().SetAutoIncrement().SetReturnValue()
						.For(e => e.OrderName)
							.ToDB(o => (o as string) + "[ToDB]")
							.FromDB(o => (o as string) + "[FromDB]");
		}

		private void SeedDB()
		{
			var order1 = new Entities.FluentMappedOrder
			{
				OrderName = "Order 1"
			};

			_db.Insert<Entities.FluentMappedOrder>(order1);
		}

		#endregion

		[TestMethod]
		public void WhenLoadingASingleOrder_ShouldEagerLoadChildren()
		{
			var order = _db.Query<Entities.FluentMappedOrder>()
						.Where(o => o.OrderName == "Order 1[ToDB]")
						.FirstOrDefault();

			Assert.IsNotNull(order);
			Assert.AreEqual("Order 1[ToDB][FromDB]", order.OrderName);
		}
	}
}
