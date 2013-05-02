using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Rhino.Mocks;
using Marr.Data.UnitTests.Entities;
using System.Data;
using Marr.Data.Parameters;

namespace Marr.Data.UnitTests
{
    [TestClass]
    public class ParameterTest : TestBase
    {
        [TestInitialize]
        public void Init()
        {
            ResetMapRepository();
        }

        #region - AddParameter Tests -

        [TestMethod]
        public void AddParameter_Null_Should_Convert_To_DBNull_Value()
        {
            // Arrange
            IDataMapper db = CreateDataMapper();
            DbParameter parameter = MockRepository.GenerateStub<DbParameter>();

            _command
                .Expect(c => c.CreateParameter())
                .Return(parameter);

            DbParameterCollection parameters = MockRepository.GenerateStub<DbParameterCollection>();

            _command
                .Expect(c => c.Parameters)
                .Return(parameters);

            // Act
            ParameterChainMethods paramChain = db.AddParameter("Test", null);

            // Assert
            Assert.AreEqual("Test", paramChain.Parameter.ParameterName);
            Assert.AreEqual(DBNull.Value, paramChain.Parameter.Value);
        }

        [TestMethod]
        public void AddParameter_Should_Use_Registered_IConverter()
        {
            // Arrange
            IDataMapper db = CreateDataMapper();
            DbParameter parameter = MockRepository.GenerateStub<DbParameter>();
            _command
                .Expect(c => c.CreateParameter())
                .Return(parameter);

            DbParameterCollection parameters = MockRepository.GenerateStub<DbParameterCollection>();

            // Register a BooleanYNConverter
            MapRepository.Instance.RegisterTypeConverter(typeof(bool), new Converters.BooleanYNConverter());

            _command
                .Expect(c => c.Parameters)
                .Return(parameters);

            // Act
            ParameterChainMethods paramChain = db.AddParameter("Flag", true);

            // Assert
            Assert.AreEqual("Flag", paramChain.Parameter.ParameterName);
            Assert.AreEqual("Y", paramChain.Parameter.Value);
        }

        [TestMethod]
        public void AddParameter_When_No_IConverter_Is_Registered_Should_Use_Assign_Straight_Value()
        {
            // Arrange
            IDataMapper db = CreateDataMapper();
            DbParameter parameter = MockRepository.GenerateStub<DbParameter>();
            _command
                .Expect(c => c.CreateParameter())
                .Return(parameter);

            DbParameterCollection parameters = MockRepository.GenerateStub<DbParameterCollection>();

            _command
                .Expect(c => c.Parameters)
                .Return(parameters);

            // Act
            ParameterChainMethods paramChain = db.AddParameter("Flag", true);

            // Assert
            Assert.AreEqual("Flag", paramChain.Parameter.ParameterName);
            Assert.AreEqual(true, paramChain.Parameter.Value);
        }

        private enum GenderType
        {
            Male,
            Female
        }

        [TestMethod]
        public void AddParameter_When_Enum_Converter_Is_Registered_Should_Use_For_Any_Enums()
        {
            // Arrange
            IDataMapper db = CreateDataMapper();
            DbParameter parameter = MockRepository.GenerateStub<DbParameter>();
            _command
                .Expect(c => c.CreateParameter())
                .Return(parameter);

            DbParameterCollection parameters = MockRepository.GenerateStub<DbParameterCollection>();

            _command
                .Expect(c => c.Parameters)
                .Return(parameters);

            // Register a BooleanYNConverter
            MapRepository.Instance.RegisterTypeConverter(typeof(Enum), new Converters.EnumStringConverter());

            // Act
            ParameterChainMethods paramChain = db.AddParameter("GenderType", GenderType.Male);

            // Assert
            Assert.AreEqual("Male", paramChain.Parameter.Value);
            Assert.IsInstanceOfType(paramChain.Parameter.Value, typeof(string));
        }

        #endregion

        #region - AddParameter with default DbTypeBuilder -

        [TestMethod]
        public void AddParameter_DbType_Should_Be_String()
        {
            SqlParamChecker(DbType.String, "Value");
        }

        [TestMethod]
        public void AddParameter_DbType_Should_Be_Decimal()
        {
            SqlParamChecker(DbType.Decimal, 1.1m);
        }

        [TestMethod]
        public void AddParameter_DbType_Should_Be_Bit()
        {
            SqlParamChecker(DbType.Boolean, true);
        }

        [TestMethod]
        public void AddParameter_DbType_Should_Be_Int()
        {
            SqlParamChecker(DbType.Int32, 1);
        }

        private void SqlParamChecker<T>(DbType expectedDbType, T inputValue)
        {
            // Arrange
            IDataMapper db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=myServerAddress;Initial Catalog=myDataBase;User Id=myUsername;Password=myPassword;");

            // Use the DbTypeBuilder (default)
            //MapRepository.Instance.DbTypeBuilder = new DbTypeBuilder();

            // Act
            var dbParam = db.AddParameter("P1", inputValue).Parameter as System.Data.SqlClient.SqlParameter;

            // Assert
            Assert.IsNotNull(dbParam, "The param should be of type SqlParameter.");
            Assert.IsNotNull(dbParam.DbType, "DataMappper should set the DbType property.");
            Assert.AreEqual(expectedDbType, dbParam.DbType);
        }

        #endregion

        #region - AddParameter with SqlDbTypeBuilder -

        [TestMethod]
        public void AddParameter_SqlDbType_Should_Be_Varchar()
        {
            SqlParamChecker(SqlDbType.NVarChar, "Value");
        }

        [TestMethod]
        public void AddParameter_SqlDbType_Should_Be_Decimal()
        {
            SqlParamChecker(SqlDbType.Decimal, 1.1m);
        }

        [TestMethod]
        public void AddParameter_SqlDbType_Should_Be_Bit()
        {
            SqlParamChecker(SqlDbType.Bit, true);
        }

        [TestMethod]
        public void AddParameter_SqlDbType_Should_Be_Int()
        {
            SqlParamChecker(SqlDbType.Int, 1);
        }

        private void SqlParamChecker<T>(SqlDbType expectedDbType, T inputValue)
        {
            // Arrange
            IDataMapper db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=myServerAddress;Initial Catalog=myDataBase;User Id=myUsername;Password=myPassword;");
            
            // Specify that the SqlDbTypeBuilder is used
            MapRepository.Instance.DbTypeBuilder = new SqlDbTypeBuilder();

            // Act
            var sqlParam = db.AddParameter("P1", inputValue).Parameter as System.Data.SqlClient.SqlParameter;

            // Assert
            Assert.IsNotNull(sqlParam, "The param should be of type SqlParameter.");
            Assert.IsNotNull(sqlParam.SqlDbType, "DataMappper should set the SqlDbType property.");
            Assert.AreEqual(expectedDbType, sqlParam.SqlDbType);
        }

        #endregion
    }
}
