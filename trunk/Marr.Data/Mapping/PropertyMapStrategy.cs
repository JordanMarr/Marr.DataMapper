using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Marr.Data.Mapping
{
    public class PropertyMapStrategy : ReflectionColumnMapStrategyBase
    {
        public PropertyMapStrategy(bool publicOnly)
            : base(publicOnly)
        { }

        protected override void CreateColumnMap(Type entityType, System.Reflection.MemberInfo member, ColumnMapCollection columnMaps)
        {
            if (member.MemberType == MemberTypes.Property)
            {
                columnMaps.Add(new ColumnMap(member));
            }
        }
    }
}
