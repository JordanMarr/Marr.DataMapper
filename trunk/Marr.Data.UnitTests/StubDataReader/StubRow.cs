using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data.UnitTests
{
    public class StubRow
    {
        object[] _rowValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubRow"/> class.
        /// </summary>
        /// <param name="values">The values.</param>
        public StubRow(params object[] values)
        {
            this._rowValues = values;
        }

        /// <summary>
        /// Gets the <see cref="Object"/> with the specified i.
        /// </summary>
        /// <value></value>
        public object this[int i]
        {
            get
            {
                return _rowValues[i];
            }
        }
    }

}
