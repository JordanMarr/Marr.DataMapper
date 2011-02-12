using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Marr.Data.Mapping
{
    /// <summary>
    /// Iterates through the members of an entity based on the BindingFlags, and provides an abstract method for adding ColumnMaps for each member.
    /// </summary>
    public abstract class ReflectionColumnMapStrategyBase : IColumnMapStrategy
    {
        private BindingFlags _bindingFlags;

        /// <summary>
        /// Loops through members with the following BindingFlags:
        /// Instance | NonPublic | Public | FlattenHierarchy
        /// </summary>
        public ReflectionColumnMapStrategyBase()
        {
            _bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;
        }

        /// <summary>
        /// Loops through members with the following BindingFlags:
        /// Instance | Public | FlattenHierarchy | NonPublic (optional)
        /// </summary>
        /// <param name="publicOnly"></param>
        public ReflectionColumnMapStrategyBase(bool publicOnly)
        {
            if (publicOnly)
            {
                _bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            }
            else
            {
                _bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            }
        }

        /// <summary>
        /// Loops through members based on the passed in BindingFlags.
        /// </summary>
        /// <param name="bindingFlags"></param>
        public ReflectionColumnMapStrategyBase(BindingFlags bindingFlags)
        {
            _bindingFlags = bindingFlags;
        }

        /// <summary>
        /// Implements IColumnMappingStrategy.
        /// Loops through filtered members and calls the virtual "CreateColumnMap" void for each member.
        /// Subclasses can override CreateColumnMap to customize adding ColumnMaps.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public ColumnMapCollection CreateColumnMaps(Type entityType)
        {
            ColumnMapCollection columnMaps = new ColumnMapCollection();

            MemberInfo[] members = entityType.GetMembers(_bindingFlags);
            foreach (var member in members)
            {
                CreateColumnMap(entityType, member, columnMaps);
            }

            if (columnMaps.Count == 0)
            {
                string msg = string.Format("Warning: The '{0}' did not add any column mappings for the '{1}' entity.", this.GetType(), entityType.Name);
                throw new Exception(msg);
            }

            return columnMaps;
        }

        
        /// <summary>
        /// The default behavior adds a new column map for every iterated property,
        /// and maps it to a database column of the exact same name.
        /// </summary>
        /// <param name="entityType">The entity type that is being mapped.</param>
        /// <param name="member">The member that is being mapped.</param>
        /// <param name="columnMaps">The ColumnMapCollection that is being created.</param>
        protected abstract void CreateColumnMap(Type entityType, MemberInfo member, ColumnMapCollection columnMaps);
    }
}
