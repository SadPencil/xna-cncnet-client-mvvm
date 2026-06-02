# Agent Instructions


## General information

### Project structure

| Path | Description |
|------|-------------|
| `AvClientExe/` | Avalonia entry-point executable — always the build target |
| `AvClientView/` | Avalonia UI layer (Views, AXAML, styles) |
| `AvClientViewModel/` | ViewModels, Models, Services (no UI dependencies) |
| `AvClientMvvmContract/` | Interfaces for View-ViewModel communication |
| `ClientCore/` | Core game-client logic |
| `ClientUpdater/` | Auto-updater logic |
| `SecondStageUpdater/` | Secondary updater executable |
| `DXMainClient/` | Legacy XNA entry-point (being phased out) |
| `ClientGUI/` | Legacy XNA UI layer (being phased out) |
| `Rampastring.XNAUI/` | UI framework (git submodule) |
| `GitVersion.yml` | GitVersion branch and versioning strategy |
| `global.json` | Pins the required .NET SDK version (10.0, any feature band) |
| `Directory.Build.props` | MSBuild properties shared across all projects |
| `Directory.Build.targets` | MSBuild targets (build events, resource copying) |
| `Directory.Packages.props` | Central NuGet package version management |
| `Docs/Build.md` | Human-oriented build documentation |

### Build the project

`SecondStageUpdater` is included as a `ProjectReference` in AvClientExe, so a simple `dotnet build` is sufficient:

```shell
dotnet build AvClientExe/AvClientExe.csproj -f net8.0
```

A successful build ends with `0 Error(s)`. The `BuildUpdater` MSBuild target in `Directory.Build.targets` handles copying the updater output during publish.

### Run the client in headless mode

The client can run without a display server using Avalonia's headless platform. This is useful for CI testing and verifying non-UI initialization.

```shell
dotnet run --project AvClientExe/AvClientExe.csproj -- --headless
```

Or directly with the built binary:

```shell
dotnet exec AvClientExe/bin/Debug/net8.0/clientav.dll -- --headless
```

The client runs an endless event loop in headless mode. Use `timeout` to stop it after a fixed duration:

```shell
timeout 10 dotnet exec AvClientExe/bin/Debug/net8.0/clientav.dll -- --headless
```

Logs are written to `Client/client.log` in the build output directory. Check for errors:

```shell
grep -i "error\|exception\|fail" AvClientExe/bin/Debug/net8.0/Client/client.log
```

### Contributing guidelines
See [Contributing.md](../Contributing.md) for coding style, formatting, and other contribution guidelines. Be aware, Copilot, you MUST read and follow this file, even if the user did not explicitly ask you to.

## GitHub Copilot coding agent setup instructions

This section only applies to the GitHub Copilot coding agent, running in a Linux runner from the GitHub Action environment. It does not apply to other environments, such as local development.

The steps in the [copilot-coding-agent-setup.md](./copilot-coding-agent-setup.md) file are automatically executed via a GitHub Action workflow before the agent starts. **Only read and run them manually if you encounter a build failure**.