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
using System.Reflection;
using Marr.Data.Converters;
using Marr.Data.Reflection;

namespace Marr.Data.Mapping
{
    /// <summary>
    /// Contains information about the class fields and their associated stored proc parameters
    /// </summary>
    public class ColumnMap
    {

        /// <summary>
        /// Creates a column map with an empty ColumnInfo object.
        /// </summary>
        /// <param name="member">The .net member that is being mapped.</param>
        public ColumnMap(MemberInfo member)
            : this(member, new ColumnInfo())
        { }

        public ColumnMap(MemberInfo member, IColumnInfo columnInfo)
        {
            FieldName = member.Name;
            ColumnInfo = columnInfo;

            // If the column name is not specified, the field name will be used.
            if (string.IsNullOrEmpty(columnInfo.Name))
                columnInfo.Name = member.Name;

            FieldType = ReflectionHelper.GetMemberType(member);
            Type paramNetType = FieldType;

            Converter = MapRepository.Instance.GetConverter(FieldType);
            if (Converter != null)
            {
                paramNetType = Converter.DbType;
            }

            DBType = MapRepository.Instance.DbTypeBuilder.GetDbType(paramNetType);

            Getter = MapRepository.Instance.ReflectionStrategy.BuildGetter(member.DeclaringType, FieldName);
            Setter = MapRepository.Instance.ReflectionStrategy.BuildSetter(member.DeclaringType, FieldName);

            if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo pi = (PropertyInfo)member;
                CanRead = pi.CanRead;
                CanWrite = pi.CanWrite;
            }
            else if (member.MemberType == MemberTypes.Field)
            {
                CanRead = true;
                CanWrite = true;
            }
        }

        public string FieldName { get; set; }
        public Type FieldType { get; set; }
        public Enum DBType { get; set; }
        public IColumnInfo ColumnInfo { get; set; }
        
        public IConverter Converter { get; private set; }
        public bool CanRead { get; private set; }
        public bool CanWrite { get; private set; }

        internal GetterDelegate Getter { get; private set; }
        internal SetterDelegate Setter { get; private set; }

        /// <summary>
        /// Gets or sets a function that converts a column value after it is read from the 
        /// datareader and before it is set to the domain object.
        /// NOTE: This property can only be set via the FluentMappings class.
        /// </summary>
        /// <remarks>
        /// DB -> [FromDB conversion function] -> Domain Object
        /// </remarks>
        public Func<object, object> FromDB { get; set; }

        /// <summary>
        /// Gets or sets a function that converts a column value after it is read from the 
        /// domain object and before it is sent to the database.
        /// NOTE: This property can only be set via the FluentMappings class.
        /// </summary>
        /// <remarks>
        /// Domain Object -> [ToDB conversion function] -> DB
        /// </remarks>
        public Func<object, object> ToDB { get; set; }
    }
}
