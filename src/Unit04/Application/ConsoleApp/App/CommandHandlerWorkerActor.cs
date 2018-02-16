extern alias akka;

using akka::Akka.Actor;
using System;

namespace ConsoleApp.App
{
	internal class CommandHandlerWorkerActor : CommandHandlerActor
	{
		public CommandHandlerWorkerActor(IActorRef writer) : base(writer)
		{
		}

		protected override void PreStart()
		{
			base.PreStart();
			// Schedule a timeout message after an inactivity TimeSpan
			Context.SetReceiveTimeout(TimeSpan.FromSeconds(30));
		}

		/// <summary>
		/// override the behavior to provide support for new messages
		/// </summary>
		protected override void Running()
		{
			// The worker shutdown itself if idle for too long
			Receive<ReceiveTimeout>(timeout => Self.Tell(PoisonPill.Instance));
			base.Running();
		}
	}
}
