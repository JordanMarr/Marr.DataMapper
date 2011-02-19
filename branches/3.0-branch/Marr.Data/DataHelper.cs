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
using System.Data;
using System.Reflection;
using Marr.Data.Mapping;

namespace Marr.Data
{
    internal static class DataHelper
    {
        public static bool HasColumn(this IDataReader dr, string columnName) 
        { 
            for (int i=0; i < dr.FieldCount; i++) 
            { 
                if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase)) 
                    return true; 
            } 
            return false; 
        }

        public static string ParameterPrefix(this IDbCommand command)
        {
            string commandType = command.GetType().Name.ToLower();
            return commandType.Contains("oracle") ? ":" : "@";
        }

        /// <summary>
        /// Returns the ColumnAttribute column name (if attribute exists), or the member name.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static string GetColumnName(this MemberInfo member, bool useAltName)
        {
            // Initialize column name as member name
            string columnName = member.Name;

            // If column name is overridden at ColumnAttribute level, use that name instead
            object[] attributes = member.GetCustomAttributes(typeof(ColumnAttribute), false);
            if (attributes.Length > 0)
            {
                IColumnInfo column = (attributes[0] as ColumnAttribute);
                if (useAltName && !string.IsNullOrEmpty(column.AltName))
                    columnName = column.AltName;
                else if (!string.IsNullOrEmpty(column.Name))
                    columnName = column.Name;
            }

            return columnName;
        }

        public static string GetColumName(this IColumnInfo col, bool useAltName)
        {
            if (useAltName && !string.IsNullOrEmpty(col.AltName))
                return col.AltName;
            else
                return col.Name;
        }

    }
}
