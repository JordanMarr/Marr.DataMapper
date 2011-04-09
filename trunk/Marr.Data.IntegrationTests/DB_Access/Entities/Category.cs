using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using Marr.Data.Mapping;

namespace Marr.Data.IntegrationTests.DB_Access.Entities
{
    [Table("Category")]
    public class Category
    {

        [Column(IsPrimaryKey = true, IsAutoIncrement = true, AltName = "CategoryID")]
        public int ID { get; internal set; }

        [Column(AltName = "CategoryName")]
        public string Name { get; set; }
        
    }
}