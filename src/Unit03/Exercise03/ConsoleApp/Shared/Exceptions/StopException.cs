using System;

namespace ConsoleApp.Shared.Exceptions
{
	[Serializable]
	public class StopException : Exception
	{
		public StopException() { }
		public StopException(string message) : base(message) { }
		public StopException(string message, Exception inner) : base(message, inner) { }
		protected StopException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
