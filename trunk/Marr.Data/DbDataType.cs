/*  Copyright (C) 2008 - 2011 Jordan Marr

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

namespace Marr.Data
{
    public enum DbDataType
    {
        AutoDetect,

        DbType_String,
        DbType_Int32,
        DbType_Decimal,
        DbType_DateTime,
        DbType_Boolean,
        DbType_Int16,
        DbType_Single,
        DbType_Int64,
        DbType_Double,
        DbType_Byte,
        DbType_Binary,
        DbType_Guid,
        DbType_Object,

        SqlDbType_VarChar,
        SqlDbType_Int,
        SqlDbType_Decimal,
        SqlDbType_DateTime,
        SqlDbType_Bit,
        SqlDbType_SmallInt,
        SqlDbType_BigInt,
        SqlDbType_Float,
        SqlDbType_Binary,
        SqlDbType_VarBinary,
        SqlDbType_UniqueIdentifier,
        SqlDbType_Variant,

        OracleDbType_Varchar2,
        OracleDbType_Int32,
        OracleDbType_TimeStamp,
        OracleDbType_Decimal,
        OracleDbType_Double,
        OracleDbType_Single,
        OracleDbType_Int16,
        OracleDbType_Int64,
        OracleDbType_Byte,
        OracleDbType_Raw,
        OracleDbType_IntervalDS,
        OracleDbType_Object,
        
        OleDb_VarChar,
        OleDb_Integer,
        OleDb_Decimal,
        OleDb_DBTimeStamp,
        OleDb_Boolean,
        OleDb_SmallInt,
        OleDb_BigInt,
        OleDb_Double,
        OleDb_Binary,
        OleDb_VarBinary,
        OleDb_Guid,
        OleDb_Variant
    }
}
