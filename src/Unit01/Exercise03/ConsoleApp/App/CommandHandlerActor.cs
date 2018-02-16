using Akka.Actor;
using ConsoleApp.Shared.Messages;
using System.Threading;

namespace ConsoleApp.App
{
	internal class CommandHandlerActor : UntypedActor
	{
		private readonly IActorRef _writer;

		public CommandHandlerActor(IActorRef writer)
		{
			_writer = writer;
		}

		protected override void OnReceive(object message)
		{
			if (message is InputCommand)
			{
				var inputCommand = message as InputCommand;

				// do something heavy: will block the Actor
				Thread.Sleep(5000);

				_writer.Tell(new CommandCompleted(inputCommand.Data));
			}
			if (message is InputCommandRequest)
			{
				var inputCommand = message as InputCommandRequest;

				// do something heavy: will block the Actor
				Thread.Sleep(1000);

				// also inform the Sender that the command has been completed
				Sender.Tell(new InputCommandResponse(inputCommand.Data));
			}
			else
			{
				// remember to handle unhandled messages
				Unhandled(message);
			}
		}
	}
}
