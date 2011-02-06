using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Marr.Data.Mapping;
using System.Data.Common;
using Marr.Data.Parameters;

namespace Marr.Data.QGen
{
    public class WhereCondition<T>
    {
        private DbCommand _command;
        private StringBuilder _sb;
        private string _paramPrefix;

        public WhereCondition(DbCommand command, Expression<Func<T, bool>> filter)
        {
            string commandType = command.GetType().Name.ToLower();
            if (commandType.Contains("oracle"))
                _paramPrefix = ":";
            else
                _paramPrefix = "@";

            _command = command;
            _sb = new StringBuilder("WHERE (");

            ParseExpression((BinaryExpression)filter.Body);

            _sb.Append(")");
        }

        /// <summary>
        /// Iterates through a BinaryExpression tree and simplifies nested BinaryExpressions 
        /// until they can be converted into a parameterized SQL where clause.
        /// </summary>
        /// <param name="body">The current expression node.</param>
        private void ParseExpression(BinaryExpression body)
        {
            if (body.Left is BinaryExpression)
            {
                _sb.Append("(");
                ParseExpression(body.Left as BinaryExpression);
                _sb.AppendFormat(" {0} ", Decode(body.NodeType));
                ParseExpression(body.Right as BinaryExpression);
                _sb.Append(")");
            }
            else
            {
                // Write to sb
                WriteExpression(body);
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
            var right = body.Right as ConstantExpression;

            string statement = string.Format("{0} {1} {2}", left.Member.Name, body.NodeType, right.Value);

            // Initialize column name as member name
            string columnName = left.Member.Name;

            // If column name is overridden at ColumnAttribute level, use that name instead
            object[] attributes = left.Member.GetCustomAttributes(typeof(ColumnAttribute), false);
            if (attributes.Length > 0)
            {
                ColumnAttribute column = (attributes[0] as ColumnAttribute);
                if (!string.IsNullOrEmpty(column.Name))
                    columnName = (attributes[0] as ColumnAttribute).Name;
            }

            // Add parameter to Command.Parameters
            string paramName = string.Concat(_paramPrefix, "P", _command.Parameters.Count.ToString());
            var parameter = new ParameterChainMethods(_command, paramName, right.Value).Parameter;

            _sb.AppendFormat("[{0}] {1} {2}", columnName, Decode(body.NodeType), paramName);
        }

        private string Decode(ExpressionType expType)
        {
            switch (expType)
            {
                case ExpressionType.AndAlso: return "AND";
                case ExpressionType.Equal: return "=";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.NotEqual: return "<>";
                case ExpressionType.OrElse: return "OR";
                default: throw new NotSupportedException(string.Format("{0} statement is not supported", expType.ToString()));
            }
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    } 
    
    public enum ConditionType
    {
        AND,
        OR
    }
}
