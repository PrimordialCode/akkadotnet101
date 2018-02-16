# Unit 03

In this lesson we'll focus on Messages Patterns and Routing.

## Key concepts

We are going to learn:

- Hocon: Human Optimized Configuration Object Notation.
- Messaging Patterns like Pub/Sub.
- Message Routing and Built-in Routers.

## Start - Hocon

Configure the ActorSystem to use Hocon is pretty easy:

- Add your configuration files to the solution.
- Use the `ConfigurationFactory` facility to parse the files.
- Pass the obtained `Config` object to the `ActorSystem` creation method.

A sample Hocon configuration file `hocon.cfg` used to configure logging:

```ini
akka {
  stdout-loglevel = INFO # DEBUG
  loglevel = INFO # DEBUG
  log-config-on-start = on
  actor {
    debug {
      receive = on
      autoreceive = on
      lifecycle = on
      event-stream = on
      unhandled = on
    }
  }
}
```

the `program.cs` becomes:

```csharp
string hocon = System.IO.File.ReadAllText(@".\hocon.cfg");
var config = ConfigurationFactory.ParseString(hocon); // .WithFallback(...);
var actorSystem = ActorSystem.Create("ConsoleApp", config);
```

You can now play with the logging settings!

## Exercise 01 - ConsistentHashingPool Router

Let's rework the sample to simulate a multi-user scenario we'll use a `ConsistentHashingPool` Router to distribute the workload.

We'll simulate a multi-threading pipeline execution environment so that each user might have her own thread of concurrent execution.

Every command should carry on some information about the User that sent it.

The ConsoleReaderActor will be modified so that Some Commands will be emitted specifying the User that generated them:

`UserId Command` (the space is a separator).

All the 'Stop/Kill/PoisonPill' Commands will be directed to the Router.

### Change the Commands to provide Context Information

We'll start adding some information to the commands:

- One option is inherit all the commands from a base class (or implement an interface) that provides support for some Headers / Metadata information that define the 'context' of each message.
- Another option could be to create a wrapper structure that encapsulates the original command and adds support for the context information.

Go for option 1 and write down a base Command abstract class, change all the commands to inherit from this base class.

Add a `Command.cs` file inside the 'Shared\Messages' folder:

```csharp
/// <summary>
/// A sample base class that is used to implement Commands in a raw CQRS pattern.
/// </summary>
public abstract class Command
{
	/// <summary>
	/// Context information attached to the message
	/// </summary>
	public CommandContext Context { get; private set; } = new CommandContext();
}

public class CommandContext
{
	/// <summary>
	/// The UserId will contribute to identify the
	/// instance of the command handler to which route the message.
	/// </summary>
	public string UserId { get; set; }
}
```

### Define a ConsistentHashingPool Router

We'll change the actual CommandHandler with a `ConsistentHashingPool` Router.

We can do it changing the way the 'CommandHandler' Actor is configured and instantiated in ConsoleUiActor.

Add a new `CommandHandlerActorConfig.cs` file and copy the following static class containing the new registration functions.

This function will be used to create a `Props` object defining a ConsistentHashingPool router (with a well defined ConsistentHashMapping function):

```csharp
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
```

### CommandHandlerActor: Add UserId to the displayed message

Also add some more logging information when handling the CommandHandlerActor's `InputCommand`s:

```csharp
...
bool Handle(InputCommand message)
{
	// do something heavy: will block the Actor
	Thread.Sleep(5000);

	_writer.Tell(new CommandCompleted($"{ActorName} {message.Context.UserId} {message.Data}"));

	return true;
}

bool Handle(InputCommandRequest message)
{
	// do something heavy: will block the Actor
	Thread.Sleep(1000);

	// also inform the Sender that the command has been completed
	Sender.Tell(new InputCommandResponse($"{ActorName} {message.Context.UserId} {message.Data}"));

	return true;
}
...
```

### ConsoleUiActor: create an instance of the router actor

Modify the `ConsoleUiActor` (ConsoleUiActor.cs) to create an instance of the routing Actor.

Replace:

```csharp
var commandHandler = Context.ActorOf(Props.Create(() => new CommandHandlerActor(writer)), "CommandHandler");
```

With:

```csharp
var commandHandler = Context.ActorOf(CommandHandlerActorConfig.ConsistentHashingRouterProps(writer), "CommandHandler");
```

### ConsoleReaderActor: change how commands are parsed, accept UserId on the input

