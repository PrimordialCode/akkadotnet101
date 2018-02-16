extern alias akka;

using akka::Akka.Actor;
using ConsoleApp.Shared;
using ConsoleApp.Shared.Exceptions;
using ConsoleApp.Shared.Messages;
using System.Threading;

namespace ConsoleApp.App
{
	internal class CommandHandlerActor : ReceiveActorWithLogs
	{
		private readonly IActorRef _writer;

		public CommandHandlerActor(IActorRef writer)
		{
			_writer = writer;

			Receive<InputCommand>(this.Handle);
			Receive<InputCommandRequest>(this.Handle);
			Receive<EscalateExceptionCommand>(this.Handle);
			Receive<RestartExceptionCommand>(this.Handle);
			Receive<StopExceptionCommand>(this.Handle);
			Receive<ResumeExceptionCommand>(this.Handle);
		}

		bool Handle(InputCommand message)
		{
			// do something heavy: will block the Actor
			Thread.Sleep(2000);

			_writer.Tell(new CommandCompleted(message.Data));

			return true;
		}

		bool Handle(InputCommandRequest message)
		{
			// do something heavy: will block the Actor
			Thread.Sleep(1000);

			// also inform the Sender that the command has been completed
			Sender.Tell(new InputCommandResponse(message.Data));

			return true;
		}

		bool Handle(EscalateExceptionCommand message)
		{
			throw new EscalateException();
		}

		bool Handle(RestartExceptionCommand message)
		{
			throw new RestartException();
		}

		bool Handle(StopExceptionCommand message)
		{
			throw new StopException();
		}

		bool Handle(ResumeExceptionCommand message)
		{
			throw new ResumeException();
		}
	}
}
