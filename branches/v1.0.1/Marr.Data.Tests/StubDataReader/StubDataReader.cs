using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;

namespace Marr.Data.Tests
{
    /// <summary>
    /// This class fakes up a data reader.
    /// </summary>
    public class StubDataReader : DbDataReader
    {
        IList<StubResultSet> _stubResultSets;
        private int _currentResultsetIndex = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubDataReader"/> class. 
        /// Each row in the arraylist is a result set.
        /// </summary>
        /// <param name="stubResultSets">The result sets.</param>
        public StubDataReader(IList<StubResultSet> stubResultSets)
        {
            this._stubResultSets = stubResultSets;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StubDataReader"/> class. 
        /// Each row in the arraylist is a result set.
        /// </summary>
        /// <param name="resultSets">The result sets to add.</param>
        public StubDataReader(params StubResultSet[] resultSets)
        {
            this._stubResultSets = new List<StubResultSet>();
            foreach (StubResultSet resultSet in resultSets)
            {
                this._stubResultSets.Add(resultSet);
            }
        }

        public override void Close()
        {
        }

        public override bool NextResult()
        {
            if (_currentResultsetIndex >= this._stubResultSets.Count)
                return false;

            return (++_currentResultsetIndex < this._stubResultSets.Count);
        }

        public override bool Read()
        {
            return CurrentResultSet.Read();
        }

        public override DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating the depth of nesting for the current row.
        /// </summary>
        /// <value></value>
        public override int Depth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsClosed
        {
            get { return false; }
        }

        public override int RecordsAffected
        {
            get { return 1; }
        }

        public void Dispose()
        {
        }

        public override string GetName(int i)
        {
            return CurrentResultSet.GetFieldNames()[i];
        }

        public override string GetDataTypeName(int i)
        {
            return this.CurrentResultSet.GetFieldNames()[i];
        }

        public override Type GetFieldType(int i)
        {
            //KLUDGE: Since we're dynamically creating this, I'll have to 
            //		  look at the actual data to determine this.
            //		  We'll loook at the first row since it's the most likely 
            //			to have data.
            return this._stubResultSets[0][i].GetType();
        }

        public override object GetValue(int i)
        {
            return CurrentResultSet[i];
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override int GetOrdinal(string name)
        {
            return this.CurrentResultSet.GetIndexFromFieldName(name);
        }

        public override bool GetBoolean(int i)
        {
            return (bool)CurrentResultSet[i];
        }

        public override byte GetByte(int i)
        {
            return (byte)CurrentResultSet[i];
        }

        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            //TODO: Need to test this method.

            byte[] totalBytes = (byte[])CurrentResultSet[i];

            int bytesRead = 0;
            for (int j = 0; j < length; j++)
            {
                long readIndex = fieldOffset + j;
                long writeIndex = bufferoffset + j;
                if (totalBytes.Length <= readIndex)
                    throw new ArgumentOutOfRangeException("fieldOffset", string.Format("Trying to read index '{0}' is out of range. (fieldOffset '{1}' + current position '{2}'", readIndex, fieldOffset, j));

                if (buffer.Length <= writeIndex)
                    throw new ArgumentOutOfRangeException("bufferoffset", string.Format("Trying to write to buffer index '{0}' is out of range. (bufferoffset '{1}' + current position '{2}'", readIndex, bufferoffset, j));

                buffer[writeIndex] = totalBytes[readIndex];
                bytesRead++;
            }
            return bytesRead;
        }

        public override char GetChar(int i)
        {
            return (char)CurrentResultSet[i];
        }

        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int i)
        {
            return (Guid)CurrentResultSet[i];
        }

        public override short GetInt16(int i)
        {
            return (short)CurrentResultSet[i];
        }

        public override int GetInt32(int i)
        {
            return (int)CurrentResultSet[i];
        }

        public override long GetInt64(int i)
        {
            return (long)CurrentResultSet[i];
        }

        public override float GetFloat(int i)
        {
            return (float)CurrentResultSet[i];
        }

        public override double GetDouble(int i)
        {
            return (double)CurrentResultSet[i];
        }

        public override string GetString(int i)
        {
            return (string)CurrentResultSet[i];
        }

        public override decimal GetDecimal(int i)
        {
            return (decimal)CurrentResultSet[i];
        }

        public override DateTime GetDateTime(int i)
        {
            return (DateTime)CurrentResultSet[i];
        }

        public DbDataReader GetData(int i)
        {
            StubDataReader reader = new StubDataReader(this._stubResultSets);
            reader._currentResultsetIndex = i;
            return reader;
        }

        public override bool IsDBNull(int i)
        {
            //TODO: Deal with value types.
            return null == CurrentResultSet[i];
        }

        public override int FieldCount
        {
            get
            {
                return CurrentResultSet.GetFieldNames().Length;
            }
        }

        public override object this[int i]
        {
            get
            {
                return CurrentResultSet[i];
            }
        }

        public override object this[string name]
        {
            get
            {
                return CurrentResultSet[name];
            }
        }

        private StubResultSet CurrentResultSet
        {
            get
            {
                return this._stubResultSets[_currentResultsetIndex];
            }
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override bool HasRows
        {
            get 
            {
                return _stubResultSets.Count > 0;
            }
        }
    }

}
