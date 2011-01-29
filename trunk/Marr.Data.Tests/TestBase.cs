using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Rhino.Mocks;

namespace Marr.Data.Tests
{
    public class TestBase
    {
        protected DbCommand _command;
        protected DbConnection _connection;
        protected DbParameterCollection _parameters;

        protected IDataMapper CreateDB_ForQuery(StubResultSet rs)
        {
            StubDataReader reader = new StubDataReader(rs);

            _parameters = MockRepository.GenerateMock<DbParameterCollection>();

            _command = MockRepository.GenerateMock<DbCommand>();
            _command.Expect(c => c.ExecuteReader()).Return(reader);
            _command.Expect(c => c.Parameters).Return(_parameters);
            _command.Stub(c => c.CommandText);

            _connection = MockRepository.GenerateMock<DbConnection>();
            _connection.Expect(c => c.CreateCommand()).Return(_command);

            _command.Expect(c => c.Connection).Return(_connection);

            DbProviderFactory dbFactory = MockRepository.GenerateMock<DbProviderFactory>();
            dbFactory.Expect(f => f.CreateConnection()).Return(_connection);

            return new DataMapper(dbFactory, "connString...");
        }

        protected IDataMapper CreateDB_ForUpdate()
        {
            _parameters = MockRepository.GenerateMock<DbParameterCollection>();

            _command = MockRepository.GenerateMock<DbCommand>();
            _command.Expect(c => c.Parameters).Return(_parameters);
            _command.Stub(c => c.CommandText);
            _command.Expect(c => c.ExecuteNonQuery()).Return(1);
            _command
                .Expect(c => c.CreateParameter())
                .Repeat.Any()
                .Return(MockRepository.GenerateStub<DbParameter>());

            _connection = MockRepository.GenerateMock<DbConnection>();
            _connection.Expect(c => c.CreateCommand()).Return(_command);

            _command.Expect(c => c.Connection).Return(_connection);

            DbProviderFactory dbFactory = MockRepository.GenerateMock<DbProviderFactory>();
            dbFactory.Expect(f => f.CreateConnection()).Return(_connection);

            return new DataMapper(dbFactory, "connString...");
        }

        protected IDataMapper CreateDB_ForInsert()
        {
            _parameters = MockRepository.GenerateMock<DbParameterCollection>();

            _command = MockRepository.GenerateMock<DbCommand>();
            _command.Expect(c => c.Parameters).Return(_parameters);
            _command.Stub(c => c.CommandText);
            _command
                .Expect(c => c.CreateParameter())
                .Repeat.Any()
                .Return(MockRepository.GenerateStub<DbParameter>());

            _connection = MockRepository.GenerateMock<DbConnection>();
            _connection.Expect(c => c.CreateCommand()).Return(_command);

            _command.Expect(c => c.Connection).Return(_connection);

            DbProviderFactory dbFactory = MockRepository.GenerateMock<DbProviderFactory>();
            dbFactory.Expect(f => f.CreateConnection()).Return(_connection);

            return new DataMapper(dbFactory, "connString...");
        }
    }
}
