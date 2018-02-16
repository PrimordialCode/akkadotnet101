# Unit 04

In this lesson we'll focus on Akka.Remote and Akka.Cluster.

## Key concepts

We are going to learn:

- How we 'deploy' actors remotely to an ActorSystem in another process.
- How to distribute the application in a cluster of processes (on different machines).

## Exercise 01 - Akka.Remote

The starting point is the application we came up with in the end of Unit 03.

Immagine it to be a VERY processing intensive and resource consuming application and that a single machine is not enough to handle the workload.

The solution: break it up in multiple services and distribute it among serveral machines!

In this exercise we will:

- Create a new ConsoleApp.Client Console project.
- Refactor the application: the actual ConsoleApp project will become a Shared library that will hold shared code and all the messages that will be exchanged between processes.
- Copy the current ActorSystem and Application initialization logic to this newly created ConsoleApp.Client project.
- Add a new ConsoleApp.Remote project and initialize another (empty) ActorSystem.
- Add and configure Akka.Remote to allow these applications to talk.
- Configure the Application to remotely deploy the CommandHandler worker actor(s).

### Create a new console project 'ConsoleApp.Client'

Create a new NET Core Console project named: `ConsoleApp.Client`.

Add Akka.net using NuGet:

```bat
Install-Package Akka
```

Move `hocon.cfg` file to the new project.

Move the `program.cs` initilization logic from the current ConsoleApp to this newly created project.

```csharp
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
```

Change the `ConsoleApp` project from 'console' to 'library' and add a reference to it.

### Create a new Console project 'ConsoleApp.Remote'

Create a new NET Core Console project named: `ConsoleApp.Remote`.

Add Akka.net using NuGet:

```bat
Install-Package Akka
```

Copy `hocon.cfg` file to the new project.

Initialize a new ActorSystem in program.cs file:

```csharp
static class Program
{
	static void Main(string[] args)
	{
		string hocon = System.IO.File.ReadAllText(@".\hocon.cfg");
		var config = ConfigurationFactory.ParseString(hocon); // .WithFallback(...);
		var actorSystem = ActorSystem.Create("ConsoleApp", config);
		ColoredConsole.WriteLineGreen($"[{Assembly.GetExecutingAssembly().FullName}]");
		ColoredConsole.WriteLineGreen($"'{actorSystem.Name}' Actor System Created.");

		// actorSystem.Terminate();

		// wait for the actor system to terminate
		ColoredConsole.WriteLine("Awaiting for ActorSystem Termination.");
		actorSystem.WhenTerminated.Wait();
		ColoredConsole.WriteLine("ActorSystem Terminated.");

		ColoredConsole.WriteLine("Press 'enter' to exit.");
		Console.ReadLine();
	}
}
```

Add a reference to ConsoleApp project.

Run the applications and verify that everything works like before.

### Add Akka.Remote

Add the Akka.Remote NuGet packages to both the console applications:

```bat
Install-Package Akka.Remote
```

To enable Akka.Remote we'll switch the IActorRef provider with one that supports Remoting; we'll also configure a remoting transport.

Add the following HOCON configuration to ConsoleApp.Client (client):

```ini
akka {
  actor {
    # IActorRef provider with remoting support: Akka.Remote.RemoteActorRefProvider, Akka.Remote
    provider = remote
  }

  remote {
    dot-netty.tcp {
      port = 0 # bound to a dynamic port assigned by the OS
      hostname = localhost
    }
  }
}
```

Add the following HOCON configuration to ConsoleApp.Remote (server):

```ini
akka {
  actor {
    # IActorRef provider with remoting support: Akka.Remote.RemoteActorRefProvider, Akka.Remote
    provider = remote
  }

  remote {
    dot-netty.tcp {
      port = 8081 # bound to a dynamic port assigned by the OS
      hostname = localhost
    }
  }
}
```

We now need to tell the Client to remotely deploy the CommandHandler Actor.

Go to `ConsoleApp.Client` project and add an `akka/actor/deployment` section in the hocon.cfg files:

```ini
akka {
  actor {
    deployment {
      /ConsoleUi/CommandHandler {
        remote = "akka.tcp://ConsoleApp@localhost:8081"
      }
    }
  }
}
```

That's it! Run the solution: set multiple startup projects and verify that now the `CommandHandlerActor` will be deployed to the 'remote' process.

### ConsistentHashingPool Router - Deploy routees remotely

Switch back the `ConsoleUiActor` to use the CommandHandler based on the ConsistentHashingPool Router instead of our custom Client-per-Entity implementation and see the effect.

Messages will not be delivered... and you'll probably end up with an error like:

```bat
_Message [InputCommand] must be handled by hashMapping, or implement [IConsistentHashable] or be wrapped in [ConsistentHashableEnvelope]_
```

