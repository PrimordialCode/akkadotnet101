# Unit 02

In this lesson we'll focus on some 'advanced' features of the Akka.net Actors.

## Key concepts

We are going to learn:

- Actor Lifecycle.
- Supervision.
- ReceiveActor.
- Behaviors.

## Exercise 01 - Lifecycle Hooks

Actors have a well defined lifecycle, they are created, listen and execute messages, can be stopped and can be restarted.

An actor is very similar to a well defined finite state machine.

We can execute custom code during the Actor state transitions wiring up the available Lifecycle hooks.

Let's write some logging code on the Lifecycle hooks (Akka.net has its own logging framework which can be configured using HOCON or writing Configuration Code.).

Create a new `UntypedActorWithLogs` class that will override the methods:

```csharp
public abstract class UntypedActorWithLogs : UntypedActor
{
	public static bool ShowLog = true;

	protected string ActorName
	{
		get { return $"[{GetType().Name} - {Context.Self.Path}]"; }
	}

	protected UntypedActorWithLogs()
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} Constructor");
		}
	}

	protected override void PreStart()
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} PreStart");
		}
		base.PreStart();
	}

	protected override void PostStop()
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} PostStop");
		}
		base.PostStop();
	}

	protected override void PreRestart(Exception reason, object message)
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} PreRestart, reason: {reason.Message}, message: {message}");
		}
		base.PreRestart(reason, message);
	}

	protected override void PostRestart(Exception reason)
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} PostRestart, reason: {reason.Message}");
		}
		base.PostRestart(reason);
	}
}
```

Change the base class of each Actor and run the sample, you'll see the state transition of each Actor colored in magenta.

Some lifecycle hooks can be tested only after we introduce Supervision Strategies.

## Exercise 02 - Supervision Strategies (and lifecycle hooks)

Let's show how supervision work by adding some commands that cause intentional failues in the `CommandHandlerActor` to see how the Supervision Strategy is configured in its parent actor (`ConsoleUiActor`).

### Add Some Exceptions

Add the following classes to the 'Shared/Exceptions' folder (or create it if not present), they represent some application exception that will be handled with different strategies:

```csharp
[Serializable]
public class ResumeException : Exception
{
	public ResumeException() { }
	public ResumeException(string message) : base(message) { }
	public ResumeException(string message, Exception inner) : base(message, inner) { }
	protected ResumeException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class StopException : Exception
{
	public StopException() { }
	public StopException(string message) : base(message) { }
	public StopException(string message, Exception inner) : base(message, inner) { }
	protected StopException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class RestartException : Exception
{
	public RestartException() { }
	public RestartException(string message) : base(message) { }
	public RestartException(string message, Exception inner) : base(message, inner) { }
	protected RestartException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class EscalateException : Exception
{
	public EscalateException() { }
	public EscalateException(string message) : base(message) { }
	public EscalateException(string message, Exception inner) : base(message, inner) { }
	protected EscalateException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
```

### Add new commands to trigger the exceptions

Add the following classes to the 'Shared/Messages' folder:

```csharp
public class ResumeExceptionCommand
{
}

public class StopExceptionCommand
{
}

public class RestartExceptionCommand
{
}

public class EscalateExceptionCommand
{
}
```

Modify the `ConsoleReaderActor` to send these commands to the `CommandHandlerActor`:

```csharp
private const string ResumeExceptionCommand = "resumeexception";
private const string StopExceptionCommand = "stopexception";
private const string RestartExceptionCommand = "restartexception";
private const string EscalateExceptionCommand = "escalateexception";
```

In the OnReceive() function, add the following code:

```csharp
case EscalateExceptionCommand:
	_commandHandler.Tell(new EscalateExceptionCommand());
	break;
case RestartExceptionCommand:
	_commandHandler.Tell(new RestartExceptionCommand());
	break;
case ResumeExceptionCommand:
	_commandHandler.Tell(new ResumeExceptionCommand());
	break;
case StopExceptionCommand:
	_commandHandler.Tell(new StopExceptionCommand());
	break;
```

### Change the Command Handler to trigger the exceptions

