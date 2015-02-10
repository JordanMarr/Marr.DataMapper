using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Marr.Data.Mapping.Strategies;
using System.Collections;
using System.Linq.Expressions;

namespace Marr.Data.Mapping
{
	/// <summary>
	/// Provides a fluent interface for mapping domain entities and properties to database tables and columns.
	/// </summary>
	public class FluentMappings
	{
		private bool _publicOnly;

		public FluentMappings()
			: this(true)
		{ }

		public FluentMappings(bool publicOnly)
		{
			_publicOnly = publicOnly;
			
		}

		/// <summary>
		/// Performs the passed in mappings on all subclasses of the given TEntityBase.
		/// </summary>
		/// <typeparam name="TEntityBase">
		/// An entity base class.
		/// </typeparam>
		/// <param name="baseEntityMappingAction">
		/// Fluent mappings that will be performed on all subclasses of the given TEntityBase.
		/// </param>
		/// <param name="assembliesToSearch">
		/// OPTIONAL.  The assemblies that will be searched for subclasses of TEntityBase.
		/// If none are passed in, only the Assembly that contains TEntityBase will be searched.
		/// </param>
		/// <returns></returns>
		public FluentMappings ForEachEntity<TEntityBase>(
			Action<MappingsFluentEntity<TEntityBase>> baseEntityMappingAction, 
			params Assembly[] assembliesToSearch)
		{
			var assemblies = assembliesToSearch.Any() ? assembliesToSearch : new[] { typeof(TEntityBase).Assembly };
			var entityType = typeof(TEntityBase);
			
			var subclassTypesOfTEntity = assemblies
				.SelectMany(a => a.GetTypes().Where(t => entityType.IsAssignableFrom(t) && t.IsClass))
				.ToArray();

			// Apply mapping action to each TEntity subclass
			foreach (var subclassType in subclassTypesOfTEntity)
			{
				var subclassMappingsRoot = new MappingsFluentEntity<TEntityBase>(_publicOnly, subclassType);
				baseEntityMappingAction(subclassMappingsRoot);
			}

			return this;
		}

		public MappingsFluentEntity<TEntity> Entity<TEntity>()
		{
			return new MappingsFluentEntity<TEntity>(_publicOnly);
		}

		public class MappingsFluentEntity<TEntity>
		{
			public MappingsFluentEntity(bool publicOnly)
				: this (publicOnly, typeof(TEntity))
			{ }

			public MappingsFluentEntity(bool publicOnly, Type entityType)
			{
				Columns = new MappingsFluentColumns<TEntity>(this, publicOnly, entityType);
				Table = new MappingsFluentTables<TEntity>(this, entityType);
				Relationships = new MappingsFluentRelationships<TEntity>(this, publicOnly, entityType);
			}

			/// <summary>
			/// Contains methods that map entity properties to database table and view column names;
			/// </summary>
			public MappingsFluentColumns<TEntity> Columns { get; private set; }

			/// <summary>
			/// Contains methods that map entity classes to database table names.
			/// </summary>
			public MappingsFluentTables<TEntity> Table { get; private set; }

			/// <summary>
			/// Contains methods that map sub-entities with database table and view column names.
			/// </summary>
			public MappingsFluentRelationships<TEntity> Relationships { get; private set; }
		}

		public class MappingsFluentColumns<TEntity>
		{
			private bool _publicOnly;
			private FluentMappings.MappingsFluentEntity<TEntity> _fluentEntity;
			private Type _entityType;

			public MappingsFluentColumns(FluentMappings.MappingsFluentEntity<TEntity> fluentEntity, bool publicOnly, Type entityType)
			{
				_fluentEntity = fluentEntity;
				_publicOnly = publicOnly;
				_entityType = entityType;
			}

			/// <summary>
			/// Creates column mappings for the given type.
			/// Maps all properties except ICollection properties.
			/// </summary>
			/// <typeparam name="T">The type that is being built.</typeparam>
			/// <returns><see cref="ColumnMapCollection"/></returns>
			public ColumnMapBuilder<TEntity> AutoMapAllProperties()
			{
				return AutoMapPropertiesWhere(m => m.MemberType == MemberTypes.Property &&
					(m as PropertyInfo).CanWrite &&
					!typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType));
			}

			/// <summary>
			/// Creates column mappings for the given type.
			/// Maps all properties that are simple types (int, string, DateTime, etc).  
			/// ICollection properties are not included.
			/// </summary>
			/// <typeparam name="T">The type that is being built.</typeparam>
			/// <returns><see cref="ColumnMapCollection"/></returns>
			public ColumnMapBuilder<TEntity> AutoMapSimpleTypeProperties()
			{
				return AutoMapPropertiesWhere(m => m.MemberType == MemberTypes.Property &&
					(m as PropertyInfo).CanWrite &&
					DataHelper.IsSimpleType((m as PropertyInfo).PropertyType) &&
					!typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType));
			}

			/// <summary>
			/// Creates column mappings for the given type if they match the predicate.
			/// </summary>
			/// <typeparam name="T">The type that is being built.</typeparam>
			/// <param name="predicate">Determines whether a mapping should be created based on the member info.</param>
			/// <returns><see cref="ColumnMapConfigurator"/></returns>
			public ColumnMapBuilder<TEntity> AutoMapPropertiesWhere(Func<MemberInfo, bool> predicate)
			{
				ConventionMapStrategy strategy = new ConventionMapStrategy(_publicOnly);
				strategy.ColumnPredicate = predicate;
				ColumnMapCollection columns = strategy.MapColumns(_entityType);
				MapRepository.Instance.Columns[_entityType] = columns;
				return new ColumnMapBuilder<TEntity>(_fluentEntity, columns);
			}

