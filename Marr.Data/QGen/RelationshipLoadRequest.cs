using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Marr.Data.Mapping;

namespace Marr.Data.QGen
{
	/// <summary>
	/// Represents a relationship that has been requested.
	/// Parses all members in the expression.
	/// </summary>
	public class RelationshipLoadRequest : ExpressionVisitor
	{
		public RelationshipLoadRequest(Expression relationshipToLoadExp)
		{
			TypePath = new List<string>();

			// Populate MemberPath
			Visit(relationshipToLoadExp);
			TypePath.Reverse();
			EntityTypePath = BuildEntityTypePath();
		}

		internal RelationshipLoadRequest(EntityGraph entGraphNode)
		{
			TypePath = new List<string>();
			EntGraphNode = entGraphNode;

			// Populate MemberPath and TypePath
			var node = entGraphNode;
			while (node != null)
			{
				if (node.Parent != null) // Do not add root entity type
				{
					TypePath.Add(node.Member.Name);
					TypePath.Add(node.EntityType.Name);					
				}

				node = node.Parent;
			}
			TypePath.Reverse();
			EntityTypePath = BuildEntityTypePath();
		}

		internal EntityGraph EntGraphNode { get; set; }
		public List<string> TypePath { get; private set; }
		public string EntityTypePath { get; private set; }
		
		private string BuildEntityTypePath()
		{
			return string.Join("-", TypePath.Select(name => name).ToArray());
		}
		
		#region - Expression Visitor -

		protected override Expression VisitMemberAccess(MemberExpression memberExp)
		{
			var type = (memberExp.Member as PropertyInfo).PropertyType;

			if (!type.IsGenericType)
			{
				TypePath.Add(memberExp.Member.Name);
				TypePath.Add(type.Name);
			}
			else
			{
				TypePath.Add(memberExp.Member.Name);
				TypePath.Add(type.GetGenericArguments().First().Name);
			}
			
			Visit(memberExp.Expression);

			return memberExp;
		}

		/// <summary>
		/// Try to parse First() method.
		/// </summary>
		protected override Expression VisitMethodCall(MethodCallExpression expression)
		{
			var arg = expression.Arguments.First();
			Visit(arg);
			return expression;
		}

		/// <summary>
		/// Try to parse Select(...) method.
		/// </summary>
		protected override Expression VisitLamda(LambdaExpression lambdaExpression)
		{
			var methodCallExp = (lambdaExpression.Body as MethodCallExpression);
			if (methodCallExp != null)
			{
				foreach (var arg in methodCallExp.Arguments.Reverse())
				{
					Visit(arg);
				}
			}

			var memberExpression = lambdaExpression.Body as MemberExpression;
			if (memberExpression != null)
			{
				Visit(memberExpression);
			}

			return lambdaExpression;
		}
		
		#endregion
	}
}
