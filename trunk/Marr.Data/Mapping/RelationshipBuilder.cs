using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Marr.Data.Mapping
{
    /// <summary>
    /// This class has fluent methods that are used to easily configure relationship mappings.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RelationshipBuilder<T>
    {
        private string _currentPropertyName;

        public RelationshipBuilder(RelationshipCollection relationships)
        {
            Relationships = relationships;
        }

        /// <summary>
        /// Gets the list of relationship mappings that are being configured.
        /// </summary>
        public RelationshipCollection Relationships { get; private set; }

        #region - Fluent Methods -

        /// <summary>
        /// Initializes the configurator to configure the given property.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public RelationshipBuilder<T> For(Expression<Func<T, object>> property)
        {
            _currentPropertyName = property.GetMemberName();
            return this;
        }

        /// <summary>
        /// Initializes the configurator to configure the given property or field.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public RelationshipBuilder<T> For(string propertyName)
        {
            _currentPropertyName = propertyName;
            return this;
        }

        public RelationshipBuilder<T> SetOneToOne()
        {
            AssertCurrentPropertyIsSet();
            SetOneToOne(_currentPropertyName);
            return this;
        }

        public RelationshipBuilder<T> SetOneToOne(string propertyName)
        {
            Relationships[propertyName].RelationshipInfo.RelationType = RelationshipTypes.One;
            return this;
        }

        public RelationshipBuilder<T> RemoveRelationship(Expression<Func<T, object>> property)
        {
            string propertyName = property.GetMemberName();
            Relationships.RemoveAll(r => r.Member.Name == propertyName);
            return this;
        }

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
