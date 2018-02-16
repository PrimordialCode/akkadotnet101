# Unit 01

In this lesson, we'll make our first actors and introduce to the fundamentals of [Akka.NET](http://getakka.net/).

## Key concepts

We are going to learn:

- How to install Akka.net.
- How to create an Actor System.
- How to terminate an Actor System.
- Create three Actors.
- We'll make them communicate exchanging messages.

### What is an Actor?

It's an entity, a business object, that can do things and communicate, actors come in Systems.

An actor can:

1. Have internal state.
1. Receive and process messages.
1. Create other actors.
1. Send messages to other actors (such as the `Sender` of the current message).
1. Change its own behavior and process the next message it receives differently.

Actors are inherently asynchronous.

### What is an `ActorSystem`?

An `ActorSystem` is a reference to the underlying system and Akka.NET framework.
You'll need to create your first actors from the context of this `ActorSystem`.
All actors live within the context of their Actor System.

The `ActorSystem` is a heavy object: create only one Actor System per application.

## Exercise 01 - Startup

Create a new .NET Core project and...

### Install Akka.net

```bat
Install-Package Akka
```

### Create the ActorSystem

go to `program.cs` and type the following

```csharp
var actorSystem = ActorSystem.Create("ConsoleApp");
Console.WriteLine($"{actorSystem.Name} Actor System Created.");
```

### Terminate an ActorSystem

The actor system shutdown is an asynchronous operation that can take some time:

```csharp
actorSystem.Terminate();

// wait for the actor system to terminate
Console.WriteLine("Awaiting for ActorSystem Termination.");
actorSystem.WhenTerminated.Wait();
Console.WriteLine("ActorSystem Terminated.");
```

## Exercise 02 - Actors

Open up the `ConsoleApp` solution: it's a simple .NET Core console application that contains some files used as starting point of the demo.

### Our first `UntypedActor`

The solution has four incomplete actors, their skeleton looks like this:

```csharp
internal class ConsoleUiActor : UntypedActor
{
	protected override void OnReceive(object message)
	{
		if (... decide if handle the message ...)
		{
			// do something
		}
		else
		{
			// call unhandled for any message you are not going to deal with.
			Unhandled(msg);
		}
	}
}
```

The OnReceive() will be called by the actor framework infrastructure passing in the messages sent to the actor, one at a time.
It's your responsibility to call the any Unhandled() for any message the Actor is not going to handle.

[Take a look at the aggregates: ConsoleWriterActor, ConsoleReaderActor, ConsoleUiActor]

### Create an Actor instance

To create an instance of the actor: go to `program.cs` and replace

```csharp
actorSystem.Terminate();
```

with

```csharp
IActorRef consoleUi = actorSystem.ActorOf(Props.Create<ConsoleUiActor>(), "ConsoleUi");
// consoleUi.Tell("start");
```

Here we've asked the actor system to create an instance of the actor, you can see some concepts at work:

- assign a name to the Actor.
- Props...  a recipe for creating an actor, a way to specify some properties that must be used when creating an instance of this actor.
- we got back an... IActorRef.

Now open up the `ConsoleUiActor` and handling the 'start' message create instance of the other three actors:

```csharp
// create the writer and the reader
var writer = Context.ActorOf<ConsoleWriterActor>("ConsoleWriter");
var commandHandler = Context.ActorOf(Props.Create(() => new CommandHandlerActor(writer)), "CommandHandler");
var reader = Context.ActorOf(Props.Create<ConsoleReaderActor>(this.Self, commandHandler), "ConsoleReader");

// tell the reader actor to start reading
// reader.Tell("readnext");
```

### How Actors Communicate: Messages

Now take a look at how messages between Actors are defined, we've decided to use:

- string (any value type is good).
- POCO classes.

Messages need to be immutable in order to properly guarantee thread safety and lock free code:

```csharp
public class InputCommand
{
	public string Data { get; }

	public InputCommand(string data)
	{
		Data = data;
	}
}

public class CommandCompleted
{
	public string Data { get; }

	public CommandCompleted(string data)
	{
		Data = data;
	}
}
```

### How Actors Communicate: Tell (Non Blocking)

Akka Actors communicate with each others with the non-blocking `.Tell()` method.

It follows the **fire and forget** comunication pattern.

Go to the `program.cs` file and tell the `ConsoleUiActor` to start:

```csharp
IActorRef consoleUi = actorSystem.ActorOf(Props.Create<ConsoleUiActor>(), "ConsoleUi");
consoleUi.Tell("start");
```

