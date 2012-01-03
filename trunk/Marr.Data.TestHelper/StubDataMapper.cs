/*  Copyright (C) 2008 - 2012 Jordan Marr

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>. */

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
