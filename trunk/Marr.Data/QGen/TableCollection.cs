using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;

namespace Marr.Data.QGen
{
    /// <summary>
    /// This class holds a collection of Table objects.
    /// </summary>
    public class TableCollection : IEnumerable<Table>
    {
        private List<Table> _tables;

        public TableCollection()
        {
            _tables = new List<Table>();
        }

        public void Add(Table table)
        {
            if (this.Any(t => t.EntityType == table.EntityType))
            {
                // Already exists -- don't add
                return;
            }

            // Create an alias (ex: "t0", "t1", "t2", etc...)
            table.Alias = string.Format("t{0}", _tables.Count);
            _tables.Add(table);
        }

        public void ReplaceBaseTable(View view)
        {
            _tables.RemoveAt(0);
            Add(view);
        }

        /// <summary>
        /// Tries to find a table for a given member.  
        /// </summary>
        public Table FindTable(MemberInfo member)
        {
            return this.FirstOrDefault(t => t.EntityType == member.DeclaringType);
        }

        public Table this[int index]
        {
            get
            {
                return _tables[index];
            }
        }

        public int Count
        {
            get
            {
                return _tables.Count;
            }
        }

        public IEnumerator<Table> GetEnumerator()
        {
            return _tables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tables.GetEnumerator();
        }
    }
}
