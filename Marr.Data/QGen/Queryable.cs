using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Marr.Data.Parameters;

namespace Marr.Data.QGen
{
	/// <summary>
	/// Creates an IQueryable context for the given TEntity.
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	public class QuerableEntityContext<TEntity> : ExpressionVisitor, IQueryContext
	{
		private QueryBuilder<TEntity> _queryBuilder;
		private SortBuilder<TEntity> _sortBuilder;

		public QuerableEntityContext(QueryBuilder<TEntity> queryBuilder)
		{
			_queryBuilder = queryBuilder;
		}
		
		public object Execute(Expression expression, bool isEnumerable)
		{
			Visit(expression);
			return isEnumerable ?
				(object)_queryBuilder.ToList() :
				(object)_queryBuilder.GetSingleOrFirstResult();
		}

		#region - Visitor -

		/// <summary>
		/// Handles OrderBy.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		protected override System.Linq.Expressions.Expression VisitMethodCall(MethodCallExpression expression)
		{
			switch (expression.Method.Name)
			{
				case "Where":
					var quote = expression.Arguments[1] as UnaryExpression;
					if(quote != null)
					{
						var predicate = quote.Operand as Expression<Func<TEntity, bool>>;
						_sortBuilder = _queryBuilder.Where(predicate);
					}
					break;

				case "OrderBy":
				case "ThenBy":
					
					this.Visit(expression.Arguments[0]);

					var keySelectorAsc = expression.Arguments[1] as UnaryExpression;
					if (keySelectorAsc != null)
					{
						if (_sortBuilder == null)
							_sortBuilder = _queryBuilder.AddSortExpression(keySelectorAsc.Operand, SortDirection.Asc);
						else
							_sortBuilder.AddSortExpression(keySelectorAsc.Operand, SortDirection.Asc);
					}
					break;

				case "OrderByDescending":
				case "ThenByDescending":

					this.Visit(expression.Arguments[0]);

					var keySelectorDesc = expression.Arguments[1] as UnaryExpression;
					if (keySelectorDesc != null)
					{
						if (_sortBuilder == null)
							_sortBuilder = _queryBuilder.AddSortExpression(keySelectorDesc.Operand, SortDirection.Desc);
						else
							_sortBuilder.AddSortExpression(keySelectorDesc.Operand, SortDirection.Desc);
					}
					break;

				case "Skip":
					this.Visit(expression.Arguments[0]);
					int skipVal = (int)GetConstantValue(expression.Arguments[1]);
					_queryBuilder.Skip(skipVal);
					break;

				case "Take":
					this.Visit(expression.Arguments[0]);
					int takeVal = (int)GetConstantValue(expression.Arguments[1]);
					_queryBuilder.Take(takeVal);
					break;

				case "FirstOrDefault":
				case "SingleOrDefault":
					this.Visit(expression.Arguments[0]);
					_queryBuilder.SetSingleOrFirstAllowNull(true);
					break;

				case "First":
				case "Single":
					this.Visit(expression.Arguments[0]);
					_queryBuilder.SetSingleOrFirstAllowNull(false);
					break;

				default:
					string msg = string.Format("'{0}' expressions are not currently supported in the where clause expression tree parser.", expression.Method.Name);
					throw new NotSupportedException(msg);
			}

			return expression;
		}

		protected override Expression VisitUnary(UnaryExpression u)
		{
			switch (u.NodeType)
			{
				case ExpressionType.Not:
					//sb.Append(" NOT ");
					this.Visit(u.Operand);
					break;
				default:
					throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
			}
			return u;
		}

		private static Expression StripQuotes(Expression e)
		{
			while (e.NodeType == ExpressionType.Quote)
			{
				e = ((UnaryExpression)e).Operand;
			}
			return e;
		}

		private object GetConstantValue(Expression expression)
		{
			var constExp = expression as ConstantExpression;
			return constExp.Value;
		}
		
		#endregion
	}

	public interface IQueryContext
	{
		object Execute(Expression expression, bool isEnumerable);
	}

	public class Queryable<T> : IOrderedQueryable<T>
	{
		public Queryable(IQueryContext queryContext)
		{
			Initialize(new QueryProvider(queryContext), null);
		}

		public Queryable(IQueryProvider provider)
		{
			Initialize(provider, null);
		}

		internal Queryable(IQueryProvider provider, Expression expression)
		{
			Initialize(provider, expression);
		}

		private void Initialize(IQueryProvider provider, Expression expression)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");
			if (expression != null && !typeof(IQueryable<T>).
				   IsAssignableFrom(expression.Type))
				throw new ArgumentException(
					 String.Format("Not assignable from {0}", expression.Type), "expression");

			Provider = provider;
			Expression = expression ?? Expression.Constant(this);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return (Provider.Execute<IEnumerable<T>>(Expression)).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return (Provider.Execute<System.Collections.IEnumerable>(Expression)).GetEnumerator();
		}

		public Type ElementType
		{
			get { return typeof(T); }
		}

		public Expression Expression { get; private set; }
		public IQueryProvider Provider { get; private set; }
	}

	public class QueryProvider : IQueryProvider
	{
		private readonly IQueryContext queryContext;

		public QueryProvider(IQueryContext queryContext)
		{
			this.queryContext = queryContext;
		}

		public virtual IQueryable CreateQuery(Expression expression)
		{
			Type elementType = TypeSystem.GetElementType(expression.Type);
			try
			{
				return
				   (IQueryable)Activator.CreateInstance(typeof(Queryable<>).
						  MakeGenericType(elementType), new object[] { this, expression });
			}
			catch (TargetInvocationException e)
			{
				throw e.InnerException;
			}
		}

		public virtual IQueryable<T> CreateQuery<T>(Expression expression)
		{
			return new Queryable<T>(this, expression);
		}

		object IQueryProvider.Execute(Expression expression)
		{
			return queryContext.Execute(expression, false);
		}

		T IQueryProvider.Execute<T>(Expression expression)
		{
			return (T)queryContext.Execute(expression,
					   (typeof(T).Name == "IEnumerable`1"));
		}
	}

	internal static class TypeSystem
	{
		internal static Type GetElementType(Type seqType)
		{
			Type ienum = FindIEnumerable(seqType);
			if (ienum == null) return seqType;
			return ienum.GetGenericArguments()[0];
		}

		private static Type FindIEnumerable(Type seqType)
		{
			if (seqType == null || seqType == typeof(string))
				return null;

			if (seqType.IsArray)
				return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());

			if (seqType.IsGenericType)
			{
				foreach (Type arg in seqType.GetGenericArguments())
				{
					Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
					if (ienum.IsAssignableFrom(seqType))
					{
						return ienum;
					}
				}
			}

			Type[] ifaces = seqType.GetInterfaces();
			if (ifaces != null && ifaces.Length > 0)
			{
				foreach (Type iface in ifaces)
				{
					Type ienum = FindIEnumerable(iface);
					if (ienum != null) return ienum;
				}
			}

			if (seqType.BaseType != null && seqType.BaseType != typeof(object))
			{
				return FindIEnumerable(seqType.BaseType);
			}

			return null;
		}
	}
}
