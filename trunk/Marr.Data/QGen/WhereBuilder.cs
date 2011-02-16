using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Marr.Data;
using Marr.Data.Mapping;
using System.Data.Common;
using Marr.Data.Parameters;
using System.Reflection;

namespace Marr.Data.QGen
{
    public class WhereBuilder<T>
    {
        private DbCommand _command;
        private StringBuilder _sb;
        private string _paramPrefix;
        private bool _useAltName;

        public WhereBuilder(DbCommand command, Expression<Func<T, bool>> filter, bool useAltName)
        {
            _command = command;
            _paramPrefix = command.ParameterPrefix();
            _sb = new StringBuilder();
            _useAltName = useAltName;

            if (filter != null)
            {
                _sb.Append("WHERE (");

                ParseExpression(filter.Body);

                _sb.Append(")");
            }            
        }

        private void ParseExpression(Expression body)
        {
            if (body is BinaryExpression)
                ParseBinaryExpression((BinaryExpression)body);
            else if (body is MethodCallExpression)
                ParseMethodCallExpression((MethodCallExpression)body);
            else
                throw new NotImplementedException(string.Format("{0} expressions are not currently supported.", body.NodeType.ToString()));
        }

        /// <summary>
        /// Iterates through a BinaryExpression tree and simplifies nested BinaryExpressions 
        /// until they can be converted into a parameterized SQL where clause.
        /// </summary>
        /// <param name="body">The current expression node.</param>
        private void ParseBinaryExpression(BinaryExpression body)
        {
            if (body.Left is MemberExpression)
            {
                WriteExpression(body);
            }
            else
            {
                _sb.Append("(");
                ParseExpression(body.Left);
                _sb.AppendFormat(" {0} ", Decode(body.NodeType));
                ParseExpression(body.Right);
                _sb.Append(")");
            }
        }

        /// <summary>
        /// 1) Converts a binary expression into a where clause
        /// 2) Examines the property to see if it has a ColumnAttribute, and if so, substitutes the overriden column name, if one exists
        /// 3) Adds the parameters to the DbCommand
        /// </summary>
        /// <param name="body">A binary expression that consists of a MemberExpression and a ConstantExpression.</param>
        private void WriteExpression(BinaryExpression body)
        {
            var left = body.Left as MemberExpression;

            var rightValue = GetRightValue(body.Right);

            string statement = string.Format("{0} {1} {2}", left.Member.Name, body.NodeType, rightValue);

            string columnName = left.Member.GetColumnName(_useAltName);

            // Add parameter to Command.Parameters
            string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());
            var parameter = new ParameterChainMethods(_command, paramName, rightValue).Parameter;

            _sb.AppendFormat("[{0}] {1} {2}", columnName, Decode(body.NodeType), paramName);
        }
        
        private object GetRightValue(Expression rightExpression)
        {
            object rightValue = null;

            var right = rightExpression as ConstantExpression;
            if (right == null) // Value is not directly passed in as a constant
            {
                var rightMemberExp = (rightExpression as MemberExpression);
                var parentMemberExpression = rightMemberExp.Expression as MemberExpression;
                if (parentMemberExpression != null) // Value is passed in as a property on a parent entity
                {
                    string entityName = (rightMemberExp.Expression as MemberExpression).Member.Name;
                    var container = ((rightMemberExp.Expression as MemberExpression).Expression as ConstantExpression).Value;
                    var entity = ReflectionHelper.GetFieldValue(container, entityName);
                    rightValue = ReflectionHelper.GetFieldValue(entity, rightMemberExp.Member.Name);
                }
                else // Value is passed in as a variable
                {
                    var parent = (rightMemberExp.Expression as ConstantExpression).Value;
                    rightValue = ReflectionHelper.GetFieldValue(parent, rightMemberExp.Member.Name);
                }
            }
            else // Value is passed in directly as a constant
            {
                rightValue = right.Value;
            }

            return rightValue;
        }

        private string Decode(ExpressionType expType)
        {
            switch (expType)
            {
                case ExpressionType.AndAlso: return "AND";
                case ExpressionType.And: return "AND";
                case ExpressionType.Equal: return "=";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.NotEqual: return "<>";
                case ExpressionType.OrElse: return "OR";
                case ExpressionType.Or: return "OR";
                default: throw new NotSupportedException(string.Format("{0} statement is not supported", expType.ToString()));
            }
        }

        private void ParseMethodCallExpression(MethodCallExpression body)
        {
            string method = (body as System.Linq.Expressions.MethodCallExpression).Method.Name;
            switch (method)
            {
                case "Contains":
                    Write_Contains(body);
                    break;

                case "StartsWith":
                    Write_StartsWith(body);
                    break;

                case "EndsWith":
                    Write_EndsWith(body);
                    break;
            }
        }

        private void Write_Contains(MethodCallExpression body)
        {
            // Add parameter to Command.Parameters
            string search = body.Arguments[0].ToString().Replace("\"", string.Empty);
            string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());
            var parameter = new ParameterChainMethods(_command, paramName, search).Parameter;

            string columnName = (body.Object as MemberExpression).Member.GetColumnName(_useAltName);
            _sb.AppendFormat("[{0}] LIKE '%' + {1} + '%'", columnName, paramName);
        }

        private void Write_StartsWith(MethodCallExpression body)
        {
            // Add parameter to Command.Parameters
            string search = body.Arguments[0].ToString().Replace("\"", string.Empty);
            string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());
            var parameter = new ParameterChainMethods(_command, paramName, search).Parameter;

            string columnName = (body.Object as MemberExpression).Member.GetColumnName(_useAltName);
            _sb.AppendFormat("[{0}] LIKE {1} + '%'", columnName, paramName);
        }

        private void Write_EndsWith(MethodCallExpression body)
        {
            // Add parameter to Command.Parameters
            string search = body.Arguments[0].ToString().Replace("\"", string.Empty);
            string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());
            var parameter = new ParameterChainMethods(_command, paramName, search).Parameter;

            string columnName = (body.Object as MemberExpression).Member.GetColumnName(_useAltName);
            _sb.AppendFormat("[{0}] LIKE '%' + {1}", columnName, paramName);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    } 
}
