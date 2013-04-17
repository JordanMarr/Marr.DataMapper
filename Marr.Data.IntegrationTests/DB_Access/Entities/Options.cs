using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Marr.Data.Mapping;
using System.Data;

namespace Marr.Data.IntegrationTests.DB_Access.Entities
{
    public class Option
    {
        [Column("OptionID", IsPrimaryKey = true, IsAutoIncrement = true)]
        public int ID { get; internal set; }

        [Column]
        public int OptionTypeID { get; set; }

        [Column]
        public string Type { get; set; }

        [Column("OptionDescription")]
        public string Description { get; set; }

        [Column("OptionPrice")]
        public decimal Price { get; set; }
    }
}
