/*  Copyright (C) 2008 - 2010 Jordan Marr

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
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Data.Common;

namespace Marr.Data.Mapping
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ColumnAttribute : Attribute, IColumnInfo
    {
        private string _name;
        private ConverterType _converter = ConverterType.None;

        private int _size = 0;
        private bool _isPrimaryKey;
        private bool _isAutoIncrement;
        private bool _returnValue;
        private ParameterDirection _paramDirection = ParameterDirection.Input;

        public ColumnAttribute()
        {
        }

        public ColumnAttribute(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Gets or sets the column name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets the column size.
        /// </summary>
        public int Size
        {
            get { return _size; }
            set { _size = value; }
        }

        /// <summary>
        /// Gets or sets a value that determines whether the column is the Primary Key.
        /// </summary>
        public bool IsPrimaryKey
        {
            get { return _isPrimaryKey; }
            set { _isPrimaryKey = value; }
        }

        /// <summary>
        /// Gets or sets a value that determines whether the column is an auto-incrementing seed column.
        /// </summary>
        public bool IsAutoIncrement
        {
            get { return _isAutoIncrement; }
            set { _isAutoIncrement = value; }
        }

        /// <summary>
        /// Gets or sets a value that determines whether the column has a return value.
        /// </summary>
        public bool ReturnValue
        {
            get { return _returnValue; }
            set { _returnValue = value; }
        }

        /// <summary>
        /// Gets or sets the ParameterDirection.
        /// </summary>
        public ParameterDirection ParamDirection
        {
            get { return _paramDirection; }
            set { _paramDirection = value; }
        }

        /// <summary>
        /// Determines which data converter (if any) should be used on this value.
        /// </summary>
        public ConverterType Converter
        {
            get { return _converter; }
            set { _converter = value; }
        }
    }
}
