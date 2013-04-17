using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data.Tests.Entities
{
    /// <summary>
    /// This object has some column names that are different from the property name (ie PersonName, PersonAge).
    /// </summary>
    public class Person2
    {
        [Column(IsPrimaryKey = true, IsAutoIncrement = true, ReturnValue = true)]
        public int ID { get; set; }

        [Column("PersonName")]
        public string Name { get; set; }

        [Column("PersonAge")]
        public int Age { get; set; }

        [Column]
        public DateTime BirthDate { get; set; }

        [Column]
        public bool IsHappy { get; set; }


    }
}
