using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data.UnitTests
{
    /// <summary>
    /// Represents a result set for the StubDataReader.
    /// </summary>
    public class StubResultSet
    {
        int _currentRowIndex = -1;
        List<StubRow> _rows = new List<StubRow>();
        Dictionary<string, int> _fieldNames = new Dictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StubResultSet"/> class with the column names.
        /// </summary>
        /// <param name="fieldNames">The column names.</param>
        public StubResultSet(params string[] fieldNames)
        {
            for (int i = 0; i < fieldNames.Length; i++)
            {
                this._fieldNames.Add(fieldNames[i], i);
            }
        }

        public string[] GetFieldNames()
        {
            string[] keys = new string[_fieldNames.Keys.Count];
            _fieldNames.Keys.CopyTo(keys, 0);
            return keys;
        }

        public string GetFieldName(int i)
        {
            return GetFieldNames()[i];
        }

        /// <summary>
        /// Adds the row.
        /// </summary>
        /// <param name="values">The values.</param>
        public void AddRow(params object[] values)
        {
            if (values.Length != _fieldNames.Count)
            {
                throw new ArgumentOutOfRangeException("values", string.Format("The Row must contain '{0}' items", _fieldNames.Count));
            }
            _rows.Add(new StubRow(values));
        }

        public int GetIndexFromFieldName(string name)
        {
            if (!this._fieldNames.ContainsKey(name))
                throw new IndexOutOfRangeException(string.Format("The key '{0}' was not found in this data reader.", name));
            return this._fieldNames[name];
        }

        public bool Read()
        {
            return ++this._currentRowIndex < this._rows.Count;
        }

        public StubRow CurrentRow
        {
            get
            {
                return this._rows[this._currentRowIndex];
            }
        }

        public object this[string key]
        {
            get
            {
                return CurrentRow[GetIndexFromFieldName(key)];
            }
        }

        public object this[int i]
        {
            get
            {
                return CurrentRow[i];
            }
        }

        public void Reset()
        {
            _currentRowIndex = -1;
        }
    }
}
