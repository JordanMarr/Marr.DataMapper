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
using Marr.Data.QGen.Dialects;

namespace Marr.Data.QGen
{
    public class WhereBuilder<T> : ExpressionVisitor
    {
        private MapRepository _repos;
        private DbCommand _command;
        private Dialect _dialect;
        private StringBuilder _sb;
        private string _paramPrefix;
        private bool _useAltName;
        private bool isLeftSide = true;

        public WhereBuilder(DbCommand command, Dialect dialect, Expression<Func<T, bool>> filter, bool useAltName)
        {
            _repos = MapRepository.Instance;
            _command = command;
            _dialect = dialect;
            _paramPrefix = command.ParameterPrefix();
            _sb = new StringBuilder();
            _useAltName = useAltName;

            if (filter != null)
            {
                _sb.Append("WHERE (");

                base.Visit(filter.Body);

                _sb.Append(")");
            }            
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            _sb.Append("(");

            isLeftSide = true;
            Visit(expression.Left);

            _sb.AppendFormat(" {0} ", Decode(expression.NodeType));

            isLeftSide = false;
            Visit(expression.Right);

            _sb.Append(")");

            return expression;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            string method = (expression as System.Linq.Expressions.MethodCallExpression).Method.Name;
            switch (method)
            {
                case "Contains":
                    Write_Contains(expression);
                    break;

                case "StartsWith":
                    Write_StartsWith(expression);
                    break;

                case "EndsWith":
                    Write_EndsWith(expression);
                    break;

                default:
                    string msg = string.Format("'{0}' expressions are not yet implemented in the where clause expression tree parser.", method);
                    throw new NotImplementedException(msg);
            }

            return expression;
        }

        protected override Expression VisitMemberAccess(MemberExpression expression)
        {
            if (isLeftSide)
            {
                string columnName = expression.Member.GetColumnName(_useAltName);
                _sb.Append(_dialect.CreateToken(columnName));
            }
            else
            {
                // Add parameter to Command.Parameters
                string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());
                _sb.Append(paramName);

                object value = GetRightValue(expression);
                new ParameterChainMethods(_command, paramName, value);
            }

            return expression;
        }

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            // Add parameter to Command.Parameters
            string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());

            _sb.Append(paramName);

            var parameter = new ParameterChainMethods(_command, paramName, expression.Value).Parameter;
            return expression;
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
                    var entity = _repos.ReflectionStrategy.GetFieldValue(container, entityName);
                    rightValue = _repos.ReflectionStrategy.GetFieldValue(entity, rightMemberExp.Member.Name);
                }
                else // Value is passed in as a variable
                {
                    var parent = (rightMemberExp.Expression as ConstantExpression).Value;
                    rightValue = _repos.ReflectionStrategy.GetFieldValue(parent, rightMemberExp.Member.Name);
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

        private void Write_Contains(MethodCallExpression body)
        {
            // Add parameter to Command.Parameters
            object value = GetRightValue(body.Arguments[0]);
            string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());
            var parameter = new ParameterChainMethods(_command, paramName, value).Parameter;

            string columnName = (body.Object as MemberExpression).Member.GetColumnName(_useAltName);
            _sb.AppendFormat("{0} LIKE '%' + {1} + '%'", _dialect.CreateToken(columnName), paramName);
        }

        private void Write_StartsWith(MethodCallExpression body)
        {
            // Add parameter to Command.Parameters
            object value = GetRightValue(body.Arguments[0]);
            string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());
            var parameter = new ParameterChainMethods(_command, paramName, value).Parameter;

            string columnName = (body.Object as MemberExpression).Member.GetColumnName(_useAltName);
            _sb.AppendFormat("{0} LIKE {1} + '%'", _dialect.CreateToken(columnName), paramName);
        }

        private void Write_EndsWith(MethodCallExpression body)
        {
            // Add parameter to Command.Parameters
            object value = GetRightValue(body.Arguments[0]);
            string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());
            var parameter = new ParameterChainMethods(_command, paramName, value).Parameter;

            string columnName = (body.Object as MemberExpression).Member.GetColumnName(_useAltName);
            _sb.AppendFormat("{0} LIKE '%' + {1}", _dialect.CreateToken(columnName), paramName);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    } 
}
