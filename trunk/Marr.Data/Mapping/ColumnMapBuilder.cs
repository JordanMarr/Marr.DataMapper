using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data;
using Marr.Data.Mapping.Strategies;

namespace Marr.Data.Mapping
{
    /// <summary>
    /// This class has fluent methods that are used to easily configure column mappings.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ColumnMapBuilder<T>
    {
        private string _currentPropertyName;

        public ColumnMapBuilder(ColumnMapCollection columns)
        {
            Columns = columns;
        }

        /// <summary>
        /// Gets the list of column mappings that are being configured.
        /// </summary>
        public ColumnMapCollection Columns { get; private set; }

        #region - Fluent Methods -

        /// <summary>
        /// Initializes the configurator to configure the given property.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public ColumnMapBuilder<T> For(Expression<Func<T, object>> property)
        {
            For(property.GetMemberName());
            return this;
        }

        /// <summary>
        /// Initializes the configurator to configure the given property or field.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public ColumnMapBuilder<T> For(string propertyName)
        {
            _currentPropertyName = propertyName;

            // Try to add the column map if it doesn't exist
            if (Columns.GetByFieldName(_currentPropertyName) == null)
            {
                TryAddColumnMapForField(_currentPropertyName);
            }

            return this;
        }

        public ColumnMapBuilder<T> SetPrimaryKey()
        {
            AssertCurrentPropertyIsSet();
            return SetPrimaryKey(_currentPropertyName);
        }

        public ColumnMapBuilder<T> SetPrimaryKey(string propertyName)
        {
            Columns.GetByFieldName(propertyName).ColumnInfo.IsPrimaryKey = true;
            return this;
        }

        public ColumnMapBuilder<T> SetAutoIncrement()
        {
            AssertCurrentPropertyIsSet();
            return SetAutoIncrement(_currentPropertyName);
        }

        public ColumnMapBuilder<T> SetAutoIncrement(string propertyName)
        {
            Columns.GetByFieldName(propertyName).ColumnInfo.IsAutoIncrement = true;
            return this;
        }

        public ColumnMapBuilder<T> SetColumnName(string columnName)
        {
            AssertCurrentPropertyIsSet();
            return SetColumnName(_currentPropertyName, columnName);
        }

        public ColumnMapBuilder<T> SetColumnName(string propertyName, string columnName)
        {
            Columns.GetByFieldName(propertyName).ColumnInfo.Name = columnName;
            return this;
        }

        public ColumnMapBuilder<T> SetReturnValue()
        {
            AssertCurrentPropertyIsSet();
            return SetReturnValue(_currentPropertyName);
        }

        public ColumnMapBuilder<T> SetReturnValue(string propertyName)
        {
            Columns.GetByFieldName(propertyName).ColumnInfo.ReturnValue = true;
            return this;
        }

        public ColumnMapBuilder<T> SetSize(int size)
        {
            AssertCurrentPropertyIsSet();
            return SetSize(_currentPropertyName, size);
        }

        public ColumnMapBuilder<T> SetSize(string propertyName, int size)
        {
            Columns.GetByFieldName(propertyName).ColumnInfo.Size = size;
            return this;
        }

        public ColumnMapBuilder<T> SetAltName(string altName)
        {
            AssertCurrentPropertyIsSet();
            return SetAltName(_currentPropertyName, altName);
        }

        public ColumnMapBuilder<T> SetAltName(string propertyName, string altName)
        {
            Columns.GetByFieldName(propertyName).ColumnInfo.AltName = altName;
            return this;
        }

        public ColumnMapBuilder<T> SetParamDirection(ParameterDirection direction)
        {
            AssertCurrentPropertyIsSet();
            return SetParamDirection(_currentPropertyName, direction);
        }

        public ColumnMapBuilder<T> SetParamDirection(string propertyName, ParameterDirection direction)
        {
            Columns.GetByFieldName(propertyName).ColumnInfo.ParamDirection = direction;
            return this;
        }

        public ColumnMapBuilder<T> RemoveColumnMap(Expression<Func<T, object>> property)
        {
            string propertyName = property.GetMemberName();
            return RemoveColumnMap(propertyName);
        }

        public ColumnMapBuilder<T> RemoveColumnMap(string propertyName)
        {
            var columnMap = Columns.GetByFieldName(propertyName);
            Columns.Remove(columnMap);
            return this;
        }

        /// <summary>
        /// Tries to add a ColumnMap for the given field name.  
        /// Throws and exception if field cannot be found.
        /// </summary>
        private void TryAddColumnMapForField(string fieldName)
        {
            // Set strategy to filter for public or private fields
            ConventionMapStrategy strategy = new ConventionMapStrategy(false);

            // Find the field that matches the given field name
            strategy.ColumnPredicate = mi => mi.Name == fieldName;
            ColumnMap columnMap = strategy.MapColumns(typeof(T)).FirstOrDefault();

            if (columnMap == null)
            {
                throw new DataMappingException(string.Format("Could not find the field '{0}' in '{1}'.",
                    fieldName,
                    typeof(T).Name));
            }
            else
            {
                Columns.Add(columnMap);
            }
        }

        /// <summary>
        /// Throws an exception if the "current" property has not been set.
        /// </summary>
        private void AssertCurrentPropertyIsSet()
        {
            if (string.IsNullOrEmpty(_currentPropertyName))
            {
                throw new DataMappingException("A property must first be specified using the 'For' method.");
            }
        }

        #endregion
    }
}
