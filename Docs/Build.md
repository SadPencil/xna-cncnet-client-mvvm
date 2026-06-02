# Build & Publish #

The information below describes the steps that the default build script (build.ps1) performs.

TargetFramework configurations
------------------------------

The client supports the following TargetFrameworks:
* net8.0
* net4.8

Build output
------------

To build the client you should install the .NET 10.0 SDK, PowerShell 7 and run `Script\Build.bat`. Compiled result will be placed to `Compiled` folder in the root of the repository.

Building with Visual Studio
---------------------------

> [!IMPORTANT] 
> IDEs can build Release configurations, but they are forbidden to run due to compile-time optimizations on binaries.

> [!WARNING]
> After changing the solution configuration in Visual Studio you *have to* manually execute `dotnet restore` through cmd/powershell in the project's root directory to load packages and also reboot Visual Studio to exclude [NETSDK1004](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1004) and [NETSDK1005](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1005) errors.
