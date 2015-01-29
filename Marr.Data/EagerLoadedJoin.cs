using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Marr.Data.QGen;

namespace Marr.Data
{
	public interface IEagerLoadedJoin
	{
		void Join<TEntity>(QueryBuilder<TEntity> query);
	}

	/// <summary>
	/// This is the eager loaded join definition specified via the fluent mappings.
	/// </summary>
	/// <typeparam name="TLeft">The parent entity that contains the eager loaded entity.</typeparam>
	/// <typeparam name="TRight">The child entity that is being eager loaded.</typeparam>
	public class EagerLoadedJoin<TLeft, TRight> : IEagerLoadedJoin
	{
		public JoinType JoinType { get; set; }

		public Expression<Func<TLeft, TRight>> RightEntityOne { get; set; }

		public Expression<Func<TLeft, IEnumerable<TRight>>> RightEntityMany { get; set; }

		public Expression<Func<TLeft, TRight, bool>> JoinOn { get; set; }

		public void Join<TEntity>(QueryBuilder<TEntity> query)
		{
			if (RightEntityOne != null)
			{
				query.Join(JoinType, RightEntityOne, JoinOn);
			}
			else if (RightEntityMany != null)
			{
				query.Join(JoinType, RightEntityMany, JoinOn);
			}
		}
	}

}
