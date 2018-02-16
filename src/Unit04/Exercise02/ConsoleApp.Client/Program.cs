using Akka.Actor;
using Akka.Configuration;
using ConsoleApp.App;
using ConsoleApp.Shared;
using System;
using System.Reflection;

namespace ConsoleApp.Client
{
	static class Program
	{
		static void Main(string[] args)
		{
			string hocon = System.IO.File.ReadAllText(@".\hocon.cfg");
			var config = ConfigurationFactory.ParseString(hocon); // .WithFallback(...);
			var actorSystem = ActorSystem.Create("ConsoleApp", config);
			ColoredConsole.WriteLineGreen($"[{Assembly.GetExecutingAssembly().FullName}]");
			ColoredConsole.WriteLineGreen($"'{actorSystem.Name}' Actor System Created.");

			// start the Application Actors
			// var consoleUi = actorSystem.ActorOf<ConsoleUiActor>("ConsoleUi");
			var consoleUi = actorSystem.ActorOf(Props.Create<ConsoleUiActor>(), "ConsoleUi");
			consoleUi.Tell("start");

			// actorSystem.Terminate();

			// wait for the actor system to terminate
			ColoredConsole.WriteLine("Awaiting for ActorSystem Termination.");
			actorSystem.WhenTerminated.Wait();
			ColoredConsole.WriteLine("ActorSystem Terminated.");

			ColoredConsole.WriteLine("Press 'enter' to exit.");
			Console.ReadLine();
		}
	}
}
