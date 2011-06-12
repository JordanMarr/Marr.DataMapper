using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data.QGen
{
    /// <summary>
    /// This class represents a View.  A view can hold multiple tables (and their columns).
    /// </summary>
    public class View : Table
    {
        private string _viewName;
        private Table[] _tables;
        private Mapping.ColumnMapCollection _columns;

        public View(string viewName, Table[] tables)
            : base(tables[0].EntityType, JoinType.None)
        {
            _viewName = viewName;
            _tables = tables;
        }

        public override string Name
        {
            get
            {
                return _viewName;
            }
            set
            {
                _viewName = value;
            }
        }

        /// <summary>
        /// Gets all the columns from all the tables included in the view.
        /// </summary>
        public override Mapping.ColumnMapCollection Columns
        {
            get
            {
                if (_columns == null)
                {
                    var allColumns = _tables.SelectMany(t => t.Columns);
                    _columns = new ColumnMapCollection();
                    _columns.AddRange(allColumns);
                }

                return _columns;
            }
        }
    }
}
