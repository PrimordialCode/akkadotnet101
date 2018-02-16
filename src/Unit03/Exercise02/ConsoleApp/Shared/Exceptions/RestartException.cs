using System;

namespace ConsoleApp.Shared.Exceptions
{
	[Serializable]
	public class RestartException : Exception
	{
		public RestartException() { }
		public RestartException(string message) : base(message) { }
		public RestartException(string message, Exception inner) : base(message, inner) { }
		protected RestartException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
