using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data
{
	[Serializable]
	public class RelationshipLoadException : Exception
	{
		public RelationshipLoadException() { }
		public RelationshipLoadException(string message) : base(message) { }
		public RelationshipLoadException(string message, Exception inner) : base(message, inner) { }
		protected RelationshipLoadException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}
