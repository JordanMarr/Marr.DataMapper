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
using Rhino.Mocks;
using System.Data.Common;

namespace Marr.Data.TestHelper
{
    /// <summary>
    /// Initializes a new DataMapper instance with a stubbed result set and mocked internals.
    /// </summary>
    public static class StubDataMapperFactory
    {
        /// <summary>
        /// Creates a DataMapper that can be used to test queries.
        /// </summary>
        /// <param name="rs">The stubbed record set.</param>
        /// <returns>Returns a StubDataMapper.</returns>
        public static IDataMapper CreateForQuery(StubResultSet rs)
        {
            StubDataReader reader = new StubDataReader(rs);

            var parameters = MockRepository.GenerateMock<DbParameterCollection>();
            parameters.Expect(p => p.Add(null)).Return(1).IgnoreArguments();

            var command = MockRepository.GenerateMock<DbCommand>();
            command.Expect(c => c.ExecuteReader()).Return(reader);

            command.Expect(c => c.Parameters).Return(parameters);
            command.Expect(c => c.CreateParameter()).Return(new System.Data.SqlClient.SqlParameter()).Repeat.Any();
            command.Stub(c => c.CommandText);

            var connection = MockRepository.GenerateMock<DbConnection>();
            connection.Expect(c => c.CreateCommand()).Return(command);

            command.Expect(c => c.Connection).Return(connection);

            DbProviderFactory dbFactory = MockRepository.GenerateMock<DbProviderFactory>();
            dbFactory.Expect(f => f.CreateConnection()).Return(connection);

            return new StubDataMapper(dbFactory, command, connection, parameters);
        }

        /// <summary>
        /// Creates a DataMapper that can be used to test updates.
        /// </summary>
        /// <returns>Returns a StubDataMapper.</returns>
        public static IDataMapper CreateForUpdate()
        {
            var parameters = MockRepository.GenerateMock<DbParameterCollection>();

            var command = MockRepository.GenerateMock<DbCommand>();
            command.Expect(c => c.Parameters).Return(parameters);
            command.Stub(c => c.CommandText);
            command.Expect(c => c.ExecuteNonQuery()).Return(1);
            command
                .Expect(c => c.CreateParameter())
                .Repeat.Any()
                .Return(MockRepository.GenerateStub<DbParameter>());

            var connection = MockRepository.GenerateMock<DbConnection>();
            connection.Expect(c => c.CreateCommand()).Return(command);

            command.Expect(c => c.Connection).Return(connection);

            DbProviderFactory dbFactory = MockRepository.GenerateMock<DbProviderFactory>();
            dbFactory.Expect(f => f.CreateConnection()).Return(connection);

            return new StubDataMapper(dbFactory, command, connection, parameters);
        }

        /// <summary>
        /// Creates a DataMapper that can be used to test inserts.
        /// </summary>
        /// <returns>Returns a StubDataMapper.</returns>
        public static IDataMapper CreateForInsert()
        {
            var parameters = MockRepository.GenerateMock<DbParameterCollection>();

            var command = MockRepository.GenerateMock<DbCommand>();
            command.Expect(c => c.Parameters).Return(parameters);
            command.Stub(c => c.CommandText);
            command
                .Expect(c => c.CreateParameter())
                .Repeat.Any()
                .Return(MockRepository.GenerateStub<DbParameter>());

            var connection = MockRepository.GenerateMock<DbConnection>();
            connection.Expect(c => c.CreateCommand()).Return(command);

            command.Expect(c => c.Connection).Return(connection);

            DbProviderFactory dbFactory = MockRepository.GenerateMock<DbProviderFactory>();
            dbFactory.Expect(f => f.CreateConnection()).Return(connection);

            return new StubDataMapper(dbFactory, command, connection, parameters);
        }
    }
}
