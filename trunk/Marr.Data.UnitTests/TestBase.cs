using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Rhino.Mocks;
using Marr.Data.Mapping;
using Marr.Data.UnitTests.Entities;
using Marr.Data.TestHelper;

namespace Marr.Data.UnitTests
{
    public class TestBase
    {
        protected DbCommand _command;
        protected DbConnection _connection;
        protected DbParameterCollection _parameters;

        protected IDataMapper CreateDB_ForQuery(StubResultSet rs)
        {
            return StubDataMapperFactory.CreateForQuery(rs);
        }

        protected IDataMapper CreateDB_ForUpdate()
        {
            return StubDataMapperFactory.CreateForUpdate();
        }

        protected IDataMapper CreateDB_ForInsert()
        {
            return StubDataMapperFactory.CreateForInsert();
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
