using System;

using AvClientView.Services;

using AvClientViewModel;
using AvClientViewModel.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientExe;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Note: we have a strict order of operations for startup:
        // 1. AvClientViewModel.PreStartup.Initialize()
        // 2. AvClientView.Startup.Run()
        // 3. AvClientViewModel.Startup.Initialize()
        // 4. AvClientView.Startup.ConfigureServices()
        // 5. AvClientViewModel.Startup.ConfigureServices()
        // 4 and 5 can be in either order.

        StartupParams viewModelStartupParams = new(args);
        AvClientViewModel.PreStartup.Initialize(viewModelStartupParams, new ViewTranslationNotifierService());

        // Create the MainWindowViewModel before the window appears so
        // WindowState (FullScreen from BorderlessWindowedClient) is
        // applied from the first frame. Replaced by the DI-created
        // instance once the service provider is ready.
        var mainWindowViewModel = new MainWindowViewModel(gameInProgressViewModel: null);

        AvClientView.Startup.Run(args, mainWindowViewModel, () =>
        {
            AvClientViewModel.Startup.Initialize(viewModelStartupParams);
            return BuildServiceProvider();
        });
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        AvClientView.Startup.ConfigureServices(services);
        AvClientViewModel.Startup.ConfigureServices(services);

        return services.BuildServiceProvider();
    }
}
