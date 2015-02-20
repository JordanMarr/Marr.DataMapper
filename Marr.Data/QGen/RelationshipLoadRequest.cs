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
			TypePath = new List<Type>();
			MemberPath = new List<MemberInfo>();

			// Populate MemberPath
			Visit(relationshipToLoadExp);
			MemberPath.Reverse();
			TypePath.Reverse();
		}

		internal RelationshipLoadRequest(EntityGraph entGraphNode)
		{
			TypePath = new List<Type>();
			MemberPath = new List<MemberInfo>();
			EntGraphNode = entGraphNode;

			// Populate MemberPath and TypePath
			var node = entGraphNode;
			while (node != null)
			{
				if (node.Member != null)
					MemberPath.Add(node.Member);

				if (node.Parent != null) // Do not add root entity type
					TypePath.Add(node.EntityType);

				node = node.Parent;
			}
			MemberPath.Reverse();
			TypePath.Reverse();
		}

		internal EntityGraph EntGraphNode { get; set; }
		public List<MemberInfo> MemberPath { get; private set; }
		public List<Type> TypePath { get; private set; }

		public string BuildEntityTypePath()
		{
			return string.Join("-", TypePath.Select(t => t.Name).ToArray());
		}

		public string BuildMemberPath()
		{
			return string.Join("-", MemberPath.Select(m => m.Name).ToArray());
		}

		#region - Expression Visitor -

		protected override Expression VisitMemberAccess(MemberExpression memberExp)
		{
			MemberPath.Add(memberExp.Member);
			var type = (memberExp.Member as PropertyInfo).PropertyType;
			if (!type.IsGenericType)
				TypePath.Add(type);
			else
				TypePath.Add(type.GetGenericArguments().First());
			
			// Nested relationships are added using the First() method
			var methodExp = memberExp.Expression as MethodCallExpression;
			if (methodExp != null)
			{
				Visit(methodExp);
			}

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
				//Visit(methodCallExp);
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
