extern alias akka;

using akka::Akka.Actor;
using ConsoleApp.Shared;
using ConsoleApp.Shared.Exceptions;
using ConsoleApp.Shared.Messages;
using System.Threading;

namespace ConsoleApp.App
{
	internal class CommandHandlerActor : UntypedActorWithLogs
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
				Thread.Sleep(2000);

				_writer.Tell(new CommandCompleted(inputCommand.Data));
			} 
			else if (message is InputCommandRequest)
			{
				var inputCommand = message as InputCommandRequest;

				// do something heavy: will block the Actor
				Thread.Sleep(1000);

				// also inform the Sender that the command has been completed
				Sender.Tell(new InputCommandResponse(inputCommand.Data));
			}
			else if (message is EscalateExceptionCommand)
			{
				throw new EscalateException();
			}
			else if (message is RestartExceptionCommand)
			{
				throw new RestartException();
			}
			else if (message is ResumeExceptionCommand)
			{
				throw new ResumeException();
			}
			else if (message is StopExceptionCommand)
			{
				throw new StopException();
			}
			else
			{
				// remember to handle unhandled messages
				Unhandled(message);
			}
		}
	}
}
