using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Marr.Data.Mapping.Strategies;

namespace Marr.Data.Mapping
{
    /// <summary>
    /// This class has fluent methods that are used to easily configure relationship mappings.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class RelationshipBuilder<TEntity>
    {
        private FluentMappings.MappingsFluentEntity<TEntity> _fluentEntity;
        private string _currentPropertyName;

        public RelationshipBuilder(FluentMappings.MappingsFluentEntity<TEntity> fluentEntity, RelationshipCollection relationships)
        {
            _fluentEntity = fluentEntity;
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
        public RelationshipBuilder<TEntity> For(Expression<Func<TEntity, object>> property)
        {
            return For(property.GetMemberName());
        }

        /// <summary>
        /// Initializes the configurator to configure the given property or field.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public RelationshipBuilder<TEntity> For(string propertyName)
        {
            _currentPropertyName = propertyName;

            // Try to add the relationship if it doesn't exist
            if (Relationships[_currentPropertyName] == null)
            {
                TryAddRelationshipForField(_currentPropertyName);
            }

            return this;
        }


        /// <summary>
		/// Sets the current property to be lazy loaded with the given query.
        /// </summary>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="query"></param>
        /// <param name="condition">condition in which a child could exist. eg. avoid call to db if foreign key is 0 or null</param>
        /// <returns></returns>
		public RelationshipBuilder<TEntity> LazyLoad(Func<IDataMapper, TEntity, object> query, Func<TEntity, bool> condition = null)
        {
            AssertCurrentPropertyIsSet();

			var relationship = Relationships[_currentPropertyName];

			Type childType = null;
			bool isLazyLoadProxyMember = typeof(ILazyLoaded).IsAssignableFrom(relationship.MemberType);
			if (isLazyLoadProxyMember)
			{
				// Field is a LazyLoaded proxy class
				childType = relationship.MemberType.GetGenericArguments()[0];
			}
			else
			{
				// Field is a dyanmic object type - find property that points to this backing field
				var member = DataHelper.FindPropertyForBackingField(typeof(TEntity), relationship.Member);
				if (member == null)
					throw new DataMappingException("Unable to infer the data type for this lazy loaded member. Try manually calling 'ToList()' or 'FirstOrDefault()'.");
				childType = member.ReturnType;
			}

			// Make generic LazyLoaded type with matching child property
			Type lazyLoadedType = typeof(LazyLoaded<,>);
			var lazyLoadedInstanceType = lazyLoadedType.MakeGenericType(typeof(TEntity), childType);
			var lazyLoaded = Activator.CreateInstance(lazyLoadedInstanceType, 
				query, 
				relationship.RelationshipInfo.RelationType, 
				condition);

			relationship.LazyLoaded = (ILazyLoaded)lazyLoaded;
            return this;
        }

		/// <summary>
		/// Sets the current property to be eager loaded by the given query.
		/// </summary>
		/// <typeparam name="TChild"></typeparam>
		/// <param name="query"></param>
		/// <param name="condition"></param>
		/// <returns></returns>
		public RelationshipBuilder<TEntity> EagerLoad<TChild>(Func<IDataMapper, TEntity, TChild> query, Func<TEntity, bool> condition = null)
		{
			AssertCurrentPropertyIsSet();

			var relationship = Relationships[_currentPropertyName];
			relationship.EagerLoaded = new EagerLoaded<TEntity, TChild>
				{
					Query = query,
					RelationshipType = relationship.RelationshipInfo.RelationType,
					Condition = condition
				};
			return this;
		}

		/// <summary>
		/// Sets the current one-to-one relationship property to be eager loaded using the given join relationship.
		/// </summary>
		/// <typeparam name="TRight">The type of entity that will be the right join.</typeparam>
		/// <param name="rightEntityOne">
		/// A lambda expression that specifies which child property to join.
		/// Ex: order => order.OrderItems
		/// </param>
		/// <param name="joinOn">
		/// A lambda expression that specifies the join condition.
		/// Ex: (order, orderItem) => order.ID == orderItem.OrderID
		/// </param>
		/// <param name="joinType">
		/// The type of SQL join: Inner, Left or Right.
		/// Default: Left
		/// </param>
		/// <returns></returns>
		public RelationshipBuilder<TEntity> JoinOne<TRight>(
			Expression<Func<TEntity, TRight>> rightEntityOne, 
			Expression<Func<TEntity, TRight, bool>> joinOn, 
			QGen.JoinType joinType = QGen.JoinType.Left)
		{
			AssertCurrentPropertyIsSet();
			Relationships[_currentPropertyName].EagerLoadedJoin = new EagerLoadedJoin<TEntity, TRight>
			{
				JoinType = joinType,
				RightEntityOne = rightEntityOne,
				JoinOn = joinOn
			};
			return this;
		}

		/// <summary>
		/// Sets the current one-to-many relationship property to be eager loaded using the given join relationship.
		/// </summary>
		/// <typeparam name="TRight"></typeparam>
		/// <param name="rightEntityMany"></param>
		/// <param name="joinOn"></param>
		/// <param name="joinType"></param>
		/// <returns></returns>
		public RelationshipBuilder<TEntity> JoinMany<TRight>(
			Expression<Func<TEntity, IEnumerable<TRight>>> rightEntityMany, 
			Expression<Func<TEntity, TRight, bool>> joinOn, 
			QGen.JoinType joinType = QGen.JoinType.Left)
		{
			AssertCurrentPropertyIsSet();
			Relationships[_currentPropertyName].EagerLoadedJoin = new EagerLoadedJoin<TEntity, TRight>
			{
				JoinType = joinType,
				RightEntityMany = rightEntityMany,
				JoinOn = joinOn
			};
			return this;
		}

		/// <summary>
		/// Marks the current relationship property as a one-to-one relationship.
		/// </summary>
		/// <returns></returns>
        public RelationshipBuilder<TEntity> SetOneToOne()
        {
            AssertCurrentPropertyIsSet();
            SetOneToOne(_currentPropertyName);
            return this;
        }

		/// <summary>
		/// Marks the current relationship property as a one-to-one relationship.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <returns></returns>
        public RelationshipBuilder<TEntity> SetOneToOne(string propertyName)
        {
            Relationships[propertyName].RelationshipInfo.RelationType = RelationshipTypes.One;
            return this;
        }

		/// <summary>
		/// Marks the current relationship property as a one-to-many relationship.
		/// </summary>
		/// <returns></returns>
        public RelationshipBuilder<TEntity> SetOneToMany()
        {
            AssertCurrentPropertyIsSet();
            SetOneToMany(_currentPropertyName);
            return this;
        }

		/// <summary>
		/// Marks the current relationship property as a one-to-many relationship.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <returns></returns>
        public RelationshipBuilder<TEntity> SetOneToMany(string propertyName)
        {
            Relationships[propertyName].RelationshipInfo.RelationType = RelationshipTypes.Many;
            return this;
        }

		/// <summary>
		/// Ignores the current relationship property. 
		/// (This is used in conjunction with one of the auto-mapping fluent methods).
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
        public RelationshipBuilder<TEntity> Ignore(Expression<Func<TEntity, object>> property)
        {
            string propertyName = property.GetMemberName();
            Relationships.RemoveAll(r => r.Member.Name == propertyName);
            return this;
        }

        public FluentMappings.MappingsFluentTables<TEntity> Tables
        {
            get
            {
                if (_fluentEntity == null)
                {
                    throw new Exception("This property is not compatible with the obsolete 'MapBuilder' class.");
                }

                return _fluentEntity.Table;
            }
        }

        public FluentMappings.MappingsFluentColumns<TEntity> Columns
        {
            get
            {
                if (_fluentEntity == null)
                {
                    throw new Exception("This property is not compatible with the obsolete 'MapBuilder' class.");
                }

                return _fluentEntity.Columns;
            }
        }

        public FluentMappings.MappingsFluentEntity<TNewEntity> Entity<TNewEntity>()
        {
            return new FluentMappings.MappingsFluentEntity<TNewEntity>(true);
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
            Relationship relationship = strategy.MapRelationships(typeof(TEntity)).FirstOrDefault();

            if (relationship == null)
            {
                throw new DataMappingException(string.Format("Could not find the field '{0}' in '{1}'.",
                    fieldName,
                    typeof(TEntity).Name));
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
