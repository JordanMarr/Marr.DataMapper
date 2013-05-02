using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data.UnitTests.Entities
{
    /// <summary>
    /// A person with no mapping attributes.
    /// </summary>
    public class UnmappedPerson
    {
        private string PrivateName { get; set; }

        public int ID { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsHappy { get; set; }

        public List<UnmappedPet> Pets { get; set; }
    }

    public class UnmappedPet
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

}
