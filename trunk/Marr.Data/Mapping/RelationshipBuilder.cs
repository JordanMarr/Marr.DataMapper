using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Marr.Data.Mapping.Strategies;

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
            return For(property.GetMemberName());
        }

        /// <summary>
        /// Initializes the configurator to configure the given property or field.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public RelationshipBuilder<T> For(string propertyName)
        {
            _currentPropertyName = propertyName;

            // Try to add the relationship if it doesn't exist
            if (Relationships[_currentPropertyName] == null)
            {
                TryAddRelationshipForField(_currentPropertyName);
            }

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

        public RelationshipBuilder<T> SetOneToMany()
        {
            AssertCurrentPropertyIsSet();
            SetOneToMany(_currentPropertyName);
            return this;
        }

        public RelationshipBuilder<T> SetOneToMany(string propertyName)
        {
            Relationships[propertyName].RelationshipInfo.RelationType = RelationshipTypes.Many;
            return this;
        }

        public RelationshipBuilder<T> RemoveRelationship(Expression<Func<T, object>> property)
        {
            string propertyName = property.GetMemberName();
            Relationships.RemoveAll(r => r.Member.Name == propertyName);
            return this;
        }

        /// <summary>
        /// Tries to add a Relationship for the given field name.  
        /// Throws and exception if field cannot be found.
        /// </summary>
        private void TryAddRelationshipForField(string fieldName)
        {
            // Set strategy to filter for public or private fields
            ConventionMapStrategy strategy = new ConventionMapStrategy(false);

            // Find the field that matches the given field name
            strategy.RelationshipPredicate = mi => mi.Name == fieldName;
            Relationship relationship = strategy.MapRelationships(typeof(T)).FirstOrDefault();

            if (relationship == null)
            {
                throw new DataMappingException(string.Format("Could not find the field '{0}' in '{1}'.",
                    fieldName,
                    typeof(T).Name));
            }
            else
            {
                Relationships.Add(relationship);
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
