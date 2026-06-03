using System;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientExe;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AvClientViewModel.PreStartup.Initialize();

        // The Avalonia window starts immediately so the loading screen appears
        // as early as possible. PreStartup.Initialize() and ServiceProvider
        // building run on a background thread, triggered from MainWindow
        // after the loading screen is visible.
        AvClientView.Startup.Run(args, () => BuildServiceProvider(args));
    }

    private static ServiceProvider BuildServiceProvider(string[] args)
    {
        AvClientViewModel.Startup.Initialize(args);

        var services = new ServiceCollection();
        AvClientView.Startup.ConfigureServices(services);
        AvClientViewModel.Startup.ConfigureServices(services);

        return services.BuildServiceProvider();
    }
}
