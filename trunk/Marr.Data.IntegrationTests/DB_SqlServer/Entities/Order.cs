using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data.IntegrationTests.DB_SqlServer.Entities
{
    [Table("Order")]
    public class Order
    {
        [Column(IsPrimaryKey = true, IsAutoIncrement=true, ReturnValue=true, AltName = "oID")]
        public int ID { get; set; }

        [Column(AltName = "oOrderName")]
        public string OrderName { get; set; }

        [Relationship] // 1-M
        public List<OrderItem> OrderItems { get; set; }
    }

    [Table("OrderItem")]
    public class OrderItem
    {
        [Column("ID", AltName = "oiID", IsPrimaryKey = true, IsAutoIncrement=true, ReturnValue=true)]
        public int ID { get; set; }

        [Column(AltName = "oiOrderID")]
        public int OrderID { get; set; }

        [Column(AltName = "oiItemDescription")]
        public string ItemDescription { get; set; }

        [Column(AltName = "oiPrice")]
        public decimal Price { get; set; }

        [Relationship] // 1-1
        public Receipt ItemReceipt { get; set; }
    }

    [Table("Receipt")]
    public class Receipt
    {
        [Column(AltName = "rOrderItemID")]
        public int OrderItemID { get; set; }

        [Column(AltName = "rAmountPaid")]
        public decimal? AmountPaid { get; set; }
    }

}