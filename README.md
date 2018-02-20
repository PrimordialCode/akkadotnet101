# Introduction 

Introductory Course on [Akka.Net](http://getakka.net/).

The Course is organized in 4 Units that will guide you through all the basic features of Akka.Net starting from the Actor's creation to the setup of a simple cluster.

We'll create a very simple application made of several Actors that using a Console UI for Inputs and Output will allow the user to issue some commands that will be elaborated by some processing actors.

The pattern adopted can be used to implement a simple CQRS application, that maps well with concepts coming from the DDD and the Actor Model worlds.

# Getting Started

The whole application is uses .NET Core 2.0.

[Unit 01](src/Unit01/Unit01.md) - Start and Stop Actors
[Unit 02](src/Unit02/Unit02.md) - Actor's Lifecycle, Supervision Strategies, Behavior and Stashing
[Unit 03](src/Unit03/Unit03.md) - HOCON, Routing, Child per Entity Pattern: implement a simple CQRS application
[Unit 04](src/Unit03/Unit04.md) - Remoting and Clustering
