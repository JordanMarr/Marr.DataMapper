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
using System.Text;
using System.Data;
using System.Text.RegularExpressions;
using System.Data.Common;

namespace Marr.Data.Mapping
{
    public class ColumnMapCollection : List<ColumnMap>, IEnumerable<ColumnMap>
    {
        #region - Filters -

        /// <summary>
        /// Gets a ColumnMap by its field name.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public ColumnMap this[string fieldName]
        {
            get
            {
                return this.Find(m => m.ColumnInfo.Name == fieldName);
            }
        }

        /// <summary>
        /// Iterates through all fields marked as return values.
        /// </summary>
        public IEnumerable<ColumnMap> ReturnValues
        {
            get
            {
                foreach (ColumnMap map in this)
                    if (map.ColumnInfo.ReturnValue)
                        yield return map;
            }
        }

        /// <summary>
        /// Iterates through all fields that are not return values.
        /// </summary>
        public ColumnMapCollection NonReturnValues
        {
            get
            {
                ColumnMapCollection collection = new ColumnMapCollection();

                foreach (ColumnMap map in this)
                    if (!map.ColumnInfo.ReturnValue)
                        collection.Add(map);

                return collection;
            }
        }

        /// <summary>
        /// Iterates through all fields marked as Output parameters or InputOutput.
        /// </summary>
        public IEnumerable<ColumnMap> OutputFields
        {
            get
            {
                foreach (ColumnMap map in this)
                    if (map.ColumnInfo.ParamDirection == ParameterDirection.InputOutput ||
                        map.ColumnInfo.ParamDirection == ParameterDirection.Output)
                        yield return map;
            }
        }

        /// <summary>
        /// Iterates through all fields marked as primary keys.
        /// </summary>
        public ColumnMapCollection PrimaryKeys
        {
            get
            {
                ColumnMapCollection keys = new ColumnMapCollection();
                foreach (ColumnMap map in this)
                    if (map.ColumnInfo.IsPrimaryKey)
                        keys.Add(map);

                return keys;
            }
        }

        /// <summary>
        /// Parses and orders the parameters from the query text.  
        /// Filters the list of mapped columns to match the parameters found in the sql query.
        /// All parameters starting with the '@' or ':' symbol are matched and returned.
        /// </summary>
        /// <param name="command">The command and parameters that are being parsed.</param>
        /// <returns>A list of mapped columns that are present in the sql statement as parameters.</returns>
        public ColumnMapCollection OrderParameters(DbCommand command)
        {
            if (command.CommandType == CommandType.Text && this.Count > 0)
            {
                string commandTypeString = command.GetType().ToString();
                if (commandTypeString.Contains("Oracle") || commandTypeString.Contains("OleDb"))
                {
                    ColumnMapCollection columns = new ColumnMapCollection();

                    // Find all @Parameters contained in the sql statement
                    string paramPrefix = commandTypeString.Contains("Oracle") ? ":" : "@";
                    string regexString = string.Format(@"{0}[\w-]+", paramPrefix);
                    Regex regex = new Regex(regexString);
                    foreach (Match m in regex.Matches(command.CommandText))
                    {
                        ColumnMap matchingColumn = this.Find(c => string.Concat(paramPrefix, c.ColumnInfo.Name.ToLower()) == m.Value.ToLower());
                        if (matchingColumn != null)
                            columns.Add(matchingColumn);
                    }

                    return columns;
                }
            }

            return this;
        }


        #endregion

        #region - Fluent Methods -

        public ColumnMapCollection SetPrimaryKey(string propertyName)
        {
            this[propertyName].ColumnInfo.IsPrimaryKey = true;
            return this;
        }

        public ColumnMapCollection SetAutoIncrement(string propertyName)
        {
            this[propertyName].ColumnInfo.IsAutoIncrement = true;
            return this;
        }

        public ColumnMapCollection SetColumnName(string propertyName, string columnName)
        {
            this[propertyName].ColumnInfo.Name = columnName;
            return this;
        }

        public ColumnMapCollection SetReturnValue(string propertyName)
        {
            this[propertyName].ColumnInfo.ReturnValue = true;
            return this;
        }

        public ColumnMapCollection SetSize(string propertyName, int size)
        {
            this[propertyName].ColumnInfo.Size = size;
            return this;
        }

        public ColumnMapCollection SetAltName(string propertyName, string altName)
        {
            this[propertyName].ColumnInfo.AltName = altName;
            return this;
        }

        public ColumnMapCollection SetParamDirection(string propertyName, ParameterDirection direction)
        {
            this[propertyName].ColumnInfo.ParamDirection = direction;
            return this;
        }

        public ColumnMapCollection RemoveColumnMap(string propertyName)
        {
            var columnMap = this[propertyName];
            this.Remove(columnMap);
            return this;
        }

        #endregion
    }
}
