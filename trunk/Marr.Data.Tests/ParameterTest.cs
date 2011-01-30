using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Rhino.Mocks;
using Marr.Data.Tests.Entities;
using System.Data;
using Marr.Data.Parameters;

namespace Marr.Data.Tests
{
    [TestClass]
    public class ParameterTest : TestBase
    {
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
        public void AddParameter_When_No_IConverter_Is_Registerd_Should_Use_Assign_Straight_Value()
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


    }
}
