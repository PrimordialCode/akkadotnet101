extern alias akka;

using akka::Akka.Actor;
using akka::Akka.Routing;
using ConsoleApp.Shared.Exceptions;
using System;

namespace ConsoleApp.App
{
	internal static class CommandHandlerActorConfig
	{
		/// <summary>
		/// Consistent Hash Mapping function
		/// </summary>
		private readonly static ConsistentHashMapping HashMapping = msg =>
		{
			if (msg is Shared.Messages.Command m)
			{
				return m.Context.UserId;
			}
			return null;
		};

		/// <summary>
		/// Props that configure a ConsistentHasingPool router.
		/// The default supervision strategy of a pool router is 'Escalate'.
		/// </summary>
		public readonly static Func<IActorRef, Props> ConsistentHashingRouterProps = (IActorRef writer) =>
			Props.Create(() => new CommandHandlerActor(writer))
				.WithRouter(new ConsistentHashingPool(5, HashMapping)
				.WithResizer(new DefaultResizer(5, 10))
				.WithSupervisorStrategy(
					// AllForOneStrategy
					// OneForOneStrategy
					new OneForOneStrategy(3, TimeSpan.FromMinutes(2),
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
						})));
	}
}