Now change the `CommandHandlerActor`'s OnReceive() function to react to these new messages and generate the exceptions:

```csharp
if (message is EscalateExceptionCommand)
{
	throw new EscalateException();
}
else if (message is RestartExceptionCommand)
{
	throw new RestartException();
}
else if (message is ResumeExceptionCommand)
{
	throw new ResumeException();
}
else if (message is StopExceptionCommand)
{
	throw new StopException();
}
```

### Add a Supervision Strategy to the parent Actor

In the end we need to specify the Supervision Strategy in the appropriate Actor; the current parent for CommandHandlerActor is `ConsoleUiActor`, here we override the default SupervisionStrategy:

```csharp
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
```

Run the application and test how the Actor behave when it crashes and the supervisor take decisions.

## Exercise 03 - ReceiveActor

The `UntypedActor` base class used so far is effective but it does not offer many facilities to make our code expressive.

Introducing `ReceiveActor`: it has all the nice features of the base class plus some more methods (syntactic sugar methods) and helper functions that make writing an Actor easier.

Let's write a `ReceiveActorWithLogs` class to keep displaying our lifecycle hooks:

```csharp
public abstract class ReceiveActorWithLogs : ReceiveActor
{
	public static bool ShowLog = true;

	protected string ActorName
	{
		get { return $"[{GetType().Name} - {Context.Self.Path}]"; } // Context.Self.Path.ToStringWithUid()
	}

	protected ReceiveActorWithLogs()
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} Constructor");
		}
	}

	protected override void PreStart()
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} PreStart");
		}
		base.PreStart();
	}

	protected override void PostStop()
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} PostStop");
		}
		base.PostStop();
	}

	protected override void PreRestart(Exception reason, object message)
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} PreRestart, reason: {reason.Message}, message: {message}");
		}
		base.PreRestart(reason, message);
	}

	protected override void PostRestart(Exception reason)
	{
		if (ShowLog)
		{
			ColoredConsole.WriteLineMagenta($"{ActorName} PostRestart, reason: {reason.Message}");
		}
		base.PostRestart(reason);
	}
}
```

We can rewrite the `CommandHandlerActor`, using `ReceiveActor` as base class:

```csharp
internal class CommandHandlerActor : ReceiveActorWithLogs
{
	private readonly IActorRef _writer;

	public CommandHandlerActor(IActorRef writer)
	{
		_writer = writer;

		Receive<InputCommand>(this.Handle);
		Receive<InputCommandRequest>(this.Handle);
		Receive<EscalateExceptionCommand>(this.Handle);
		Receive<RestartExceptionCommand>(this.Handle);
		Receive<StopExceptionCommand>(this.Handle);
		Receive<ResumeExceptionCommand>(this.Handle);
	}

	bool Handle(InputCommand message)
	{
		// do something heavy: will block the Actor
		Thread.Sleep(2000);

		_writer.Tell(new CommandCompleted(message.Data));

		return true;
	}

	bool Handle(InputCommandRequest message)
	{
		// do something heavy: will block the Actor
		Thread.Sleep(1000);

		// also inform the Sender that the command has been completed
		Sender.Tell(new InputCommandResponse(message.Data));

		return true;
	}

	bool Handle(EscalateExceptionCommand message)
	{
		throw new EscalateException();
	}

	bool Handle(RestartExceptionCommand message)
	{
		throw new RestartException();
	}

	bool Handle(StopExceptionCommand message)
	{
		throw new StopException();
	}

	bool Handle(ResumeExceptionCommand message)
	{
		throw new ResumeException();
	}
}
```

Now run the application and verify everything continue to work like before.

## Exercise 04 - Actor Behavior & Stash

We can think about our Actors as finite state machines: in each state they act and react in a very unique way.

Let's rethink the `CommandHandlerActor` so it has two states: 'Running' and 'Paused'; in the running state it will operate normally, in the paused state it will just report that he cannot take any action at all (for now).

Add a couple of commands:

```csharp
public class PauseCommandHandlerCommand
{
}

public class ResumeCommandHandlerCommand
{
}
```

