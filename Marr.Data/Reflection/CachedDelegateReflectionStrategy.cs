using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Marr.Data.Reflection
{
    public class CachedDelegateReflectionStrategy : IReflectionStrategy
    {

        private static readonly Dictionary<string, MemberInfo> MemberCache = new Dictionary<string, MemberInfo>();
        private static readonly Dictionary<string, GetterDelegate> GetterCache = new Dictionary<string, GetterDelegate>();
        private static readonly Dictionary<string, SetterDelegate> SetterCache = new Dictionary<string, SetterDelegate>();

        private static MemberInfo GetMember(Type entityType, string name)
        {
            MemberInfo member;
            var key = entityType.FullName + name;
            if (!MemberCache.TryGetValue(key, out member))
            {
                member = entityType.GetMember(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
                MemberCache[key] = member;
            }

            return member;
        }

        /// <summary>
        /// Gets an entity field value by name.
        /// </summary>
        public object GetFieldValue(object entity, string fieldName)
        {
            var member = GetMember(entity.GetType(), fieldName);

            if (member.MemberType == MemberTypes.Field)
            {
                return (member as FieldInfo).GetValue(entity);
            }
            if (member.MemberType == MemberTypes.Property)
            {
                return BuildGetter(entity.GetType(), fieldName)(entity);
            }
            throw new DataMappingException(string.Format("The DataMapper could not get the value for {0}.{1}.", entity.GetType().Name, fieldName));
        }

        public object CreateInstance(Type type)
        {
			// If type is an interface and is IEnumerable, then create a List<T>
			if (type.IsInterface && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
			{
				if (!type.IsGenericType)
					throw new NotSupportedException("Non-generic IEnumerable relationship types are not supported. Please use a generic instead.");

				Type genericList = typeof(List<>);
				Type[] typeArgs = type.GetGenericArguments();
				Type listT = genericList.MakeGenericType(typeArgs);
				return Activator.CreateInstance(listT);
			}

            return Activator.CreateInstance(type);
        }

        public GetterDelegate BuildGetter(Type type, string memberName)
        {
            GetterDelegate getter;
            var key = type.FullName + memberName;
            if (!GetterCache.TryGetValue(key, out getter))
            {
                getter = GetPropertyGetter(GetMember(type, memberName));
            }

            return getter;
        }

        public SetterDelegate BuildSetter(Type type, string memberName)
        {
            SetterDelegate setter;
            var key = type.FullName + memberName;
            if (!SetterCache.TryGetValue(key, out setter))
            {
                setter = GetPropertySetter(GetMember(type, memberName));
            }

            return setter;
        }

        private static SetterDelegate GetPropertySetter(MemberInfo memberInfo)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Property:
                {
                    var prop = (PropertyInfo)memberInfo;

                    if (!prop.CanWrite)
                        return null;

#if NO_EXPRESSIONS
                    return (o, convertedValue) =>
                    {
                        propertySetMethod.Invoke(o, new[] { convertedValue });
                        return;
                    };
#else
                    var instance = Expression.Parameter(typeof(object), "i");
                    var argument = Expression.Parameter(typeof(object), "a");

                    var instanceParam = Expression.Convert(instance, prop.DeclaringType);
                    var valueParam = Expression.Convert(argument, prop.PropertyType);

                    var setterCall = Expression.Call(instanceParam, prop.GetSetMethod(true), valueParam);

                    return Expression.Lambda<SetterDelegate>(setterCall, instance, argument).Compile();
#endif
                }
                case MemberTypes.Field:
                {
                    return ((instance, value) => ((FieldInfo)memberInfo).SetValue(instance, value));
                }
                default:
                {
                    throw new ArgumentException("Member needs to be a property or field: " + memberInfo.Name);
                }
            }
        }

        private static GetterDelegate GetPropertyGetter(MemberInfo memberInfo)
        {

            switch (memberInfo.MemberType)
            {
                case MemberTypes.Property:
                {

                    var prop = (PropertyInfo)memberInfo;

                    if (!prop.CanRead)
                        return null;

                    var getMethodInfo = (prop).GetGetMethod(true);

#if NO_EXPRESSIONS
			return o => propertyInfo.GetGetMethod().Invoke(o, new object[] { });
#else

                    var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
                    var instanceParam = Expression.Convert(oInstanceParam, memberInfo.DeclaringType);

                    var exprCallPropertyGetFn = Expression.Call(instanceParam, getMethodInfo);
                    var oExprCallPropertyGetFn = Expression.Convert(exprCallPropertyGetFn, typeof(object));

                    var propertyGetFn = Expression.Lambda<GetterDelegate>
                    (
                        oExprCallPropertyGetFn,
                        oInstanceParam
                    ).Compile();

                    return propertyGetFn;
                }
                case MemberTypes.Field:
                {
                    return (instance => ((FieldInfo)memberInfo).GetValue(instance));
                }
                default:
                {
					throw new ArgumentException("Member needs to be a property or field: " + memberInfo.Name);
                }
            }
#endif
        }
    }
}
