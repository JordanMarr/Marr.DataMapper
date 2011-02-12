using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data.Mapping
{
    /// <summary>
    /// Creates the ColumnMapCollection for a given entity type.
    /// </summary>
    public interface IColumnMapStrategy
    {
        ColumnMapCollection CreateColumnMaps(Type entityType);
    }
}
