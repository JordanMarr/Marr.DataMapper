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
using System.Reflection;
/* * 
 * The FastReflection library was written by Renaud Bédard (renaud.bedard@gmail.com)
 * http://theinstructionlimit.com/?p=76
 * */
using FastReflection;

namespace Marr.Data
{
    public class ReflectionHelper
    {
        /// <summary>
        /// Sets an entity field value by name to the passed in 'val'.
        /// </summary>
        public static void SetFieldValue<T>(T entity, string fieldName, object val)
        {
            CachedReflector reflector = MapRepository.Instance.Reflector;
            MemberInfo member = entity.GetType().GetMember(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
            
            try
            {
                // Handle DB null values
                if (val == DBNull.Value)
                {
                    if (member.MemberType == MemberTypes.Field)
                        reflector.SetValue(member, entity, GetDefaultValue((member as FieldInfo).FieldType));
                    else if (member.MemberType == MemberTypes.Property)
                    {
                        var pi = (member as PropertyInfo);
                        if (pi.CanWrite)
                            reflector.SetValue(member, entity, GetDefaultValue((member as PropertyInfo).PropertyType));
                    }
                }
                else
                {
                    if (member.MemberType == MemberTypes.Field)
                        reflector.SetValue(member, entity, val);
                    else if (member.MemberType == MemberTypes.Property)
                    {
                        var pi = (member as PropertyInfo);
                        if (pi.CanWrite)
                            reflector.SetValue(member, entity, val);
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("The DataMapper was unable to load the following field: {0}.  \nDetails: {1}", fieldName, ex.Message);
                throw new Exception(msg, ex);
            }
        }

        /// <summary>
        /// Gets an entity field value by name.
        /// </summary>
        public static object GetFieldValue(object entity, string fieldName)
        {
            CachedReflector reflector = MapRepository.Instance.Reflector;
            MemberInfo member = entity.GetType().GetMember(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];

            if (member.MemberType == MemberTypes.Field)
            {
                return reflector.GetValue(member, entity);
            }
            else if (member.MemberType == MemberTypes.Property)
            {
                if ((member as PropertyInfo).CanRead)
                    return reflector.GetValue(member, entity);
            }

            throw new Exception(string.Format("The DataMapper could not get the value for {0}.{1}.", entity.GetType().Name, fieldName));
        }

        /// <summary>
        /// Converts a DBNull.Value to a null for a reference field,
        /// or the default value of a value field.
        /// </summary>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        public static object GetDefaultValue(Type fieldType)
        {
            if (fieldType.IsGenericType)
            {
                return null;
            }
            else if (fieldType.IsValueType)
            {
                return CreateInstance(fieldType);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Instantiantes a type using the FastReflector library for increased speed.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object CreateInstance(Type type)
        {
            CachedReflector cachedReflector = MapRepository.Instance.Reflector;
            return cachedReflector.Instantiate(type);
        }

        /// <summary>
        /// Gets the CLR data type of a MemberInfo.  
        /// If the type is nullable, returns the underlying type.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static Type GetMemberType(MemberInfo member)
        {
            Type memberType = null;
            if (member.MemberType == MemberTypes.Property)
                memberType = (member as PropertyInfo).PropertyType;
            else if (member.MemberType == MemberTypes.Field)
                memberType = (member as FieldInfo).FieldType;
            else
                memberType = typeof(object);

            // Handle nullable types - get underlying type
            if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                memberType = memberType.GetGenericArguments()[0];
            }

            return memberType;
        }

    }
}
