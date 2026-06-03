using System;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientExe;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // The Avalonia window starts immediately so the loading screen appears
        // as early as possible. PreStartup.Initialize() and ServiceProvider
        // building run on a background thread, triggered from MainWindow
        // after the loading screen is visible.
        AvClientView.Startup.Run(args, BuildServiceProvider);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        AvClientViewModel.PreStartup.Initialize();

        var services = new ServiceCollection();
        AvClientView.PreStartup.ConfigureServices(services);
        AvClientViewModel.PreStartup.ConfigureServices(services);

        return services.BuildServiceProvider();
    }
}
