using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data.UnitTests.Entities
{
    [Table("PersonTable")]
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

        [Relationship]
        public List<Pet> Pets { get; set; }
    }

    public class Pet
    {
        [Column("ID",  AltName="Pet_ID", IsPrimaryKey=true)]
        public int ID { get; set; }

        [Column("Name", AltName = "Pet_Name")]
        public string Name { get; set; }
    }
}