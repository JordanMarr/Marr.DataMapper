using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Marr.Data.Mapping;
using System.Data;

namespace Marr.Data.IntegrationTests.DB_Access.Entities
{
    public class OptionType
    {
        [Relationship]
        private List<Option> _Options;

        [Column("OptionTypeID", IsPrimaryKey = true, IsAutoIncrement = true)]
        public int ID { get; internal set; }

        [Column]
        public int CategoryID { get; set; }

        [Column]
        public string Type { get; set; }

        [Column]
        public bool MultiPick { get; set; }

        public List<Option> Options
        {
            get { return _Options; }
        }
    }
}
