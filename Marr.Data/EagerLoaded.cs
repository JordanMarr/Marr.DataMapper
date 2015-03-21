using System;
using System.Collections.Generic;
using System.Linq;
using Marr.Data.Mapping;
using Marr.Data.QGen;

namespace Marr.Data
{
	public interface IEagerLoaded
	{
		object Load(IDataMapper db, object parent);
	}

	/// <summary>
	/// This is the eager loaded definition specified via the fluent mappings.
	/// </summary>
	/// <typeparam name="TParent">The parent entity that contains the eager loaded entity.</typeparam>
	/// <typeparam name="TChild">The child entity that is being eager loaded.</typeparam>
	public class EagerLoaded<TParent, TChild> : IEagerLoaded
	{
		/// <summary>
		/// A linq query that is specified in the fluent mappings that returns a TChild.
		/// </summary>
		public Func<IDataMapper, TParent, TChild> Query { get; set; }

		/// <summary>
		/// An optional condition that, if set and evaluates to false, 
		/// will return the TChild default instead of running the query.
		/// </summary>
		public Func<TParent, bool> Condition { get; set; }

		public RelationshipTypes RelationshipType { get; set; }

		public object Load(IDataMapper db, object parent)
		{
			TParent tParent = (TParent)parent;

			if (Condition != null && !Condition(tParent))
			{
				return default(TChild);
			}
			else
			{
				var result = Query(db, tParent);

				IQueryToList query = result as IQueryToList;
				if (query != null)
				{
					// User did not call ToList or FirstOrDefault
					var enumerable = result as System.Collections.IEnumerable;
					if (RelationshipType == RelationshipTypes.Many)
					{
						return query.ToListObject();
					}
					else
					{
						var enumeratorOne = enumerable.GetEnumerator();
						return enumeratorOne.MoveNext() ? enumeratorOne.Current : null;
					}
				}
				else
				{
					// User already called ToList or FirstOrDefault
					return result;
				}
			}
		}
	}

}