using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Rhino.Mocks;

namespace Marr.Data.TestHelper
{
    public class StubDataMapper : DataMapper
    {
        protected DbCommand _command;
        protected DbConnection _connection;
        protected DbParameterCollection _parameters;

        internal StubDataMapper(DbProviderFactory dbfactory, DbCommand command, DbConnection connection, DbParameterCollection parameters)
            : base(dbfactory, "connection string") 
        {
            _command = command;
            _connection = connection;
            _parameters = parameters;
        }
    }
}
