using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data.Tests.Entities
{
    public class Person
    {
        [Column(IsPrimaryKey = true, IsAutoIncrement = true, ReturnValue = true)]
        public int ID { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public int Age { get; set; }

        [Column]
        public DateTime BirthDate { get; set; }

        [Column]
        public bool IsHappy { get; set; }

    }
}