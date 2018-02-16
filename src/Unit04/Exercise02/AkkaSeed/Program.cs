using Akka.Actor;
using Akka.Configuration;
using System;
using System.Reflection;

namespace AkkaSeed
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			string hocon = System.IO.File.ReadAllText(@".\hocon.cfg");
			var config = ConfigurationFactory.ParseString(hocon);

			// get the ActorSystem name from the configuration file
			var actorSystemName = config.GetConfig("akka-seed").GetString("actorsystem");
			if (string.IsNullOrWhiteSpace(actorSystemName))
			{
				throw new ConfigurationException("Please provide an ActorSystem name in the 'akka-seed/actorsystem' configuration section.");
			}

			var actorSystem = ActorSystem.Create(actorSystemName, config);
			Console.WriteLine($"[{Assembly.GetExecutingAssembly().FullName}]");
			Console.WriteLine($"'{actorSystem.Name}' Actor System Created.");

			Console.WriteLine("Press 'enter' to quit.");
			Console.ReadLine();
			actorSystem.Terminate();

			// wait for the actor system to terminate
			Console.WriteLine("Awaiting for ActorSystem Termination.");
			actorSystem.WhenTerminated.Wait();
			Console.WriteLine("ActorSystem Terminated.");

			Console.WriteLine("Press 'enter' to exit.");
			Console.ReadLine();
		}
	}
}
