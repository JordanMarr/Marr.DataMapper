using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data.UnitTests.Entities
{
    [Table("Order")]
    public class Order
    {
        [Column(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Column]
        public string OrderName { get; set; }

        [Relationship] // 1-M
        public List<OrderItem> OrderItems { get; set; }
    }

    public class OrderItem
    {
        [Column("ID", AltName = "OrderItemID", IsPrimaryKey = true)]
        public int ID { get; set; }

        [Column]
        public int OrderID { get; set; }

        [Column]
        public string ItemDescription { get; set; }

        [Column]
        public decimal Price { get; set; }

        [Relationship] // 1-1
        public Receipt ItemReceipt { get; set; }
    }

    public class Receipt
    {
        [Column]
        public decimal? AmountPaid { get; set; }
    }

}