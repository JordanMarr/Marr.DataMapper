using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using System.Data.Common;

namespace Marr.Data.QGen
{
    public class DeleteQuery : IQuery
    {
        protected string Target { get; set; }
        protected string WhereClause { get; set; }

        public DeleteQuery(string target, string whereClause)
        {
            Target = target;
            WhereClause = whereClause;
        }

        public string Generate()
        {
            return string.Format("DELETE FROM {0} {1} ", Target, WhereClause);
        }
    }
}
