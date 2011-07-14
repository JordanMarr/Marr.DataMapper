using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using Marr.Data.QGen.Dialects;

namespace Marr.Data.QGen
{
    /// <summary>
    /// Decorates the SelectQuery by wrapping it in a paging query.
    /// </summary>
    public class PagingQueryDecorator : IQuery
    {
        private SelectQuery _innerQuery;
        private int _firstRow;
        private int _lastRow;

        public PagingQueryDecorator(SelectQuery innerQuery, int skip, int take)
        {
            if (string.IsNullOrEmpty(innerQuery.OrderBy))
            {
                throw new DataMappingException("A paged query must specify an order by clause.");
            }

            _innerQuery = innerQuery;
            _firstRow = skip + 1;
            _lastRow = skip + take;
        }

        public string Generate()
        {
            // Decide which type of paging query to create

            bool isView = _innerQuery.Tables[0] is View;
            bool isJoin = _innerQuery.Tables.Count > 1;

            if (isView || isJoin)
            {
                return ComplexPaging();
            }
            else
            {
                return SimplePaging();
            }
        }

        /// <summary>
        /// Generates a query that pages a simple inner query.
        /// </summary>
        /// <returns></returns>
        private string SimplePaging()
        {
            // Create paged query
            StringBuilder sql = new StringBuilder();

            sql.AppendLine("WITH RowNumCTE AS");
            sql.AppendLine("(");
            _innerQuery.BuildSelectClause(sql);
            BuildRowNumberColumn(sql);
            _innerQuery.BuildFromClause(sql);
            _innerQuery.BuildJoinClauses(sql);
            _innerQuery.BuildWhereClause(sql);
            sql.AppendLine(")");
            BuildSimpleOuterSelect(sql);

            return sql.ToString();
        }
        
        /// <summary>
        /// Generates a query that pages a view or joined inner query.
        /// </summary>
        /// <returns></returns>
        private string ComplexPaging()
        {
            // Create paged query
            StringBuilder sql = new StringBuilder();

            sql.AppendLine("WITH GroupCTE AS (");
            _innerQuery.BuildSelectClause(sql);
            BuildGroupColumn(sql);
            _innerQuery.BuildFromClause(sql);
            _innerQuery.BuildJoinClauses(sql);
            _innerQuery.BuildWhereClause(sql);
            sql.AppendLine("),");
            sql.AppendLine("RowNumCTE AS (");
            sql.AppendLine("SELECT *");
            BuildRowNumberColumn(sql);
            sql.AppendLine("FROM GroupCTE");
            sql.AppendLine("WHERE GroupRow = 1");
            sql.AppendLine(")");
            _innerQuery.BuildSelectClause(sql);
            _innerQuery.BuildFromClause(sql);
            _innerQuery.BuildJoinClauses(sql);
            BuildJoinBackToCTE(sql);
            sql.AppendFormat("WHERE RowNumber BETWEEN {0} AND {1}", _firstRow, _lastRow);

            return sql.ToString();
        }

        private void BuildJoinBackToCTE(StringBuilder sql)
        {
            Table baseTable = _innerQuery.Tables[0];
            sql.AppendLine("INNER JOIN RowNumCTE cte");
            int pksAdded = 0;
            foreach (var pk in _innerQuery.Tables[0].Columns.PrimaryKeys)
            {
                if (pksAdded > 0)
                    sql.Append(" AND ");

                string pkName = _innerQuery.NameOrAltName(pk.ColumnInfo);
                sql.AppendFormat("ON cte.{0} = {1} ", pkName, _innerQuery.Dialect.CreateToken(string.Concat("t0", ".", pkName)));
                pksAdded++;
            }
            sql.AppendLine();
        }

        private void BuildSimpleOuterSelect(StringBuilder sql)
        {
            sql.Append("SELECT ");
            int startIndex = sql.Length;

            // COLUMNS
            foreach (Table join in _innerQuery.Tables)
            {
                for (int i = 0; i < join.Columns.Count; i++)
                {
                    var c = join.Columns[i];

                    if (sql.Length > startIndex)
                        sql.Append(",");

                    string token = _innerQuery.NameOrAltName(c.ColumnInfo);
                    sql.Append(_innerQuery.Dialect.CreateToken(token));
                }
            }

            sql.AppendLine("FROM RowNumCTE");
            sql.AppendFormat("WHERE RowNumber BETWEEN {0} AND {1}", _firstRow, _lastRow).AppendLine();
            sql.AppendLine("ORDER BY RowNumber ASC;");
        }

        private void BuildGroupColumn(StringBuilder sql)
        {
            sql.AppendFormat(", ROW_NUMBER() OVER (PARTITION BY {0} {1}) As GroupRow ", BuildBaseTablePKColumns(), _innerQuery.OrderBy);
        }

        private string BuildBaseTablePKColumns()
        {
            Table baseTable = _innerQuery.Tables[0];
            StringBuilder sb = new StringBuilder();
            foreach (var col in baseTable.Columns.PrimaryKeys)
            {
                if (sb.Length > 0)
                    sb.AppendLine(", ");

                sb.AppendFormat(_innerQuery.Dialect.CreateToken(string.Concat(baseTable.Alias, ".", _innerQuery.NameOrAltName(col.ColumnInfo))));
            }

            return sb.ToString();
        }

        private void BuildRowNumberColumn(StringBuilder sql)
        {
            string orderBy = _innerQuery.OrderBy;
            // Remove table prefixes from order columns
            foreach (Table t in _innerQuery.Tables)
            {
                orderBy = orderBy.Replace(string.Format("[{0}].", t.Alias), "");
            }
            
            sql.AppendFormat(", ROW_NUMBER() OVER ({0}) As RowNumber ", orderBy);
        }
    }
}
