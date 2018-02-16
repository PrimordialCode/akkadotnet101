extern alias akka;

using akka::Akka.Actor;
using ConsoleApp.Shared;
using ConsoleApp.Shared.Exceptions;
using ConsoleApp.Shared.Messages;
using System.Threading;
using System;

namespace ConsoleApp.App
{
	internal class CommandHandlerActor : ReceiveActorWithLogs, IWithUnboundedStash
	{
		public IStash Stash { get; set; }

		private readonly IActorRef _writer;

		public CommandHandlerActor(IActorRef writer)
		{
			_writer = writer;

			Become(Running);
		}

		private void Running()
		{
			Receive<InputCommand>(this.Handle);
			Receive<InputCommandRequest>(this.Handle);
			Receive<EscalateExceptionCommand>(this.Handle);
			Receive<RestartExceptionCommand>(this.Handle);
			Receive<StopExceptionCommand>(this.Handle);
			Receive<ResumeExceptionCommand>(this.Handle);
			Receive<PauseCommandHandlerCommand>(message =>
			{
				_writer.Tell("Going out of service...");
				Become(Paused);
				// BecomeStacked(Paused);
			});
		}

		private void Paused()
		{
			Receive<ResumeCommandHandlerCommand>(message =>
			{
				_writer.Tell("Resuming operations...");
				Become(Running);
				// UnbecomeStacked();

				// Unstash all the messages
				Stash.UnstashAll();
			});
			// ReceiveAny must always be the last one!
			ReceiveAny((message) =>
			{
				ColoredConsole.WriteLineYellow($"Out of service: {message}");
				// Stash the messages for later processing
				Stash.Stash();
			});
		}

		protected override void PreRestart(Exception reason, object message)
		{
			// move stashed messages to the mailbox so they persist through restart
			Stash.UnstashAll();

			base.PreRestart(reason, message);
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
