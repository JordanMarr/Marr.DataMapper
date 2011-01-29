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

namespace Marr.Data.Mapping
{
    /// <summary>
    /// Contains information about the class fields and their associated stored proc parameters
    /// </summary>
    public class ColumnMap
    {
        public ColumnMap(string fieldName, Type fieldType, Enum dbType, IColumnInfo columnAttribute)
        {
            FieldName = fieldName;
            FieldType = fieldType;
            DBType = dbType;
            ColumnInfo = columnAttribute;
        }

        public string FieldName { get; set; }
        public Type FieldType { get; set; }
        public Enum DBType { get; set; }
        public IColumnInfo ColumnInfo { get; set; }
    }
}
