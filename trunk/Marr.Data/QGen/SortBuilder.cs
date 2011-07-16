using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Marr.Data.QGen.Dialects;

namespace Marr.Data.QGen
{
    /// <summary>
    /// This class is responsible for creating an "ORDER BY" clause.
    /// It uses chaining methods to provide a fluent interface.
    /// It also has some methods that coincide with Linq methods, to provide Linq compatibility.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SortBuilder<T> : IEnumerable<T>, IQueryBuilder
    {
        private string _constantOrderByClause;
        private QueryBuilder<T> _baseBuilder;
        private Dialect _dialect;
        private List<SortColumn<T>> _sortExpressions;
        private bool _useAltName;
        private TableCollection _tables;

        public SortBuilder(QueryBuilder<T> baseBuilder, Dialect dialect, TableCollection tables, bool useAltName)
        {
            _baseBuilder = baseBuilder;
            _dialect = dialect;
            _sortExpressions = new List<SortColumn<T>>();
            _useAltName = useAltName;
            _tables = tables;
        }

        internal SortBuilder<T> Order(MemberInfo member)
        {
            _sortExpressions.Add(new SortColumn<T>(member, SortDirection.Asc));
            return this;
        }

        internal SortBuilder<T> OrderByDescending(MemberInfo member)
        {
            _sortExpressions.Add(new SortColumn<T>(member, SortDirection.Desc));
            return this;
        }

        public SortBuilder<T> OrderBy(string orderByClause)
        {
            if (string.IsNullOrEmpty(orderByClause))
                throw new ArgumentNullException("orderByClause");

            if (!orderByClause.ToUpper().Contains("ORDER BY "))
            {
                orderByClause = orderByClause.Insert(0, " ORDER BY ");
            }

            _constantOrderByClause = orderByClause;
            return this;
        }

        public SortBuilder<T> OrderBy(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Asc));
            return this;
        }

        public SortBuilder<T> OrderByDescending(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Desc));
            return this;
        }

        public SortBuilder<T> ThenBy(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Asc));
            return this;
        }

        public SortBuilder<T> ThenByDescending(Expression<Func<T, object>> sortExpression)
        {
            _sortExpressions.Add(new SortColumn<T>(sortExpression, SortDirection.Desc));
            return this;
        }

        public SortBuilder<T> Take(int count)
        {
            _baseBuilder.Take(count);
            return this;
        }

        public SortBuilder<T> Skip(int count)
        {
            _baseBuilder.Skip(count);
            return this;
        }

        public SortBuilder<T> Page(int pageNumber, int pageSize)
        {
            _baseBuilder.Page(pageNumber, pageSize);
            return this;
        }

        public int GetRowCount()
        {
            return _baseBuilder.GetRowCount();
        }

        public List<T> ToList()
        {
            return _baseBuilder.ToList();
        }

        public string BuildQuery()
        {
            return _baseBuilder.BuildQuery();
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_constantOrderByClause))
            {
                return _constantOrderByClause;
            }

            StringBuilder sb = new StringBuilder();

            foreach (var sort in _sortExpressions)
            {
                if (sb.Length > 0)
                    sb.Append(",");

                Table table = _tables.FindTable(sort.Member);

                if (table == null)
                {
                    string msg = string.Format("The property '{0} -> {1}' you are trying to reference in the 'ORDER BY' statement belongs to an entity that has not been joined in your query.  To reference this property, you must join the '{0}' entity using the Join method.",
                        sort.Member.DeclaringType.Name,
                        sort.Member.Name);

                    throw new DataMappingException(msg);
                }

                string columnName = sort.Member.GetColumnName(_useAltName);
                sb.Append(_dialect.CreateToken(string.Format("{0}.{1}", table.Alias, columnName)));

                if (sort.Direction == SortDirection.Desc)
                    sb.Append(" DESC");
            }

            if (sb.Length > 0)
                sb.Insert(0, " ORDER BY ");

            return sb.ToString();
        }

        public static implicit operator List<T>(SortBuilder<T> builder)
        {
            return builder.ToList();
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            var list = this.ToList();
            return list.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
