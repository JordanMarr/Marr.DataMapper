using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Marr.Data.Converters;
using Marr.Data.Parameters;

namespace Marr.Data.Mapping
{
    /// <summary>
    /// Maps fields or properties that are marked with the ColumnAttribute.
    /// </summary>
    public class AttributeColumnMapStrategy : ReflectionColumnMapStrategyBase
    {
        protected override void CreateColumnMap(Type entityType, MemberInfo member, ColumnMapCollection columnMaps)
        {
            if (member.IsDefined(typeof(ColumnAttribute), false))
            {
                ColumnAttribute column = (ColumnAttribute)member.GetCustomAttributes(typeof(ColumnAttribute), false)[0];
                ColumnMap columnMap = new ColumnMap(member, column);
                columnMaps.Add(columnMap);
            }
        }
    }
}
