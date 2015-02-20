using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Marr.Data.QGen;

namespace Marr.Data
{
	/// <summary>
	/// An internal subclass of the DataMapper that fulfills EagerLoaded and LazyLoaded queries.
	/// </summary>
	internal class RelationshipDataMapper : DataMapper
	{
		private QueryBuilder _rootQuery;
		private int _graphIndex;

		/// <summary>
		/// A database provider agnostic initialization.
		/// </summary>
		/// <param name="connectionString">The db connection string.</param>
		/// <param name="dbProviderFactory">The db provider factory.</param>
		/// <param name="rootQuery">
		/// A parent query that provides the contextual information from the parent query.
		/// This includes which levels of the object graph should be returned in the result set.
		/// </param>
		/// <param name="graphIndex">The current graph index.</param>
		internal RelationshipDataMapper(DbProviderFactory dbProviderFactory, string connectionString, QueryBuilder rootQuery, int graphIndex)
			: base(dbProviderFactory, connectionString)
		{
			if (rootQuery == null)
				throw new ArgumentNullException("parentQuery");

			_rootQuery = rootQuery;
			_graphIndex = graphIndex;
		}

		public override QueryBuilder<T> Query<T>()
		{
			var dialect = QGen.QueryFactory.CreateDialect(this);
			var nextGraphIndex = _graphIndex + 1;
			return new QueryBuilder<T>(this, dialect, _rootQuery, nextGraphIndex);
		}
	}
}
