using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data.TestHelper
{
    /// <summary>
    /// Creates a stubbed result set by working backwards from a set of entities.
    /// </summary>
    public class StubEntityResultSet : StubResultSet
    {
        private List<ColumnMapInfo> _columns;
        private List<Type> _types;
        private int _columnIndex;

        public StubEntityResultSet()
        {
            _columns = new List<ColumnMapInfo>();
            _types = new List<Type>();
        }

        /// <summary>
        /// Adds the values of a single entity to the stubbed result set.
        /// </summary>
        /// <param name="entity">A single entity that already has defined data mappings.</param>
        public void AddEntity(object entity)
        {
            AddEntityWithChildren(entity);
        }

        /// <summary>
        /// Adds an entity and its child entities to the stubbed result set.
        /// NOTE: All entity values will be added to a single stubbed row.
        /// </summary>
        /// <param name="entities">A set of parent and child entities that will create a single stubbed row.</param>
        public void AddEntityWithChildren(params object[] entities)
        {
            var reflector = new Marr.Data.Reflection.SimpleReflectionStrategy();

            List<object> stubbedValues = new List<object>();

            foreach (object entity in entities)
            {
                Type entityType = entity.GetType();
                AddFieldNames(entityType);

                var mappings = Marr.Data.MapRepository.Instance.GetColumns(entity.GetType());
                if (mappings.Count == 0)
                {
                    string error = string.Format("The entity type '{0}' does not have any column mappings.", entityType.Name);
                    throw new ArgumentException(error);
                }

                foreach (var column in _columns.Where(c => c.Type == entityType))
                {
                    ColumnMap mapping = null;

                    if (!column.IsAlt)
                    {
                        mapping = mappings.Where(m => m.ColumnInfo.Name == column.Map.ColumnInfo.Name).FirstOrDefault();
                    }
                    else
                    {
                        mapping = mappings.Where(m => m.ColumnInfo.AltName == column.Map.ColumnInfo.AltName).FirstOrDefault();
                    }

                    if (mapping != null)
                    {
                        object val = reflector.GetFieldValue(entity, column.Map.FieldName);
                        stubbedValues.Add(val);
                    }
                }
            }

            AddRow(stubbedValues.ToArray());
            _columnIndex = 0;
        }

        private void AddFieldNames(Type type)
        {
            if (_types.Contains(type))
            {
                return;
            }

            _types.Add(type);

            ColumnMapCollection columns = Marr.Data.MapRepository.Instance.GetColumns(type);

            foreach (ColumnMap column in columns)
            {
                if (!_columns.Exists(c => !c.IsAlt && c.Map.ColumnInfo.Name == column.ColumnInfo.Name))
                {
                    _fieldNames.Add(column.ColumnInfo.Name, _columnIndex++);
                    _columns.Add(new ColumnMapInfo(column, false, type));
                }

                if (column.ColumnInfo.AltName != null && !_columns.Exists(c => c.IsAlt && c.Map.ColumnInfo.AltName == column.ColumnInfo.AltName))
                {
                    _fieldNames.Add(column.ColumnInfo.AltName, _columnIndex++);
                    _columns.Add(new ColumnMapInfo(column, true, type));
                }
            }
        }
    }
    
    internal class ColumnMapInfo
    {
        public ColumnMapInfo(ColumnMap column, bool isAlt, Type type)
        {
            Map = column;
            IsAlt = isAlt;
            Type = type;
        }

        public ColumnMap Map { get; set; }
        public bool IsAlt { get; set; }
        public Type Type { get; set; }
    }
}
