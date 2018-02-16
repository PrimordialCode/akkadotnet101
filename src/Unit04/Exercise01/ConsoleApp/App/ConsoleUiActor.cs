extern alias akka;

using akka::Akka.Actor;
using ConsoleApp.Shared;
using ConsoleApp.Shared.Exceptions;
using ConsoleApp.Support;
using System;

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
	public class ConsoleUiActor : UntypedActorWithLogs
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
					ColoredConsole.WriteLine("Debug commands: dumpactors");
					ColoredConsole.WriteLine("Router commands: stop | poisonpill | kill | gracefulstop");
					ColoredConsole.WriteLine("Allowed commands: pausecommandhadler | resumecommandhandler | resumeexception | stopexception | restartexception | escalateexception | stop | poisonpill | kill | gracefulstop | any other text message");

					// create the writer and the reader
					var writer = Context.ActorOf<ConsoleWriterActor>("ConsoleWriter");
					Context.ActorOf(Props.Create(() => new DebugUtilsActor(writer)), "DebugUtils");
					// var commandHandler = Context.ActorOf(Props.Create(() => new CommandHandlerActor(writer)), "CommandHandler");
					// var commandHandler = Context.ActorOf(CommandHandlerActorConfig.ConsistentHashingRouterProps(writer), "CommandHandler");
					var commandHandler = Context.ActorOf(Props.Create(() => new CommandHandlerCoordinatorActor(writer)), "CommandHandler");
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
