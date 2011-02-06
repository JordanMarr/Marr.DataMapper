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
        private ConditionType _conditionType;
        private DbCommand _command;
        private StringBuilder _sb;

        public WhereCondition(DbCommand command, params Expression<Func<T, bool>>[] filters)
            : this(command, ConditionType.AND, filters)
        { }

        public WhereCondition(DbCommand command, ConditionType conditionType, params Expression<Func<T, bool>>[] filters)
        {
            string paramPrefix = null;
            string commandType = command.GetType().Name.ToLower();
            if (commandType.Contains("oracle"))
                paramPrefix = ":";
            else
                paramPrefix = "@";

            _conditionType = conditionType;
            _command = command;
            _sb = new StringBuilder("WHERE (");

            int startIndex = _sb.Length;

            foreach (var filter in filters)
            {
                // Add AND/OR between filters
                if (_sb.Length > startIndex)
                    _sb.AppendFormat(" {0} ", _conditionType.ToString());

                var body = (BinaryExpression)filter.Body;
                var left = (MemberExpression)body.Left;
                var right = (ConstantExpression)body.Right;

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
                var parameter = new ParameterChainMethods(command, columnName, right.Value).Parameter;

                _sb.AppendFormat("[{0}] {1} {2}{3}", columnName, Decode(body.NodeType), paramPrefix, parameter.ParameterName);
            }

            _sb.Append(")");
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
