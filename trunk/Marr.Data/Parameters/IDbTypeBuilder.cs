using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Marr.Data.Mapping;

namespace Marr.Data.Parameters
{
    /// <summary>
    /// Converts from a .NET datatype to the appropriate DB type enum, 
    /// and then adds the value to the appropriate property on the parameter.
    /// </summary>
    public interface IDbTypeBuilder
    {
        Enum GetDbType(Type type);
        void SetDbType(IDbDataParameter param, ColumnMap column);
    }
}
