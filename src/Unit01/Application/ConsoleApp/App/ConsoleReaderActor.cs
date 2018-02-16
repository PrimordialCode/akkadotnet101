using Akka.Actor;
using ConsoleApp.Shared;
using ConsoleApp.Shared.Messages;
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
	internal class ConsoleReaderActor : UntypedActor
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

		protected override void OnReceive(object message)
		{
			var text = Console.ReadLine();
			switch (text)
			{
				case "quit":
					_consoleUi.Tell("terminate");
					return;
				default:
					// Tell something!
					_commandHandler.Tell(new InputCommand(text));

					/*
					// Ask something!
					ColoredConsole.WriteLineYellow("Awaiting for the command to complete");
					var completedTask = _commandHandler.Ask<InputCommandResponse>(new InputCommandRequest(text));
					completedTask.Wait(); // blocking operation: wait for completion and a specific reply
					ColoredConsole.WriteLineYellow($"Response: {completedTask.Result.Data}");
					ColoredConsole.WriteLineYellow("The command has been completed");
					*/

					break;
			}
			// send a message to outself stating we are ready to read the next command
			Self.Tell("readnext");

			// we have no unhandled message here, every message will activare the read cycle again.
		}
	}
}
