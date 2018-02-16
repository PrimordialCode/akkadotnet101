extern alias akka;

using akka::Akka.Actor;
using ConsoleApp.Shared;
using ConsoleApp.Shared.Exceptions;
using ConsoleApp.Shared.Messages;
using System;

namespace ConsoleApp.App
{
	/// <summary>
	/// Worker Actors Coordinator
	/// 
	/// it follows the 'child per entity' pattern
	/// - it forwards the messages to the appropriate child worker actor
	/// - creates the new command handlers (given the content of the message)
	/// - forward the message to the correct handler
	/// - spawned actors have a way to stop themselves when not needed anymore
	/// 
	/// downside:
	/// - we pay the price of having a mailbox for this Actor (it should be a router)
	/// </summary>
	internal class CommandHandlerCoordinatorActor : ReceiveActorWithLogs
	{
		private readonly IActorRef _writer;

		public CommandHandlerCoordinatorActor(IActorRef writer)
		{
			this._writer = writer;

			Receive<Shared.Messages.Command>(this.Handle);
		}

		private bool Handle(Shared.Messages.Command command)
		{
			// create a command handling actor instance if needed and then
			// forward the command
			var childActorId = GetHashMapping(command);
			var child = Context.Child(childActorId);
			if (child == ActorRefs.Nobody)
			{
				child = Context.ActorOf(Props.Create<CommandHandlerWorkerActor>(_writer), childActorId);
			}
			// check for stop, poisonpill, kill, gracefulstop commands..
			// It's responsibility of this Actor to manage its children
			switch (command)
			{
				case StopCommand cmd:
					Context.Stop(child);
					return true;
				case PoisonPillCommand cmd:
					child.Tell(PoisonPill.Instance);
					return true;
				case KillCommand cmd:
					child.Tell(Kill.Instance);
					return true;
				case GracefulStopCommand cmd:
					try
					{
						var gracefulStop = child.GracefulStop(TimeSpan.FromSeconds(5));
						gracefulStop.Wait();
						ColoredConsole.WriteLineGreen("GracefulStop completed");
					}
					catch (AggregateException ex)
					{
						// the GracefulStop can fail if it cannot complete within the specified TimeSpan.
						// The Task will be cancelled.
						ColoredConsole.WriteLineYellow($"GracefulStop failed, exception: {ex}");
					}
					return true;
			}

			// child.Tell(command);
			child.Forward(command);
			return true;
		}

		private static string GetHashMapping(Shared.Messages.Command command)
		{
			return command.Context.UserId;
		}

		protected override SupervisorStrategy SupervisorStrategy()
		{
			// AllForOneStrategy
			// OneForOneStrategy
			return new OneForOneStrategy(
				3,
				TimeSpan.FromMinutes(2),
				ex =>
				{
					if (ex is EscalateException)
					{
						return Directive.Escalate;
					}
					if (ex is RestartException)
					{
						return Directive.Restart;
					}
					if (ex is ResumeException)
					{
						return Directive.Resume;
					}
					if (ex is StopException)
					{
						return Directive.Stop;
					}
					// delegate to the default supervision startegy
					return akka.Akka.Actor.SupervisorStrategy.DefaultStrategy.Decider.Decide(ex);
				});
		}
	}
}
