using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data.UnitTests.Entities
{
    public class Person
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsHappy { get; set; }
        public List<Pet> Pets { get; set; }
    }

    public class Pet
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}