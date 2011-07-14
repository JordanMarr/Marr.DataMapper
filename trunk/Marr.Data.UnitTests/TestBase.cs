using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Rhino.Mocks;
using Marr.Data.Mapping;
using Marr.Data.UnitTests.Entities;

namespace Marr.Data.UnitTests
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
            _parameters.Expect(p => p.Add(null)).Return(1).IgnoreArguments();

            _command = MockRepository.GenerateMock<DbCommand>();
            _command.Expect(c => c.ExecuteReader()).Return(reader);
            
            _command.Expect(c => c.Parameters).Return(_parameters);
            _command.Expect(c => c.CreateParameter()).Return(new System.Data.SqlClient.SqlParameter()).Repeat.Any();
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

        protected IDataMapper CreateDataMapper()
        {
            _command = MockRepository.GenerateMock<DbCommand>();

            _connection = MockRepository.GenerateMock<DbConnection>();
            _connection.Expect(c => c.CreateCommand()).Return(_command);

            DbProviderFactory dbFactory = MockRepository.GenerateMock<DbProviderFactory>();
            dbFactory.Expect(f => f.CreateConnection()).Return(_connection);

            return new DataMapper(dbFactory, "connString...");
        }

        protected void InitMappings()
        {
            MapBuilder builder = new MapBuilder();

            builder.BuildTable<Person>("PersonTable");

            builder.BuildColumns<Person>()
                .SetReturnValue("ID")
                .SetPrimaryKey("ID")
                .SetAutoIncrement("ID");

            builder.BuildRelationships<Person>();

            builder.BuildColumns<Pet>()
                .SetPrimaryKey("ID")
                .SetAltName("ID", "Pet_ID")
                .SetAltName("Name", "Pet_Name");
        }

    }
}
