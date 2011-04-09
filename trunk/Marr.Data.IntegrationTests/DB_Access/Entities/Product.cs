using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Marr.Data.Mapping;

namespace Marr.Data.IntegrationTests.DB_Access.Entities
{
    [Table("Product")]
    public class Product
    {
        [Column(IsPrimaryKey = true, IsAutoIncrement = true)]
        public int ID { get; set; }

        [Column(Size=50)]
        public string Name { get; set; }

        [Column(Size=200)]
        public string Description { get; set; }

        [Column]
        public decimal Price { get; set; }

        [Column]
        public int? CategoryID { get; set; }

        [Column]
        public bool NewItem { get; set; }

        [Relationship]
        public Category Category { get; set; }

        [Relationship]
        public List<OptionType> OptionTypes { get; set; }
        

    }
}
