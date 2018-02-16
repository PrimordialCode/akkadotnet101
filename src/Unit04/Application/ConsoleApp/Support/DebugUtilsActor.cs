extern alias akka;

using akka::Akka.Actor;
using ConsoleApp.Shared;
using ConsoleApp.Support.Messages;

namespace ConsoleApp.Support
{
	/// <summary>
	/// Actor that is used to identify all the actors starting from a specified Node in the tree.
	/// We can also specify some identities to skip (because they can be blocking or they can cause infinite recursion)
	/// 
	/// This actor will not be shown in the hierarchy.
	/// 
	/// Actors that should not be considered:
	/// - DebugUtilsActor, it seems it's always included in the selection.
	/// - ConsoleReaderActor is blocking! actor process 1 message at a time, it's identity will be dumped when the next command arrives
	/// </summary>
	internal class DebugUtilsActor : ReceiveActorWithLogs
	{
		private readonly IActorRef _writer;

		public DebugUtilsActor(IActorRef writer)
		{
			_writer = writer;
			Receive<ActorIdentity>(id =>
			{
				// There might be a bug, it seems the current actor is always included in the ActorSelection;
				// do not select my children, block recursion
				if (Sender != Self)
				{
					_writer.Tell(string.Format("actor {0}, parent {1}", Sender, id.MessageId));
					Context.System.ActorSelection(Sender.Path + "/*").Tell(new Identify(Sender.Path), Self);
				}
			});
			Receive<DumpActors>(msg =>
			{
				// Context.System.ActorSelection("../../*").Tell(new Identify(""), Self);
				Context.System.ActorSelection(msg.SelectionPath).Tell(new Identify(""), Self);
			});
		}
	}
}
