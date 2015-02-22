using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.QGen;
using System.Linq.Expressions;
using System.Reflection;
using Marr.Data.Mapping;
using System.Data.Common;
using System.Collections;

namespace Marr.Data.QGen
{
	/// <summary>
	/// Contains internal contextual properties about the query in a non-generic base class.
	/// </summary>
	public abstract class QueryBuilder : ExpressionVisitor
	{
		public QueryBuilder(QueryBuilder rootQuery, int graphIndex)
		{
			RootQuery = rootQuery;
			GraphIndex = graphIndex;
			RelationshipsToLoad = new List<RelationshipLoadRequest>();
		}

		internal QueryBuilder RootQuery { get; set; }
		internal List<RelationshipLoadRequest> RelationshipsToLoad { get; set; }
		internal bool IsGraph { get; set; }
		internal int SkipCount { get; set; }
		internal int TakeCount { get; set; }
		internal bool IsJoin { get; set; }
		internal bool IsManualQuery { get; set; }
		internal bool EnablePaging { get; set; }
		internal bool IsFromView { get; set; }
		internal bool IsFromTable { get; set; }
		internal bool HasDirectChildrenToMergeInQuery { get; set; }
		internal TableCollection Tables;
		internal string CommandText { get; set; }
		internal int GraphIndex { get; set; }

		private EntityGraph _entityGraph;
		internal EntityGraph EntGraph
		{
			get
			{
				if (_entityGraph == null)
				{
					if (GraphIndex == 0)
					{
						_entityGraph = LoadEntityGraphForT();
						_entityGraph.RootList = GetResultsListForT();
					}
					else
					{
						// The GraphIndex finds the correct level of children in the graph
						var node = RootQuery.EntGraph.ElementAt(GraphIndex);

						// Find the node that matches the type being loaded for this level
						var entityType = this.GetEntityType();
						_entityGraph =  node.Parent.Children.Where(c => c.EntityType == entityType).First();
						_entityGraph.RootList = GetResultsListForT();
					}
				}

				return _entityGraph;
			}
		}

		internal bool UseAltNames()
		{
			return IsFromView || IsJoin || HasDirectChildrenToMergeInQuery;
		}
		
		internal abstract Type GetEntityType();

		internal abstract EntityGraph LoadEntityGraphForT();
		internal abstract IList GetResultsListForT();
	}