This happens because the Props of the ConsistentHashingPool router configuration uses a lamdba function to define the hashing mechanism, and this is not allowed (Lamda functions are not serializable).

Let's change the `Command` base message class to implement `IConsistentHashable` to quickly solve the problem:

```csharp
/// <summary>
/// A sample base class that is used to implement Commands in a raw CQRS pattern.
/// </summary>
public abstract class Command : IConsistentHashable
{
	/// <summary>
	/// Context information attached to the message
	/// </summary>
	public CommandContext Context { get; private set; } = new CommandContext();

	public object ConsistentHashKey => Context.UserId;
}
```

Now run the application again and see it in action!

## Exercise 02 - Akka.Cluster

Build up your own cluster.

### Create a Seed Project

Let's add a project that will host a bare ActorSystem instance.

Create a new .NET Core Console Application project and install the following NuGet packages:

```bat
Install-Package Akka
Install-Package Akka.Remote
Install-Package Akka.Cluster
```

Add the code to initialize the ActorSystem to the `program.cs` file:

```csharp
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
```

add an `hocon.cfg` file (and set the property 'Copy to Output Directory' to 'Copy if Newer'):

```ini
akka-seed {
  actorsystem = "ConsoleApp"
}

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
    # IActorRef provider with clustering support: Akka.Cluster.ClusterActorRefProvider, Akka.Cluster
    provider = cluster
  }

  remote {
    dot-netty.tcp {
      port = 8080
      hostname = localhost
    }
  }

  cluster {
    seed-nodes = ["akka.tcp://ConsoleApp@localhost:8080"]
    auto-down-unreachable-after = 30s # dangerous in production: can cause split brain!
  }
}
```

### Configure the other projects to join the Cluster

Install Akka.Cluster using NuGet:

```bat
Install-Package Akka.Cluster
```

Make sure all the projects share the same ActorSystem name.

Change the configuration to enable cluster support:

- the actor ref provider inside the the `akka/actor` section should be: `provider = cluster`
- add an `akka/cluster` section, specifying the seed nodes:

```ini
cluster {
  seed-nodes = ["akka.tcp://ConsoleApp@localhost:8080"]
}
```

- there must be an Akka.Remote transport configured like before.

