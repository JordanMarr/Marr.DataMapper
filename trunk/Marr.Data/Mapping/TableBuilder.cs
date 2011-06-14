using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data.Mapping
{
    /// <summary>
    /// This class has fluent methods that are used to easily configure the table mapping.
    /// </summary>
    public class TableBuilder<T>
    {
        #region - Fluent Methods -

        public TableBuilder<T> SetTableName(string tableName)
        {
            MapRepository.Instance.Tables[typeof(T)] = tableName;
            return this;
        }

        #endregion
    }
}