	/// <summary>
	/// This class is responsible for building a query.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class QueryBuilder<T> : QueryBuilder, IEnumerable<T>, IQueryBuilder
	{
		#region - Private Members -

		private DataMapper _db;
		private Dialects.Dialect _dialect;		
		private WhereBuilder<T> _whereBuilder;
		private bool _singleOrFirstNullable;
		private SortBuilder<T> _sortBuilder;		
		private SortBuilder<T> SortBuilder
		{
			get
			{
				// Lazy load
				if (_sortBuilder == null)
				{
					bool useAltNames = UseAltNames();
					_sortBuilder = new SortBuilder<T>(this, _db, _whereBuilder, _dialect, Tables, useAltNames);
				}

				return _sortBuilder;
			}
		}
		private List<T> _results = new List<T>();

		#endregion

		#region - Constructor -
		
		public QueryBuilder(DataMapper db, Dialects.Dialect dialect, QueryBuilder rootQuery = null, int graphIndex = 0)
			: base(rootQuery, graphIndex)
		{
			_db = db;
			_dialect = dialect;
			Tables = new TableCollection();
			Tables.Add(new Table(typeof(T)));

			if (rootQuery != null)
			{
				PrepareGraphQuery(rootQuery.RelationshipsToLoad);
			}
		}

		#endregion

		#region - Fluent Methods -

		/// <summary>
		/// Overrides the base table name that will be used in the query.
		/// </summary>
		[Obsolete("This method is obsolete.  Use either the FromTable or FromView method instead.", true)]
		public virtual QueryBuilder<T> From(string tableName)
		{
			return FromView(tableName);
		}

		/// <summary>
		/// Overrides the base view name that will be used in the query.
		/// Will try to use the mapped "AltName" values when loading the columns.
		/// </summary>
		public virtual QueryBuilder<T> FromView(string viewName)
		{
			if (string.IsNullOrEmpty(viewName))
				throw new ArgumentNullException("view");

			IsFromView = true;
			HasDirectChildrenToMergeInQuery = true;

			// Replace the base table with a view with tables
			if (Tables[0] is View)
			{
				(Tables[0] as View).Name = viewName;
			}
			else
			{
				View view = new View(viewName, Tables.ToArray());
				Tables.ReplaceBaseTable(view);
			}

			return this;
		}

		/// <summary>
		/// Overrides the base table name that will be used in the query.
		/// Will not try to use the mapped "AltName" values when loading the  columns.
		/// </summary>
		public virtual QueryBuilder<T> FromTable(string table)
		{
			if (string.IsNullOrEmpty(table))
				throw new ArgumentNullException("view");

			IsFromTable = true;

			// Override the base table name
			Tables[0].Name = table;
			return this;
		}

		/// <summary>
		/// Allows you to manually specify the query text.
		/// </summary>
		public virtual QueryBuilder<T> QueryText(string queryText)
		{
			IsManualQuery = true;
			CommandText = queryText;
			return this;
		}

		/// <summary>
		/// If no parameters are passed in, this method instructs the DataMapper to load all related entities in the graph.
		/// If specific entities are passed in, only these relationships will be loaded.
		/// </summary>
		/// <param name="childrenToLoad">A list of related child entites to load (passed in as properties / lambda expressions).</param>
		public virtual QueryBuilder<T> Graph(params Expression<Func<T, object>>[] childrenToLoad)
		{
			IsGraph = true;

			var membersNotFoundInGraph = new List<string>();

			// Populate RelationshipsToLoad
			foreach (var ctl in childrenToLoad)
			{
				var rtl = new RelationshipLoadRequest(ctl);
				
				// Set the corresponding ent graph node
				var graphNode = EntGraph
					.Where(n => n.BuildEntityTypePath() == rtl.BuildEntityTypePath())
					.FirstOrDefault();

				rtl.EntGraphNode = graphNode;

				if (graphNode != null)
					RelationshipsToLoad.Add(rtl);
				else
					membersNotFoundInGraph.Add(string.Format("- {0}", ctl.ToString()));
			}

			if (membersNotFoundInGraph.Any())
				throw new RelationshipLoadException(
					string.Format("The following requested members are not mapped as relationships on the '{0}' entity:\n{1}",
						typeof(T).Name,
						string.Join(",\n", membersNotFoundInGraph.ToArray())));

			if (!childrenToLoad.Any())
			{
				foreach (var node in EntGraph.Where(g => g.Member != null))
				{
					RelationshipsToLoad.Add(new RelationshipLoadRequest(node));
				}
			}

			PrepareGraphQuery(RelationshipsToLoad);

			return this;
		}

		internal void PrepareGraphQuery(IEnumerable<RelationshipLoadRequest> relationshipsToLoad)
		{
			HasDirectChildrenToMergeInQuery = EntGraph.HasDirectChildrenToMergeInQuery(relationshipsToLoad);

			// Populate _tables that need to added to the generated query
			// (ignore eager/lazy loaded entities)
			var tablesInView = new TableCollection();
			// Load specific layers (starting with root table/entity)
			tablesInView.Add(Tables[0]);

			if (HasDirectChildrenToMergeInQuery)
			{
				foreach (var rtl in relationshipsToLoad)
				{
					var node = EntGraph
						.Where(g => g.BuildEntityTypePath() == rtl.BuildEntityTypePath() &&
								!rtl.EntGraphNode.Relationship.IsEagerLoaded &&
								!rtl.EntGraphNode.Relationship.IsLazyLoaded)
								.FirstOrDefault();

					if (node != null)
					{
						if (rtl.EntGraphNode.Relationship.IsEagerLoadedJoin)
							rtl.EntGraphNode.Relationship.EagerLoadedJoin.Join(this);
						else
							tablesInView.Add(new Table(node.EntityType, JoinType.None));
					}
				}
			}

			if (!IsJoin)
			{
				// Replace the base table with a view with tables
				View view = new View(Tables[0].Name, tablesInView.ToArray());
				Tables.ReplaceBaseTable(view);
			}
		}

		public virtual QueryBuilder<T> Page(int pageNumber, int pageSize)
		{
			EnablePaging = true;
			SkipCount = (pageNumber - 1) * pageSize;
			TakeCount = pageSize;
			return this;
		}

		private string[] ParseChildrenToLoad(Expression<Func<T, object>>[] childrenToLoad)
		{
			List<string> entitiesToLoad = new List<string>();

			// Parse relationship member names from expression array
			foreach (var exp in childrenToLoad)
			{
				MemberInfo member = (exp.Body as MemberExpression).Member;
				entitiesToLoad.Add(member.Name);

			}

			return entitiesToLoad.ToArray();
		}

		/// <summary>
		/// Allows you to interact with the DbDataReader to manually load entities.
		/// </summary>
		/// <param name="readerAction">An action that takes a DbDataReader.</param>
		public virtual void DataReader(Action<DbDataReader> readerAction)
		{
			if (string.IsNullOrEmpty(CommandText))
				throw new ArgumentNullException("The query text cannot be blank.");

			var mappingHelper = new MappingHelper(_db);
			_db.Command.CommandText = CommandText;

			try
			{
				_db.OpenConnection();
				using (DbDataReader reader = _db.Command.ExecuteReader())
				{
					readerAction.Invoke(reader);
				}
			}
			finally
			{
				_db.CloseConnection();
			}
		}

		public virtual int GetRowCount()
		{
			SqlModes previousSqlMode = _db.SqlMode;

			// Generate a row count query
			string where = _whereBuilder != null ? _whereBuilder.ToString() : string.Empty;

			bool useAltNames = HasDirectChildrenToMergeInQuery;
			IQuery query = QueryFactory.CreateRowCountSelectQuery(Tables, _db, where, SortBuilder, useAltNames);
			string queryText = query.Generate();

			_db.SqlMode = SqlModes.Text;
			int count = Convert.ToInt32(_db.ExecuteScalar(queryText));

			_db.SqlMode = previousSqlMode;
			return count;
		}

		/// <summary>
		/// Executes the query and returns a list of results.
		/// </summary>
		/// <returns>A list of query results of type T.</returns>
		public virtual List<T> ToList()
		{
			SqlModes previousSqlMode = _db.SqlMode;

			ValidateQuery();

			BuildQueryOrAppendClauses();

			var rootQuery = RootQuery ?? this;
			if (rootQuery.IsGraph || rootQuery.IsJoin)
			{
				// Project a query join results into an object graph
				_results = (List<T>)_db.QueryToGraph<T>(this);
			}
			else
			{
				_results = (List<T>)_db.Query<T>(CommandText, _results, this);
			}

			// Return to previous sql mode
			_db.SqlMode = previousSqlMode;

			return _results;
		}

		internal void SetSingleOrFirstAllowNull(bool allowNullResult)
		{
			_singleOrFirstNullable = allowNullResult;
		}

		internal T GetSingleOrFirstResult()
		{
			return _singleOrFirstNullable ?
				ToList().FirstOrDefault() :
				ToList().First();
		}

		private void ValidateQuery()
		{
			if (IsManualQuery && IsFromView)
				throw new InvalidOperationException("Cannot use FromView in conjunction with QueryText");

			if (IsManualQuery && IsFromTable)
				throw new InvalidOperationException("Cannot use FromTable in conjunction with QueryText");

			if (IsManualQuery && IsJoin)
				throw new InvalidOperationException("Cannot use Join in conjuntion with QueryText");

			if (IsManualQuery && EnablePaging)
				throw new InvalidOperationException("Cannot use Page, Skip or Take in conjunction with QueryText");

			if (IsJoin && IsFromView)
				throw new InvalidOperationException("Cannot use FromView in conjunction with Join");

			if (IsJoin && IsFromTable)
				throw new InvalidOperationException("Cannot use FromView in conjunction with Join");

			if (IsFromView && IsFromTable)
				throw new InvalidOperationException("Cannot use FromView in conjunction with FromTable");
		}

		private void BuildQueryOrAppendClauses()
		{
			if (CommandText == null)
			{
				// Build entire query
				_db.SqlMode = SqlModes.Text;
				BuildQuery();
			}
			else if (_whereBuilder != null || _sortBuilder != null)
			{
				_db.SqlMode = SqlModes.Text;
				if (_whereBuilder != null)
				{
					// Append a where clause to an existing query
					CommandText = string.Concat(CommandText, " ", _whereBuilder.ToString());
				}

				if (_sortBuilder != null)
				{
					// Append an order clause to an existing query
					CommandText = string.Concat(CommandText, " ", _sortBuilder.ToString());
				}
			}
		}

		public virtual string BuildQuery()
		{
			// Generate a query
			string where = _whereBuilder != null ? _whereBuilder.ToString() : string.Empty;

			bool useAltNames = HasDirectChildrenToMergeInQuery;

			IQuery query = null;
			if (EnablePaging)
			{
				query = QueryFactory.CreatePagingSelectQuery(Tables, _db, where, SortBuilder, useAltNames, SkipCount, TakeCount);
			}
			else
			{
				query = QueryFactory.CreateSelectQuery(Tables, _db, where, SortBuilder, useAltNames);
			}

			CommandText = query.Generate();

			return CommandText;
		}

		#endregion

		#region - Helper Methods -

		internal override Type GetEntityType()
		{
			return typeof(T);
		}

		internal override EntityGraph LoadEntityGraphForT()
		{
			return new EntityGraph(typeof(T), _results);
		}

		internal override IList GetResultsListForT()
		{
			return _results;
		}

		public static implicit operator List<T>(QueryBuilder<T> builder)
		{
			return builder.ToList();
		}

		#endregion

		#region - Linq Support -

		public virtual SortBuilder<T> Where<TObj>(Expression<Func<TObj, bool>> filterExpression)
		{
			bool useAltNames = HasDirectChildrenToMergeInQuery && !IsJoin;
			bool addTablePrefixToColumns = true;
			_whereBuilder = new WhereBuilder<T>(_db.Command, _dialect, filterExpression, Tables, useAltNames, addTablePrefixToColumns);
			return SortBuilder;
		}

		public virtual SortBuilder<T> Where(Expression<Func<T, bool>> filterExpression)
		{
			bool useAltNames = HasDirectChildrenToMergeInQuery && !IsJoin;
			bool addTablePrefixToColumns = true;
			_whereBuilder = new WhereBuilder<T>(_db.Command, _dialect, filterExpression, Tables, useAltNames, addTablePrefixToColumns);
			return SortBuilder;
		}

		public virtual SortBuilder<T> Where(string whereClause)
		{
			if (string.IsNullOrEmpty(whereClause))
				throw new ArgumentNullException("whereClause");

			if (!whereClause.ToUpper().Contains("WHERE "))
			{
				whereClause = whereClause.Insert(0, " WHERE ");
			}

			bool useAltNames = HasDirectChildrenToMergeInQuery && !IsJoin;
			_whereBuilder = new WhereBuilder<T>(whereClause, useAltNames);
			return SortBuilder;
		}

		// Used by QuerableEntityContext / IQueryable
		internal SortBuilder<T> AddSortExpression(Expression exp, SortDirection dir)
		{
			SortBuilder.AddSortExpression(exp, dir);
			return SortBuilder;
		}

		public virtual SortBuilder<T> OrderBy(Expression<Func<T, object>> sortExpression)
		{
			SortBuilder.OrderBy(sortExpression);
			return SortBuilder;
		}

		public virtual SortBuilder<T> OrderBy(Expression<Func<T, object>> sortExpression, SortDirection sortDirection)
		{
			SortBuilder.OrderBy(sortExpression, sortDirection);
			return SortBuilder;
		}

		public virtual SortBuilder<T> ThenBy(Expression<Func<T, object>> sortExpression)
		{
			SortBuilder.OrderBy(sortExpression);
			return SortBuilder;
		}

		public virtual SortBuilder<T> ThenBy(Expression<Func<T, object>> sortExpression, SortDirection sortDirection)
		{
			SortBuilder.OrderBy(sortExpression, sortDirection);
			return SortBuilder;
		}

		public virtual SortBuilder<T> OrderByDescending(Expression<Func<T, object>> sortExpression)
		{
			SortBuilder.OrderByDescending(sortExpression);
			return SortBuilder;
		}

		public virtual SortBuilder<T> ThenByDescending(Expression<Func<T, object>> sortExpression)
		{
			SortBuilder.OrderByDescending(sortExpression);
			return SortBuilder;
		}

		public virtual SortBuilder<T> OrderBy(string orderByClause)
		{
			if (string.IsNullOrEmpty(orderByClause))
				throw new ArgumentNullException("orderByClause");

			if (!orderByClause.ToUpper().Contains("ORDER BY "))
			{
				orderByClause = orderByClause.Insert(0, " ORDER BY ");
			}

			SortBuilder.OrderBy(orderByClause);
			return SortBuilder;
		}

		public virtual QueryBuilder<T> Take(int count)
		{
			EnablePaging = true;
			TakeCount = count;
			return this;
		}

		public virtual QueryBuilder<T> Skip(int count)
		{
			EnablePaging = true;
			SkipCount = count;
			return this;
		}

		/// <summary>
		/// Handles all.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		protected override System.Linq.Expressions.Expression Visit(System.Linq.Expressions.Expression expression)
		{
			return base.Visit(expression);
		}

		/// <summary>
		/// Handles Where.
		/// </summary>
		/// <param name="lambdaExpression"></param>
		/// <returns></returns>
		protected override System.Linq.Expressions.Expression VisitLamda(System.Linq.Expressions.LambdaExpression lambdaExpression)
		{
			_sortBuilder = this.Where(lambdaExpression as Expression<Func<T, bool>>);
			return base.VisitLamda(lambdaExpression);
		}

		/// <summary>
		/// Handles OrderBy.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		protected override System.Linq.Expressions.Expression VisitMethodCall(MethodCallExpression expression)
		{
			if (expression.Method.Name == "OrderBy" || expression.Method.Name == "ThenBy")
			{
				var memberExp = ((expression.Arguments[1] as UnaryExpression).Operand as System.Linq.Expressions.LambdaExpression).Body as System.Linq.Expressions.MemberExpression;
				_sortBuilder.Order(memberExp.Expression.Type, memberExp.Member.Name);
			}
			if (expression.Method.Name == "OrderByDescending" || expression.Method.Name == "ThenByDescending")
			{
				var memberExp = ((expression.Arguments[1] as UnaryExpression).Operand as System.Linq.Expressions.LambdaExpression).Body as System.Linq.Expressions.MemberExpression;
				_sortBuilder.OrderByDescending(memberExp.Expression.Type, memberExp.Member.Name);
			}

			return base.VisitMethodCall(expression);
		}

		public virtual QueryBuilder<T> Join<TLeft, TRight>(JoinType joinType, Expression<Func<TLeft, IEnumerable<TRight>>> rightEntity, Expression<Func<TLeft, TRight, bool>> filterExpression)
		{
			IsJoin = true;
			HasDirectChildrenToMergeInQuery = true;
			MemberInfo rightMember = (rightEntity.Body as MemberExpression).Member;
			return this.Join(joinType, rightMember, filterExpression);
		}

		public virtual QueryBuilder<T> Join<TLeft, TRight>(JoinType joinType, Expression<Func<TLeft, TRight>> rightEntity, Expression<Func<TLeft, TRight, bool>> filterExpression)
		{
			IsJoin = true;
			HasDirectChildrenToMergeInQuery = true;
			MemberInfo rightMember = (rightEntity.Body as MemberExpression).Member;
			return this.Join(joinType, rightMember, filterExpression);
		}

		public virtual QueryBuilder<T> Join<TLeft, TRight>(JoinType joinType, MemberInfo rightMember, Expression<Func<TLeft, TRight, bool>> filterExpression)
		{
			IsJoin = true;
			HasDirectChildrenToMergeInQuery = true;

			var rightNode = EntGraph
				.Where(g => g.Member != null && g.Member.EqualsMember(rightMember))
				.FirstOrDefault();

			if (rightNode != null && !RelationshipsToLoad.Any(rtl => rtl.BuildEntityTypePath() == rightNode.BuildEntityTypePath()))
				RelationshipsToLoad.Add(new RelationshipLoadRequest(rightNode));

			Table table = new Table(typeof(TRight), joinType);
			Tables.Add(table);

			JoinBuilder<TLeft, TRight> builder = null;
			try
			{
				builder = new JoinBuilder<TLeft, TRight>(_db.Command, _dialect, filterExpression, Tables);
			}
			catch (Exception ex)
			{
				throw new RelationshipLoadException(
					string.Format("Join failed for {0} -> {1}.", typeof(TLeft).Name, rightMember.Name),
					ex);
			}
			
			table.JoinClause = builder.ToString();
			return this;
		}


		#endregion

		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			var list = this.ToList();
			return list.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			var list = this.ToList();
			return list.GetEnumerator();
		}

		#endregion
	}
}
