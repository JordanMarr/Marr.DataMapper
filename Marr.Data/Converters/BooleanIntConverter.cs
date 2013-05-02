﻿/*  Copyright (C) 2008 - 2011 Jordan Marr

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
using Marr.Data.Mapping;

namespace Marr.Data.Converters
{
    public class BooleanIntConverter : IConverter
    {
        public object FromDB(ColumnMap map, object dbValue)
        {
            if (dbValue == DBNull.Value)
            {
                return DBNull.Value;
            }

            int val = (int)dbValue;

            if (val == 1)
            {
                return true;
            }
            else if (val == 0)
            {
                return false;
            }
            else
            {
                throw new ConversionException(
                    string.Format(
                    "The BooleanCharConverter could not convert the value '{0}' to a boolean.",
                    dbValue));
            }
        }

        public object ToDB(object clrValue)
        {
            bool? val = (bool?)clrValue;

            if (val == true)
            {
                return 1;
            }
            else if (val == false)
            {
                return 0;
            }
            else
            {
                return DBNull.Value;
            }
        }

        public Type DbType
        {
            get
            {
                return typeof(int);
            }
        }
    }
}