			/// <summary>
			/// Creates a ColumnMapBuilder that starts out with no pre-populated columns.
			/// All columns must be added manually using the builder.
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <returns></returns>
			public ColumnMapBuilder<TEntity> MapProperties()
			{
				ColumnMapCollection columns = new ColumnMapCollection();
				MapRepository.Instance.Columns[_entityType] = columns;
				return new ColumnMapBuilder<TEntity>(_fluentEntity, columns);
			}
		}

		public class MappingsFluentTables<TEntity>
		{
			private FluentMappings.MappingsFluentEntity<TEntity> _fluentEntity;
			private Type _entityType;

			public MappingsFluentTables(FluentMappings.MappingsFluentEntity<TEntity> fluentEntity, Type entityType)
			{
				_fluentEntity = fluentEntity;
				_entityType = entityType;
			}

			/// <summary>
			/// Sets the table name to the class name of the entity that is being mapped.
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <returns></returns>
			public TableBuilder<TEntity> AutoMapTable()
			{
				return new TableBuilder<TEntity>(_fluentEntity, _entityType);
			}

			/// <summary>
			/// Sets the table name for a given type.
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="tableName"></param>
			public TableBuilder<TEntity> MapTable(string tableName)
			{
				return new TableBuilder<TEntity>(_fluentEntity, _entityType).SetTableName(tableName);
			}

			/// <summary>
			/// Sets the table name for a given type based on the entity class type.
			/// </summary>
			/// <param name="createTableNameFromEntityType">
			/// A function that returns a table name based on the entity type that is being mapped.
			/// </param>
			/// <returns></returns>
			public TableBuilder<TEntity> MapTable(Func<Type, string> createTableNameFromEntityType)
			{
				string tableName = createTableNameFromEntityType(_entityType);
				return new TableBuilder<TEntity>(_fluentEntity, _entityType).SetTableName(tableName);
			}
		}

		public class MappingsFluentRelationships<TEntity>
		{
			private FluentMappings.MappingsFluentEntity<TEntity> _fluentEntity;
			private bool _publicOnly;
			private Type _entityType;

			public MappingsFluentRelationships(FluentMappings.MappingsFluentEntity<TEntity> fluentEntity, bool publicOnly, Type entityType)
			{
				_fluentEntity = fluentEntity;
				_publicOnly = publicOnly;
				_entityType = entityType;
			}

			/// <summary>
			/// Creates relationship mappings for the given type.
			/// Maps all properties that implement ICollection or are not "simple types".
			/// </summary>
			/// <returns></returns>
			public RelationshipBuilder<TEntity> AutoMapICollectionOrComplexProperties()
			{
				return AutoMapPropertiesWhere(m =>
					m.MemberType == MemberTypes.Property &&
					(m as PropertyInfo).CanWrite &&
					(
						typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType) || !DataHelper.IsSimpleType((m as PropertyInfo).PropertyType)
					)
				);

			}

			/// <summary>
			/// Creates relationship mappings for the given type.
			/// Maps all properties that implement ICollection.
			/// </summary>
			/// <returns><see cref="RelationshipBuilder"/></returns>
			public RelationshipBuilder<TEntity> AutoMapICollectionProperties()
			{
				return AutoMapPropertiesWhere(m =>
					m.MemberType == MemberTypes.Property &&
					(m as PropertyInfo).CanWrite &&
					typeof(ICollection).IsAssignableFrom((m as PropertyInfo).PropertyType));
			}

			/// <summary>
			/// Creates relationship mappings for the given type.
			/// Maps all properties that are not "simple types".
			/// </summary>
			/// <returns></returns>
			public RelationshipBuilder<TEntity> AutoMapComplexTypeProperties()
			{
				return AutoMapPropertiesWhere(m =>
					m.MemberType == MemberTypes.Property &&
					(m as PropertyInfo).CanWrite &&
					!DataHelper.IsSimpleType((m as PropertyInfo).PropertyType));
			}
			
			/// <summary>
			/// Creates relationship mappings for the given type if they match the predicate.
			/// </summary>
			/// <param name="predicate">Determines whether a mapping should be created based on the member info.</param>
			/// <returns><see cref="RelationshipBuilder"/></returns>
			public RelationshipBuilder<TEntity> AutoMapPropertiesWhere(Func<MemberInfo, bool> predicate)
			{
				ConventionMapStrategy strategy = new ConventionMapStrategy(_publicOnly);
				strategy.RelationshipPredicate = predicate;
				RelationshipCollection relationships = strategy.MapRelationships(_entityType);
				MapRepository.Instance.Relationships[_entityType] = relationships;
				return new RelationshipBuilder<TEntity>(_fluentEntity, relationships);
			}

			/// <summary>
			/// Creates a RelationshipBuilder that starts out with no pre-populated relationships.
			/// All relationships must be added manually using the builder.
			/// </summary>
			/// <returns></returns>
			public RelationshipBuilder<TEntity> MapProperties()
			{
				RelationshipCollection relationships = new RelationshipCollection();
				MapRepository.Instance.Relationships[_entityType] = relationships;
				return new RelationshipBuilder<TEntity>(_fluentEntity, relationships);
			}
		}
	}
}
