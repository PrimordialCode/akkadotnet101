using ConsoleApp.Shared;
using ConsoleApp.Shared.Messages;

namespace ConsoleApp.App
{
	/// <summary>
	/// An actor that displays the user input
	/// - it will receive text messages
	/// </summary>
	internal class ConsoleWriterActor : UntypedActorWithLogs
	{
		protected override void OnReceive(object message)
		{
			if (message is CommandCompleted)
			{
				ColoredConsole.WriteLineGreen(((CommandCompleted)message).Data);
			}
			else if (message is string)
			{
				ColoredConsole.WriteLine((string)message);
			}
			else
			{
				// call unhandled for any message you are not going to deal with.
				Unhandled(message);
			}
		}
	}
}
