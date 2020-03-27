# MSTK
A toolkit for setting up hosts, disovery hub, event hub, monitoring hub, and API gateway for the purpose of on-premise Microservice architectures.

## Background

MSTK was developed primarily for the purpose of being a demo platform during my Microservice community sessions.
Describing Microservice concepts like host isolation, redundancy, discoverability, and others really required something to show that attendees and viewers could use to get their head around what can be some prety abstract and complex topics. MSTK is the manifestation of everything I discuss on stage and online. 
###
MSTK is a simplified version of a hosting platform developed for a client which among many other things, allows the hosting of different technologies (ASP Core, Web API, WCF, Nancy, etc) in a modular fashion. 

## Installation

Either clone or download the contents of the 'src' folder and open the solution with Visual Studio. The binaries are not on this repository so you will need to compile the solution.
###
The class libraries are all .NET Standard 2.0 projects and can be consumed by any kind of project. The console applications used for executable are all .NET Core console applicaitons.

## The Projects

MSTK.Core
###
MSTK.Core.Abstractions
###
MSTK.Discovery
###
MSTK.Discovery.ConsoleHost
###
MSTK.Eventing
###
MSTK.Eventing.ConsoleHost
###
MSTK.Gateway
###
MSTK.Gateway.JSClient
###
MSTK.Hosting
###
MSTK.Hosting.ConsoleHost1
###
MSTK.Hosting.ConsoleHost2
###
MSTK.Monitor
###
MSTK.Monitor.ConsoleHost
###
MSTK.SampleServices1
###
MSTK.SampleServices2
###
MSTK.SDK
###
MSTK.TestClient
###

in progresss...

## Running the Demo

The code in this repository is ready to run as is as a full featured demo.
###
The excutables that should be run are all the ConsoleHost projects.
###

in progress...
