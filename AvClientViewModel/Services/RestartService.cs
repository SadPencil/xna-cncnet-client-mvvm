#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.Versioning;

using AvClientMvvmContract.ViewServices;

using ClientCore;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Services;

/// <summary>
/// Handles client restart, with optional admin elevation.
/// Consolidates restart logic that was split between MainMenuViewModel and AdminRestarter.
/// </summary>
public class RestartService : IRestartService
{
    private readonly IApplicationLifecycleService lifecycleService;

    public RestartService(IApplicationLifecycleService lifecycleService)
    {
        this.lifecycleService = lifecycleService;
    }

    public void RestartClient()
    {
        LaunchProcess(admin: false);
    }

    public void RestartAsAdmin()
    {
        LaunchProcess(admin: true);
    }

    public void Shutdown()
    {
        lifecycleService.Shutdown();
    }

    private static void LaunchProcess(bool admin)
    {
#if NETFRAMEWORK
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = ProgramConstants.StartupExecutable,
            Verb = admin ? "runas" : null,
            UseShellExecute = admin,
        });
#else
        if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.LauncherExe),
                Arguments = "-NET8 -Av",
                Verb = admin ? "runas" : null,
                UseShellExecute = admin,
            });
        }
        else
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Environment.ProcessPath!,
                Arguments = Environment.CommandLine
            });
        }
#endif
    }
}
