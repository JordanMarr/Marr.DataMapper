using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data.IntegrationTests.DB_SqlServer.Entities
{
	public class FluentMappedOrder
	{
		public int ID { get; set; }
		public string OrderName { get; set; }
		public List<FluentMappedOrderItem> OrderItems { get; set; }
	}

	public class FluentMappedOrderItem
	{
		public int ID { get; set; }
		public int OrderID { get; set; }
		public string ItemDescription { get; set; }
		public decimal Price { get; set; }
		public FluentMappedReceipt ItemReceipt { get; set; }
	}

	public class FluentMappedReceipt
	{
		public int OrderItemID { get; set; }
		public decimal? AmountPaid { get; set; }
	}
}
