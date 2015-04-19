using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data
{
	/// <summary>
	/// Eager loads a child property using a given foreign key mapping.
	/// </summary>
	/// <typeparam name="TParent"></typeparam>
	/// <typeparam name="TChild"></typeparam>
	public class EagerLoadedOn<TParent, TChild> : IEagerLoaded
	{
		private string _fk;
		private Type _tParentType; // Will differ from TParent if using ForEachEntity mapping
		private RelationshipTypes _relationshipType;
		private ColumnMapCollection _parentColumns;
		private ColumnMapCollection _childColumns;

		public EagerLoadedOn(string fk, Type tParentType, RelationshipTypes relationshipType, Func<TParent, bool> condition)
		{
			var repos = MapRepository.Instance;

			_fk = fk;
			_tParentType = tParentType;
			_relationshipType = relationshipType;
			_parentColumns = repos.Columns[_tParentType];
			_childColumns = repos.Columns[typeof(TChild)];

			Condition = condition;
		}

		/// <summary>
		/// An optional condition that, if set and evaluates to false, 
		/// will return the TChild default instead of running the query.
		/// </summary>
		public Func<TParent, bool> Condition { get; private set; }

		public object Load(IDataMapper db, object parent)
		{
			TParent tParent = (TParent)parent;

			if (Condition != null && !Condition(tParent))
			{
				return null;
			}
			else
			{
				if (_relationshipType == RelationshipTypes.One)
				{
					// Parent FK = Child PK
					if (_childColumns.PrimaryKeys.Count != 1)
						throw new DataMappingException(string.Format("'{0}' must have exactly one primary key mapped.", typeof(TChild).Name));

					var childPK = _childColumns.PrimaryKeys[0];

					var parentFK = _parentColumns.GetByFieldName(_fk);
					if (parentFK == null)
						throw new DataMappingException(string.Format("'{0}' does not contain foreign key field '{1}'.", typeof(TParent), _fk));

					db.AddParameter("@FK", parentFK.Getter(parent));
					var query = db.Query<TChild>();
					string whereClause = query.BuildColumnName(childPK.ColumnInfo.Name) + "=@FK";
					return query.Where(whereClause).FirstOrDefault();
				}
				else if (_relationshipType == RelationshipTypes.Many)
				{
					// Parent PK = Child FK
					if (_parentColumns.PrimaryKeys.Count != 1)
						throw new DataMappingException(string.Format("'{0}' must have exactly one primary key mapped.", typeof(TParent).Name));

					var parentPK = _parentColumns.PrimaryKeys[0];

					var childFK = _childColumns.GetByFieldName(_fk);
					if (childFK == null)
						throw new DataMappingException(string.Format("'{0}' does not contain foreign key field '{1}'.", typeof(TChild), _fk));

					db.AddParameter("@PK", parentPK.Getter(parent));
					var query = db.Query<TChild>();
					string whereClause = query.BuildColumnName(childFK.ColumnInfo.Name) + "=@PK";
					return query.Where(whereClause).ToList();
				}
				else
				{
					// This should not happen because relationship type should be inferred already
					throw new DataMappingException(string.Format("Unable to infer relationship type for parent '{0}' -> '{1}'.", 
						typeof(TParent).Name, typeof(TChild).Name));
				}
			}
		}
	}
}