### Change the CommandHandlerActor - add Behaviors

- Creating a new 'Running()' function that will contain all the previous Receive() function calls, add the handler for the `PauseCommandHandlerCommand` that will switch to the new 'Paused' behavior.
- In the Actor's constructor switch to the 'Running' behavior:
- add a new 'Paused()' function with the new behavior, it will handle the `ResumeCommandHandler` command going back to the Running behavior, every other message will result in an 'out of service' alert:

```csharp
public CommandHandlerActor(IActorRef writer)
{
	_writer = writer;

	Become(Running);
}

private void Running()
{
	Receive<InputCommand>(this.Handle);
	Receive<InputCommandRequest>(this.Handle);
	Receive<EscalateExceptionCommand>(this.Handle);
	Receive<RestartExceptionCommand>(this.Handle);
	Receive<StopExceptionCommand>(this.Handle);
	Receive<ResumeExceptionCommand>(this.Handle);
	Receive<PauseCommandHandlerCommand>(message =>
	{
		_writer.Tell("Going out of service...");
		Become(Paused);
		// BecomeStacked(Paused);
	});
}

private void Paused()
{
	Receive<ResumeCommandHandlerCommand>(message =>
	{
		_writer.Tell("Resuming operations...");
		Become(Running);
		// UnbecomeStacked();
	});
	// ReceiveAny must always be the last one!
	ReceiveAny((message) =>
	{
		ColoredConsole.WriteLineYellow($"Out of service: {message}");
	});
}
```

### Change ConsoleReaderActor to emit pause/resume commands

Change the `ConsoleReaderActor` to emit the new commands, add this code to the OnReceive():

```csharp
private const string PauseCommandHandlerCommand = "pausecommandhandler";
private const string ResumeCommandHandlerCommand = "resumecommandhandler";

// ...

case PauseCommandHandlerCommand:
	_commandHandler.Tell(new PauseCommandHandlerCommand());
	break;
case ResumeCommandHandlerCommand:
	_commandHandler.Tell(new ResumeCommandHandlerCommand());
	break;
```

Now run the application and experiment with pause / resume.

### Stashing - do not loose messages

Now let's add a Stash so we can process the messages that arrive while the Actor is 'Out of Service' at a later time.

Make the `CommandHandlerActor` Implement the `IWithUnboundedStash` interface and stash / unstash the messages whenever we enter and exit the paused state.

We also override the PreRestart lifecycle hook to not loose messages if the Actor is restarted.

```csharp
internal class CommandHandlerActor : ReceiveActorWithLogs, IWithUnboundedStash
{
	public IStash Stash { get; set; }

	private void Paused()
	{
		Receive<ResumeCommandHandlerCommand>(message =>
		{
			_writer.Tell("Resuming operations...");
			Become(Running);
			// UnbecomeStacked();

			// Unstash all the messages
			Stash.UnstashAll();
		});
		// ReceiveAny must always be the last one!
		ReceiveAny((message) =>
		{
			ColoredConsole.WriteLineYellow($"Out of service: {message}");
			// Stash the messages for later processing
			Stash.Stash();
		});
	}

	protected override void PreRestart(Exception reason, object message)
	{
		// move stashed messages to the mailbox so they persist through restart
		Stash.UnstashAll();

		base.PreRestart(reason, message);
	}

// ... the rest of the actor remain unchanged
}
```

## Exercise 05 - Actor Lifecycle & Internal State

What about the Actor's Internal State ?

The Internal State and the Stash are ephemeral, they will be wiped out whenever the Actor will be Stopped, Killed or Restarted (a new Actor instance will be created, you can re-hydrate the internal state in the PreStart lifecycle hook).

As a sample you can:

- Store the last executed message text in an internal field.
- Create a DumpCommand to print out the last saved message.
- Try to Stop, Resume or Restart the Actor and see what happens to the state.

Try it yourself!

## Bonus

- As an exercise we can modify the ConsoleUiActor so that it '.Ask()' the ConsoleReaderActor for a list of all the allowed commands. We can use that information to display a proper menu.
