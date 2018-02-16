using Akka.Actor;
using ConsoleApp.Shared;

namespace ConsoleApp.App
{
	/// <summary>
	/// The main application actor: it will the whole application
	/// 
	/// when it receives a start message we:
	/// - create all the needed interacting actors (reader, writer)
	/// - display the UI welcome screen
	/// - ask the reader actor to await for user input
	/// - process messages that belongs to us (terminate)
	/// </summary>
	internal class ConsoleUiActor : UntypedActor
	{
		protected override void OnReceive(object message)
		{
			var msg = (string)message;
			switch (msg)
			{
				case "start":
					// display some information
					ColoredConsole.WriteLine("Console UI Ready");
					ColoredConsole.WriteLine("Enter 'quit' to terminate");

					// create the writer and the reader
					var writer = Context.ActorOf<ConsoleWriterActor>("ConsoleWriter");
					var commandHandler = Context.ActorOf(Props.Create(() => new CommandHandlerActor(writer)), "CommandHandler");
					var reader = Context.ActorOf(Props.Create<ConsoleReaderActor>(this.Self, commandHandler), "ConsoleReader");

					// tell the reader actor to start reading
					reader.Tell("readnext");

					break;
				case "terminate":
					// tell the actor system to shutdown
					Context.System.Terminate();
					break;
				default:
					// remember to caall Undlandled() on any message not handled by an UntypedActor
					Unhandled(message);
					break;
			}
		}
	}
}
