using Marr.Data.Mapping;
using Marr.Data.TestHelper;
using Marr.Data.UnitTests.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data.UnitTests
{
	#region - Test Entity -

	public class ImplicitProperties
	{
		public decimal DecimalValue { get; set; }
		public double DoubleValue { get; set; }
		public Single SingleValue { get; set; }
		public long LongValue { get; set; }
		public int IntValue { get; set; }
		public short ShortValue { get; set; }
		public Byte ByteValue { get; set; }
	}

	#endregion

	[TestClass]
	public class ImplicitLoadingTests : TestBase
	{
		private StubResultSet _rs;

		[TestInitialize]
		public void Init()
		{
			// Register type converters first
			MapRepository.Instance.RegisterTypeConverter(typeof(decimal), new Converters.CastConverter<decimal, IConvertible>());
			MapRepository.Instance.RegisterTypeConverter(typeof(double), new Converters.CastConverter<double, IConvertible>());
			MapRepository.Instance.RegisterTypeConverter(typeof(Single), new Converters.CastConverter<Single, IConvertible>());
			MapRepository.Instance.RegisterTypeConverter(typeof(long), new Converters.CastConverter<long, IConvertible>());
			MapRepository.Instance.RegisterTypeConverter(typeof(int), new Converters.CastConverter<int, IConvertible>());
			MapRepository.Instance.RegisterTypeConverter(typeof(short), new Converters.CastConverter<short, IConvertible>());
			MapRepository.Instance.RegisterTypeConverter(typeof(Byte), new Converters.CastConverter<Byte, IConvertible>());

			new FluentMappings()
				.Entity<ImplicitProperties>()
					.Columns.AutoMapAllProperties();

			_rs = new StubResultSet("DecimalValue", "DoubleValue", "SingleValue", "LongValue", "IntValue", "ShortValue", "ByteValue");
		}

		[TestMethod]
		public void NoCastsRequired_ShouldPassWithoutConverter()
		{
			// Arrange
			_rs.AddRow((decimal)1, (double)1, (Single)1, (long)1, (int)1, (short)1, (Byte)1);

			// Act
			var db = CreateDB_ForQuery(_rs);
			var implicitProperties = db.Find<ImplicitProperties>("sql...");

			// Assert
			Assert.IsNotNull(implicitProperties);
		}

		[TestMethod]
		public void DecimalProperty_ShouldAcceptImplicitValues()
		{
			// Arrange
			object[] values = new object[] { (decimal)1, (double)1, (Single)1, (long)1, (short)1, (Byte)1 };
			foreach (object value in values)
			{
				_rs.AddRow(value, (double)1, (Single)1, (long)1, (int)1, (short)1, (Byte)1);
			}

			// Act
			var db = CreateDB_ForQuery(_rs);
			var implicitProperties = db.Query<ImplicitProperties>("sql...");

			// Assert
			Assert.IsNotNull(implicitProperties);
		}

		[TestMethod]
		public void DoubleProperty_ShouldAcceptImplicitValues()
		{
			// Arrange
			object[] values = new object[] { (double)1, (Single)1, (long)1, (short)1, (Byte)1 };
			foreach (object value in values)
			{
				_rs.AddRow((decimal)1, value, (Single)1, (long)1, (int)1, (short)1, (Byte)1);
			}

			// Act
			var db = CreateDB_ForQuery(_rs);
			var implicitProperties = db.Query<ImplicitProperties>("sql...");

			// Assert
			Assert.IsNotNull(implicitProperties);
		}

		[TestMethod]
		public void SingleProperty_ShouldAcceptImplicitValues()
		{
			// Arrange
			object[] values = new object[] { (double)1, (Single)1, (long)1, (short)1, (Byte)1 };
			foreach (object value in values)
			{
				_rs.AddRow((decimal)1, (double)1, value, (long)1, (int)1, (short)1, (Byte)1);
			}

			// Act
			var db = CreateDB_ForQuery(_rs);
			var implicitProperties = db.Query<ImplicitProperties>("sql...");

			// Assert
			Assert.IsNotNull(implicitProperties);
		}

		[TestMethod]
		public void LongProperty_ShouldAcceptImplicitValues()
		{
			// Arrange
			object[] values = new object[] { (double)1, (Single)1, (long)1, (short)1, (Byte)1 };
			foreach (object value in values)
			{
				_rs.AddRow((decimal)1, (double)1, (Single)1, value, (int)1, (short)1, (Byte)1);
			}

			// Act
			var db = CreateDB_ForQuery(_rs);
			var implicitProperties = db.Query<ImplicitProperties>("sql...");

			// Assert
			Assert.IsNotNull(implicitProperties);
		}

		[TestMethod]
		public void IntProperty_ShouldAcceptImplicitValues()
		{
			// Arrange
			object[] values = new object[] { (double)1, (Single)1, (long)1, (short)1, (Byte)1 };
			foreach (object value in values)
			{
				_rs.AddRow((decimal)1, (double)1, (Single)1, (long)1, value, (short)1, (Byte)1);
			}

			// Act
			var db = CreateDB_ForQuery(_rs);
			var implicitProperties = db.Query<ImplicitProperties>("sql...");

			// Assert
			Assert.IsNotNull(implicitProperties);
		}

		[TestMethod]
		public void ShortProperty_ShouldAcceptImplicitValues()
		{
			// Arrange
			object[] values = new object[] { (double)1, (Single)1, (long)1, (short)1, (Byte)1 };
			foreach (object value in values)
			{
				_rs.AddRow((decimal)1, (double)1, (Single)1, (long)1, (int)1, value, (Byte)1);
			}

			// Act
			var db = CreateDB_ForQuery(_rs);
			var implicitProperties = db.Query<ImplicitProperties>("sql...");

			// Assert
			Assert.IsNotNull(implicitProperties);
		}

		[TestMethod]
		public void ByteProperty_ShouldAcceptImplicitValues()
		{
			// Arrange
			object[] values = new object[] { (double)1, (Single)1, (long)1, (short)1, (Byte)1 };
			foreach (object value in values)
			{
				_rs.AddRow((decimal)1, (double)1, (Single)1, (long)1, (int)1, (short)1, value);
			}

			// Act
			var db = CreateDB_ForQuery(_rs);
			var implicitProperties = db.Query<ImplicitProperties>("sql...");

			// Assert
			Assert.IsNotNull(implicitProperties);
		}
	}
}
