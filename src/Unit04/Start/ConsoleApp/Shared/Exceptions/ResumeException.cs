using System;

namespace ConsoleApp.Shared.Exceptions
{
	[Serializable]
	public class ResumeException : Exception
	{
		public ResumeException() { }
		public ResumeException(string message) : base(message) { }
		public ResumeException(string message, Exception inner) : base(message, inner) { }
		protected ResumeException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
