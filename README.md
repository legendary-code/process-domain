#ProcessDomain

ProcessDomain implements a remoting solution for creating out-of-process AppDomains. It's written in C# and the assemblies will work with .NET Framework 2.0. Some possible usages include:
* Further code isolation
* Allow for native code to run with different environment variables
* Allow for multiple versions of a native library to be loaded without writing separate applications. E.g. A managed application that uses native database drivers, but needs to support loading multiple versions so that connectivity can occur for multiple versions of the database.

See the example project for some usages

#Features

* AppDomain-like semantics, so its usage is familiar and easy
* Implements the IDisposable pattern
* Event handlers for when the remote process exits or is restarted
* Automatic restart of remote process supported
* Remote process assembly is generated on-the-fly at runtime
* Remote process' AppDomain is fully configurable and by default takes on the settings of the AppDomain creating the ProcessDomain

#Installation

As of version 1.8, ProcessDomain will be available via NuGet as well: https://nuget.org/packages/ProcessDomain/1.8

#Requirements

###To Build Source

Visual Studio 2013
[MSBuild Community Tasks 1.3](http://msbuildtasks.tigris.org/MSBuild.Community.Tasks.Nightly.msi)

###To Use Assemblies
.NET Framework 2.0
