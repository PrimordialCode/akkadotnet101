using Akka.Actor;
using ConsoleApp.App;
using ConsoleApp.Shared;
using System;

namespace ConsoleApp
{
	static class Program
	{
		static void Main(string[] args)
		{
			var actorSystem = ActorSystem.Create("ConsoleApp");
			ColoredConsole.WriteLineGreen($"{actorSystem.Name} Actor System Created.");

			/*
			// start the Application Actors
			// var consoleUi = actorSystem.ActorOf<ConsoleUiActor>("ConsoleUi");
			var consoleUi = actorSystem.ActorOf(Props.Create<ConsoleUiActor>(), "ConsoleUi");
			consoleUi.Tell("start");
			*/

			actorSystem.Terminate();

			// wait for the actor system to terminate
			ColoredConsole.WriteLine("Awaiting for ActorSystem Termination.");
			actorSystem.WhenTerminated.Wait();
			ColoredConsole.WriteLine("ActorSystem Terminated.");

			ColoredConsole.WriteLine("Press 'enter' to exit.");
			Console.ReadLine();
		}
	}
}
