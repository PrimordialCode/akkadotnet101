using System;

namespace ConsoleApp.Shared.Exceptions
{
	[Serializable]
	public class EscalateException : Exception
	{
		public EscalateException() { }
		public EscalateException(string message) : base(message) { }
		public EscalateException(string message, Exception inner) : base(message, inner) { }
		protected EscalateException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
