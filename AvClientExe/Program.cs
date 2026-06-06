using System;

using AvClientViewModel.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientExe;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AvClientViewModel.PreStartup.Initialize();

        // Create the initial MainWindowViewModel before the window appears
        // so WindowState (FullScreen from BorderlessWindowedClient) is set
        // from the first frame. Replaced by the DI-created instance later.
        var initialVM = new MainWindowViewModel(gameInProgressVM: null);

        // The Avalonia window starts immediately so the loading screen appears
        // as early as possible. PreStartup.Initialize() and ServiceProvider
        // building run on a background thread, triggered from MainWindow
        // after the loading screen is visible.
        AvClientView.Startup.Run(args, () => BuildServiceProvider(args), initialVM);
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