In the `ConsoleUiActor` when handling the `start` message add the following code after having created the actors (it will tell the `ConsoleReaderActor` to `readnext` message):

```csharp
// tell the reader actor to start reading
reader.Tell("readnext");
```

Also take a look at the `ConsoleReaderActor`'s OnReceive function:

```csharp
var text = Console.ReadLine();
switch (text)
{
	case "quit":
		_consoleUi.Tell("terminate");
		return;
	default:
		_commandHandler.Tell(new InputCommand(text));
		break;
}
// send a message to outself stating we are ready to read the next command
Self.Tell("readnext");
```

Here you can also see an Actor sending a message to itself.

Compile and run the application to see how it behave!

### How Actors Communicate: Ask (Blocking)

What if we want some sort of acknowledgement that a message has been received or an elaboration complete notification in response to a direct message ?

Use the blocking `.Ask()` method and await on the `Task` it returns.

It follows the **request / response** communication pattern.

Add these message classes:

```csharp
public class InputCommandRequest
{
	public string Data { get; }

	public InputCommandRequest(string data)
	{
		Data = data;
	}
}

public class InputCommandResponse
{
	public string Data { get; }

	public InputCommandResponse(string data)
	{
		Data = data;
	}
}
```

In the `ConsoleReaderActor` replace the .Tell() with the following code:

```csharp
ColoredConsole.WriteLineYellow("Awaiting for the command to complete");
var completedTask = _commandHandler.Ask<InputCommandResponse>(new InputCommandRequest(text));
completedTask.Wait(); // blocking operation: wait for completion
ColoredConsole.WriteLineYellow($"Response: {completedTask.Result.Data}");
ColoredConsole.WriteLineYellow("The command has been completed");
```

Ask() is a blocking operation and it's not recommended.

In the `CommandHandlerActor` you need to reply to the Sender in order for the Task to complete:

```csharp
if (message is InputCommandRequest)
{
	var inputCommand = message as InputCommandRequest;

	// do something heavy: it will block the Actor
	Thread.Sleep(1000);

	// also inform the Sender that the command has been completed
	Sender.Tell(new InputCommandResponse(inputCommand.Data));
}
```

## Exercise 03 - How to stop Actors

Comment the `.Ask()` method call in `ConsoleReaderActor`, maybe raise the Thread.Sleep interval a bit so you can fire more than one command before the previous one have completed.
Try to send some more messages and `quit` the application before the last message has been completed.

You'll see an output telling you that some messages has been sent to `DeadLetter`.

This mean that the CommandHandlerActor was stopped before it could have a chance to process all the message.

So here is the question: how to stop an Actor?

Let's add some commands we can use to stop the `CommandHandlerActor`.

In `ConsoleReaderActor` add the following commands:

```csharp
private const string StopCommand = "stop";
private const string PoisonPillCommand = "poisonpill";
private const string KillCommand = "kill";
private const string GracefulStopCommand = "gracefulstop";
```

And Handle them in the OnReceive() function; place the following code inside the switch() instruction:

```csharp
case StopCommand:
	// Send a System Stop message to the specified actor (the message will have priority over others already in the mailbox).
	// Actor receives the Stop message and suspends the actor’s Mailbox (other messages will go to DeadLetters).
	// Actor tells all its children to Stop. Stop messages propagate down the hierarchy below the actor.
	// Actor waits for all children to stop.
	// Actor calls PostStop lifecycle hook method for resource cleanup.
	// Actor shuts down.
	Context.Stop(_commandHandler);
	break;
case PoisonPillCommand:
	// Use a PoisonPill message if you want the actor to process its mailbox before shutting down.
	// PoisonPill is a message that will be placed in the mailbox.
	// When the Actor process the message the above mentioned stop sequence will be initiated.
	_commandHandler.Tell(PoisonPill.Instance);
	break;
case KillCommand:
	// Use a Kill message if you want it to show in your logs that the actor was killed.
	// Send a System Kill message to the specified actor.
	// The actor throws an ActorKilledException (The actor’s supervisor logs this message).
	// This suspends the actor mailbox from processing further user messages.
	// The actor’s supervisor handles the ActorKilledException and issues a Stop directive.
	// The actor will stop following the above mentioned stop sequence.
	_commandHandler.Tell(Kill.Instance);
	break;
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
	break;
```

Now you can run the application send those commands to see what happens when you shutdown the `CommandHandlerActor`.