The last thing to do is to change the `ConsoleReaderActor`, many commands (except the 'quit' and the stop commands) must now be emitted by a User; the commands will follow this pattern:

`UserId Command` (the space is a separator):

```csharp
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
```

Run the application and send some commands.

You can even try to generate failures in several routees to see the supervision strategy in action.

## Exercise 02 - Child per Entity

The ConsistentHashingPool Router works, we can distribute the work among different threads (execution pipeline), but what if we want an execution pipeline per Single User.

The Consistent Hash Mapping algorithm does not guarantee that: several keys can be mapped on the same Actor instance.

What we want is a 1:1 relationship between the domain entity / process and the actor.

Let's follow another approach and manage the children worker Actors ourselves... in short we'll act as a Router.

Enter: __Child-per-Entity__ pattern.

### 1. Change CommandHandlerActor and derive a CommandHandlerWorkerActor

We start the sample changing the CommandHandlerActor a bit and deriving a new CommandHandlerWorkerActor, this way we keep the previous sample working.

In `CommandHandlerActor`, change the Running() and Pause() behavior function to be 'protected virtual':

```csharp
protected virtual void Running() { ... }

protected virtual void Paused() { ... }
```

Add a new `CommandhandlerWorkerActor` derived from 'CommandHandlerActor' and change its behavior:

```csharp
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
```

The actor will send a ReceiveTimeout message to itself if no messages are processed for the defined timespan window (idle period).

As a result of this shutdown the Actor will terminate itself freeing some resources.

### 2. Create a 'CommandHandlerCoordinatorActor'

Add some more commands to control the lifecycle of the child worker actors:

```csharp
public class StopCommand : Command
{
}

public class PoisonPillCommand : Command
{
}

public class KillCommand : Command
{
}

public class GracefulStopCommand : Command
{
}
```

Let's create a `CommandHandlerCoordinatorActor`.

It will be responsible for:

- Creating child worker actors if needed (must check if she already has an instance of the requested actor running).
- Forward messages to the children telling them what to do.
- Supervise the children.

```csharp
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
```

### Change the 'ConsoleReaderActor' to emit the new commands for our workers

Inside the OnReceive() function find the Command parsing code, the one used to send the commands to the child actors; add the following code to parse and generate the proper shutdown commands for the worker actors.

Around line 130 of `ConsoleReaderActor`, add:

```csharp
...
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
...
```

### Change 'ConsoleUIActor' to use the new CommandHandlerCoordinatorActor

Modify the `ConsoleUiActor` (ConsoleUiActor.cs) to create an instance of the coordinator Actor.

Replace:

```csharp
var commandHandler = Context.ActorOf(Props.Create(() => new CommandHandlerActor(writer)), "CommandHandler");

or

var commandHandler = Context.ActorOf(CommandHandlerActorConfig.ConsistentHashingRouterProps(writer), "CommandHandler");
```

With:

```csharp
var commandHandler = Context.ActorOf(Props.Create(() => new CommandHandlerCoordinatorActor(writer)), "CommandHandler");
```

Run the Application and play sending some commands as different users.

## Exercise 03 - (Bonus): Identify Actors

Sometimes given an `ActorSelection` we need to know the real `IActorRef` of the actor we're going to talk to.

We can ask the Actors to identify themselves sending them an `Identify` message.

They will reply with an `ActorIdentity` message that contains the IActorRef of the Actor that replied.

We want to be able to identify all the active actors starting from a given node in the hierarchy, maybe the CommandHandlerCoordinator actor of the previous sample.

Let's add a new 'DumpActors' command and a new 'DebugUtilsActor' that's responsible for managing the process of walking the Actors hierarchy tree asking each node to identify itself.

Add a 'Support' folder with the following classes:

```csharp
public class DumpActors
{
	public string SelectionPath { get; }

	public DumpActors(string selectionPath)
	{
		SelectionPath = selectionPath;
	}
}
```

```csharp
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
```

In the 'ConsoleUiActor' create an instance of the new Actor:

```csharp
Context.ActorOf(Props.Create(() => new DebugUtilsActor(writer)), "DebugUtils");
```

We now need to change the 'ConsoleReaderActor' to support a new 'dumpactors' command.

Change the OnReceive() function to send the new command to the new actor using ActorSelection:

```csharp
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
...

```

**WARNING - Identity is NOT a system message! It will NOT take priority on other messages.**
