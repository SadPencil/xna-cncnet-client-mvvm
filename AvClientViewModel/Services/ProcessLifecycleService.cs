#nullable enable
using System;
using System.Diagnostics;

using AvClientMvvmContract.ViewServices;

using ClientCore;

using Rampastring.Tools;

namespace AvClientViewModel.Services;

/// <summary>
/// Manages the client application process lifecycle.
/// Launches a new instance and / or terminates the current one on the UI thread.
/// </summary>
public class ProcessLifecycleService : IProcessLifecycleService
{
    private readonly IApplicationLifecycleService lifecycleService;
    private readonly IUIThreadMarshaller uiThreadMarshaller;

    public ProcessLifecycleService(
        IApplicationLifecycleService lifecycleService,
        IUIThreadMarshaller uiThreadMarshaller)
    {
        this.lifecycleService = lifecycleService;
        this.uiThreadMarshaller = uiThreadMarshaller;
    }

    public void Restart()
    {
        LaunchProcess(admin: false);
        uiThreadMarshaller.AddCallback(lifecycleService.Shutdown);
    }

    public void RestartAsAdmin()
    {
        LaunchProcess(admin: true);
        uiThreadMarshaller.AddCallback(lifecycleService.Shutdown);
    }

    public void Shutdown()
    {
        uiThreadMarshaller.AddCallback(lifecycleService.Shutdown);
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
