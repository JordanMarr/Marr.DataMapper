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
using Marr.Data.Reflection;

namespace Marr.Data.Mapping
{
    public class Relationship
    {

        public Relationship(MemberInfo member)
            : this(member, new RelationshipInfo())
        { }

        public Relationship(MemberInfo member, IRelationshipInfo relationshipInfo)
        {
            Member = member;

            MemberType = ReflectionHelper.GetMemberType(member);

            // Try to determine the RelationshipType
            if (relationshipInfo.RelationType == RelationshipTypes.AutoDetect)
            {
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(MemberType))
                {
                    relationshipInfo.RelationType = RelationshipTypes.Many;
                }
                else
                {
                    relationshipInfo.RelationType = RelationshipTypes.One;
                }
            }

            // Try to determine the EntityType
            if (relationshipInfo.EntityType == null)
            {
                if (relationshipInfo.RelationType == RelationshipTypes.Many)
                {
                    if (MemberType.IsGenericType)
                    {
                        // Assume a Collection<T> or List<T> and return T
                        relationshipInfo.EntityType = MemberType.GetGenericArguments()[0];
                    }
                    else
                    {
                        throw new ArgumentException(string.Format(
                            "The DataMapper could not determine the RelationshipAttribute EntityType for {0}.",
                            MemberType.Name));
                    }
                }
                else
                {
                    relationshipInfo.EntityType = MemberType;
                }
            }

            RelationshipInfo = relationshipInfo;



            Setter = MapRepository.Instance.ReflectionStrategy.BuildSetter(member.DeclaringType, member.Name);
        }

        public IRelationshipInfo RelationshipInfo { get; private set; }

        public MemberInfo Member { get; private set; }

        public Type MemberType { get; private set; }

		public ILazyLoaded LazyLoaded { get; set; }
        public bool IsLazyLoaded
        {
            get
            {
                return LazyLoaded != null;
            }
        }

		public IEagerLoaded EagerLoaded { get; set; }
		public bool IsEagerLoaded
		{
			get
			{
				return EagerLoaded != null;
			}
		}

		public IEagerLoadedJoin EagerLoadedJoin { get; set; }
		public bool IsEagerLoadedJoin
		{
			get
			{
				return EagerLoadedJoin != null;
			}
		}

		/// <summary>
		/// An undefined relationship is a relationship that has not been marked as 
		/// LazyLoaded, EagerLoaded or EagerLoadedJoin.  
		/// Undefined relationships will try to map joined columns (using alt names)
		/// into an object graph.
		/// </summary>
		public bool IsUndefined
		{
			get
			{
				return !IsLazyLoaded && !IsEagerLoaded && !IsEagerLoadedJoin;
			}
		}

        public GetterDelegate Getter { get; set; }
        public SetterDelegate Setter { get; set; }
    }
}
