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
public class ViewModelLifecycleService : IViewModelLifecycleService
{
    private readonly IViewLifecycleService viewLifecycleService;
    private readonly IUIThreadMarshaller uiThreadMarshaller;

    public ViewModelLifecycleService(
        IViewLifecycleService viewLifecycleService,
        IUIThreadMarshaller uiThreadMarshaller)
    {
        this.viewLifecycleService = viewLifecycleService;
        this.uiThreadMarshaller = uiThreadMarshaller;
    }

    public void Restart()
    {
        LaunchProcess(admin: false);
        uiThreadMarshaller.AddCallback(viewLifecycleService.Shutdown);
    }

    public void RestartAsAdmin()
    {
        LaunchProcess(admin: true);
        uiThreadMarshaller.AddCallback(viewLifecycleService.Shutdown);
    }

    public void Shutdown()
    {
        uiThreadMarshaller.AddCallback(viewLifecycleService.Shutdown);
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
