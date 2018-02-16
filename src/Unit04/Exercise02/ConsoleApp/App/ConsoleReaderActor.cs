extern alias akka;

using akka::Akka.Actor;
using ConsoleApp.Shared;
using ConsoleApp.Shared.Messages;
using ConsoleApp.Support.Messages;
using System;

namespace ConsoleApp.App
{
	/// <summary>
	/// An Actor that manages the user input from the console,
	/// - it waits for user input
	/// - it parses the user input into commands that will be directed to the rest of the application
	/// 
	/// To 'activate' the actor we need to send it a message, we ask her to read the next message with a 'readnext' message.
	/// (actually any message activates the actor)
	/// </summary>
	internal class ConsoleReaderActor : UntypedActorWithLogs
	{
		private readonly IActorRef _consoleUi;
		private readonly IActorRef _commandHandler;

		public ConsoleReaderActor(
			IActorRef consoleUi,
			IActorRef commandHandler)
		{
			_consoleUi = consoleUi;
			_commandHandler = commandHandler;
		}

		private const string QuitCommand = "quit";
		private const string StopCommand = "stop";
		private const string PoisonPillCommand = "poisonpill";
		private const string KillCommand = "kill";
		private const string GracefulStopCommand = "gracefulstop";
		private const string ResumeExceptionCommand = "resumeexception";
		private const string StopExceptionCommand = "stopexception";
		private const string RestartExceptionCommand = "restartexception";
		private const string EscalateExceptionCommand = "escalateexception";
		private const string PauseCommandHandlerCommand = "pausecommandhandler";
		private const string ResumeCommandHandlerCommand = "resumecommandhandler";
		private const string DumpActors = "dumpactors";

		protected override void OnReceive(object message)
		{
			var text = Console.ReadLine();
			// Parse the Command (not production code! :D)
			// First check the stop/kill commands directed to the router
			switch (text)
			{
				case QuitCommand:
					_consoleUi.Tell("terminate");
					return;
				case DumpActors:
					var selection = Context.System.ActorSelection("/user/ConsoleUi/DebugUtils");
					selection.Tell(new DumpActors("/user/ConsoleUi/CommandHandler"));

					ReadNext();
					return;
				case StopCommand:
					// Send a System Stop message to the specified actor (the message will have priority over others already in the mailbox).
					// Actor receives the Stop message and suspends the actor’s Mailbox (other messages will go to DeadLetters).
					// Actor tells all its children to Stop. Stop messages propagate down the hierarchy below the actor.
					// Actor waits for all children to stop.
					// Actor calls PostStop lifecycle hook method for resource cleanup.
					// Actor shuts down.
					Context.Stop(_commandHandler);

					ReadNext();
					return;
				case PoisonPillCommand:
					// Use a PoisonPill message if you want the actor to process its mailbox before shutting down.
					// PoisonPill is a message that will be placed in the mailbox.
					// When the Actor process the message the above mentioned stop sequence will be initiated.
					_commandHandler.Tell(PoisonPill.Instance);

					ReadNext();
					return;
				case KillCommand:
					// Use a Kill message if you want it to show in your logs that the actor was killed.
					// Send a System Kill message to the specified actor.
					// The actor throws an ActorKilledException (The actor’s supervisor logs this message).
					// This suspends the actor mailbox from processing further user messages.
					// The actor’s supervisor handles the ActorKilledException and issues a Stop directive.
					// The actor will stop following the above mentioned stop sequence.
					_commandHandler.Tell(Kill.Instance);

					ReadNext();
					return;
				case GracefulStopCommand:
					// If you want confirmation that the actor was stopped within a specified Timespan.
					// It will send a PoisonPill message and 'start a timer to check if the actor stops within the specified amount of time'.
					// It will return a Task<bool> you can wait on to know if the Actor was stopped.
					// The Task can be cancelled if the Actor does not stop with the specified TimeSpan.
					try
					{
						var gracefulStop = _commandHandler.GracefulStop(TimeSpan.FromSeconds(5));
						gracefulStop.Wait();
						ColoredConsole.WriteLineGreen("GracefulStop completed");
					}
					catch (AggregateException ex)
					{
						// the GracefulStop can fail if it cannot complete within the specified TimeSpan.
						// The Task will be cancelled.
						ColoredConsole.WriteLineYellow($"GracefulStop failed, exception: {ex}");
					}

					ReadNext();
					return;
			}

			// Try to parse the User Commands: 'UserId command'
			var parsedText = text.Split(' ');
			if (parsedText.Length == 2)
			{
				Command cmd;
				switch (parsedText[1])
				{
					case EscalateExceptionCommand:
						cmd = new EscalateExceptionCommand();
						break;
					case RestartExceptionCommand:
						cmd = new RestartExceptionCommand();
						break;
					case ResumeExceptionCommand:
						cmd = new ResumeExceptionCommand();
						break;
					case StopExceptionCommand:
						cmd = new StopExceptionCommand();
						break;
					case PauseCommandHandlerCommand:
						cmd = new PauseCommandHandlerCommand();
						break;
					case ResumeCommandHandlerCommand:
						cmd = new ResumeCommandHandlerCommand();
						break;
					case StopCommand:
						cmd = new StopCommand();
						break;
					case PoisonPillCommand:
						cmd = new PoisonPillCommand();
						break;
					case KillCommand:
						cmd = new KillCommand();
						break;
					case GracefulStopCommand:
						cmd = new GracefulStopCommand();
						break;
					default:
						// Tell something!
						cmd = new InputCommand(parsedText[1]);

						/*
						// Ask something!
						ColoredConsole.WriteLineYellow("Awaiting for the command to complete");
						var completedTask = _commandHandler.Ask<InputCommandResponse>(new InputCommandRequest(parsedText[1]));
						completedTask.Wait(); // blocking operation: wait for completion and a specific reply
						ColoredConsole.WriteLineYellow($"Response: {completedTask.Result.Data}");
						ColoredConsole.WriteLineYellow("The command has been completed");
						*/

						break;
				}
				cmd.Context.UserId = parsedText[0];
				_commandHandler.Tell(cmd);
			}
			else
			{
				ReportUnsupportedCommand(text);
			}
			ReadNext();

			// we have no unhandled message here, every message will activare the read cycle again.
		}

		protected void ReadNext()
		{
			// send a message to outself stating we are ready to read the next command
			Self.Tell("readnext");
		}

		private void ReportUnsupportedCommand(string text)
		{
			ColoredConsole.WriteLineYellow("Unsuported command: " + text);
		}
	}
}
