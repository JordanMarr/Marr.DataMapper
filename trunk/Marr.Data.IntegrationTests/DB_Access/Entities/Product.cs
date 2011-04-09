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

        [Column(Size=100)]
        public string ImageFileName { get; set; }

        /// <summary>
        /// Gets the product slug (url friendly name).
        /// Ex: Name = "my blue dog" returns "my-blue-dog".
        /// </summary>
        public string Slug
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                {
                    return string.Empty;
                }
                else
                {
                    return Name
                        .Replace(" ", "-")
                        .Replace(",", "-");
                }
            }
        }

        [Column]
        public bool NewItem { get; set; }

        [Column]
        public bool IsSplash { get; set; }

        [Relationship]
        public Category Category { get; set; }

        [Relationship]
        public List<OptionType> OptionTypes { get; set; }
        

    }
}