The whole config file for the 'ConsoleApp.Remote' project should now be:

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
    # IActorRef provider with clustering support: Akka.Cluster.ClusterActorRefProvider, Akka.Cluster
    provider = cluster
  }

  remote {
    dot-netty.tcp {
      port = 8081
      hostname = localhost
    }
  }

  cluster {
    seed-nodes = ["akka.tcp://ConsoleApp@localhost:8080"]
  }
}
```

Similar changes should be made to the 'ConsoleApp.Client' project.

If we launch the application now we see welcome messages:

AkkaSeed:

```console
[INFO][19/11/2017 08:16:20][Thread 0003][[akka://ConsoleApp/system/cluster/core/daemon#141987411]] Node [akka.tcp://ConsoleApp@localhost:8080] is JOINING, roles []
[INFO][19/11/2017 08:16:20][Thread 0003][[akka://ConsoleApp/system/cluster/core/daemon#141987411]] Leader is moving node [akka.tcp://ConsoleApp@localhost:8080] to [Up]
[INFO][19/11/2017 08:16:20][Thread 0017][[akka://ConsoleApp/system/cluster/core/daemon#141987411]] Node [akka.tcp://ConsoleApp@localhost:8081] is JOINING, roles []
[INFO][19/11/2017 08:16:20][Thread 0017][[akka://ConsoleApp/system/cluster/core/daemon#141987411]] Node [akka.tcp://ConsoleApp@localhost:50400] is JOINING, roles []
[INFO][19/11/2017 08:16:20][Thread 0006][[akka://ConsoleApp/system/cluster/core/daemon#141987411]] Leader is moving node [akka.tcp://ConsoleApp@localhost:8081] to [Up]
[INFO][19/11/2017 08:16:20][Thread 0006][[akka://ConsoleApp/system/cluster/core/daemon#141987411]] Leader is moving node [akka.tcp://ConsoleApp@localhost:50400] to [Up]
```

Application Projects:

```console
[INFO][19/11/2017 08:16:20][Thread 0005][[akka://ConsoleApp/system/cluster/core/daemon#1514803599]] Welcome from [akka.tcp://ConsoleApp@localhost:8080]
```

But everything still works like before, while we want to distribute the work among all the nodes of the cluster.

### Cluster Enabled Routers

One quick way we can take advange of being in a Cluster is to use Routers to handle the workload.

Let's switch our sample to use a ConsistentHashingPool router, follow the last section of the previous sample to use the router in a distributed environment:

Change the `Command` base class to implement the `IConsistentHashable` interface:

```csharp
/// <summary>
/// A sample base class that is used to implement Commands in a raw CQRS pattern.
/// </summary>
public abstract class Command : IConsistentHashable
{
	/// <summary>
	/// Context information attached to the message
	/// </summary>
	public CommandContext Context { get; private set; } = new CommandContext();

	public object ConsistentHashKey => Context.UserId;
}
```

Change the `ConsoleUiActor` to use the CommandHandler based on the router, replace:

```csharp
var commandHandler = Context.ActorOf(Props.Create(() => new CommandHandlerCoordinatorActor(writer)), "CommandHandler");
```

with:

```csharp
var commandHandler = Context.ActorOf(CommandHandlerActorConfig.ConsistentHashingRouterProps(writer), "CommandHandler");
```

### Cluster Enabled ConsistentHashPool Router

Now We need to configure the router to take advantage of the cluster, so change the `ConsoleApp.Client` HOCON file like this:

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
    # IActorRef provider with clustering support: Akka.Cluster.ClusterActorRefProvider, Akka.Cluster
    provider = cluster
    deployment {
      /ConsoleUi/CommandHandler {
        # remote = "akka.tcp://ConsoleApp@localhost:8081"
        router = consistent-hashing-pool
        nr-of-instances = 10 # max number of total routees
        cluster {
          enabled = on
          allow-local-routees = on
          # use-role = name
          max-nr-of-instances-per-node = 5
        }
      }
    }
  }

  remote {
    dot-netty.tcp {
      port = 0 # bound to a dynamic port assigned by the OS
      hostname = localhost
    }
  }

  cluster {
    seed-nodes = ["akka.tcp://ConsoleApp@localhost:8080"]
  }
}
```

If we run the sample now it should start working, BUT we have some errors displayed:

```bat
[ERROR][19/11/2017 08:53:08][Thread 0003][akka.tcp://ConsoleApp@localhost:8080/remote/akka.tcp/ConsoleApp@localhost:51081/user/ConsoleUi/CommandHandler/c7] Error while creating actor instance of type Akka.Actor.ActorBase with 1 args: ([akka.tcp://ConsoleApp@localhost:51081/user/ConsoleUi/ConsoleWriter#2036119908])
Cause: [akka.tcp://ConsoleApp@localhost:8080/remote/akka.tcp/ConsoleApp@localhost:51081/user/ConsoleUi/CommandHandler/c7#608338592]: Akka.Actor.ActorInitializationException: Exception during creation ---> System.TypeLoadException: Error while creating actor instance of type Akka.Actor.ActorBase with 1 args: ([akka.tcp://ConsoleApp@localhost:51081/user/ConsoleUi/ConsoleWriter#2036119908]) ---> System.InvalidOperationException: No actor producer specified!
   at Akka.Actor.Props.DefaultProducer.Produce()
   at Akka.Actor.Props.NewActor()
   --- End of inner exception stack trace ---
   at Akka.Actor.Props.NewActor()
   at Akka.Actor.ActorCell.CreateNewActorInstance()
   at Akka.Actor.ActorCell.<>c__DisplayClass109_0.<NewActor>b__0()
   at Akka.Actor.ActorCell.UseThreadContext(Action action)
   at Akka.Actor.ActorCell.NewActor()
   at Akka.Actor.ActorCell.Create(Exception failure)
   --- End of inner exception stack trace ---
   at Akka.Actor.ActorCell.Create(Exception failure)
   at Akka.Actor.ActorCell.SysMsgInvokeAll(EarliestFirstSystemMessageList messages, Int32 currentState)
```

This happens because all the nodes, even the seeds, partecipate in the cluster and are valid 'targets' on which deploy new actors; but the seed project does NOT have a reference to the shared library class: they exists only as an entry point for new nodes.

The solutions:

- Make the Seed an active part of the application by referencing the libraries (not a good idea).
- Use the **roles** to specify which nodes are valid targets to deploy the actors.

Let's change the configuration and add the roles:

- In the `ConsoleApp.Client` and `ConsoleApp.Remote` HOCON configuration files, inside the 'cluster' section add the roles settings:

```ini
cluster {
  seed-nodes = ["akka.tcp://ConsoleApp@localhost:8080"]
  roles = ["worker"]
}
```

- In the `ConsoleApp.Client` HOCON configuration file, change the deployment configuration of the router adding a 'use-role' setting:

```ini
deployment {
  /ConsoleUi/CommandHandler {
    # remote = "akka.tcp://ConsoleApp@localhost:8081"
    router = consistent-hashing-pool
    nr-of-instances = 10 # max number of total routees
    cluster {
      enabled = on
      allow-local-routees = on
      # use-role = name
      max-nr-of-instances-per-node = 5
      use-role = "worker"
    }
  }
}
```

Now run the application again and make some experiment adding and removing nodes and altering the router configuration!