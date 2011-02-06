using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marr.Data.Mapping;
using System.Data.Common;

namespace Marr.Data.QGen
{
    public class SqlServerDeleteQuery : IQuery
    {
        private string _target;
        private string _whereClause;

        public SqlServerDeleteQuery(string target, string whereClause)
        {
            _target = target;
            _whereClause = whereClause;
        }

        public string Generate()
        {
            return string.Format("DELETE FROM {0} {1} ", _target, _whereClause);
        }
    }
}
