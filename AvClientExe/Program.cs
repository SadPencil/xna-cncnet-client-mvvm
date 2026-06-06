using System;

using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientExe;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AvClientViewModel.PreStartup.Initialize();

        // Create the MainWindowViewModel before the window appears so
        // WindowState (FullScreen from BorderlessWindowedClient) is
        // applied from the first frame. Replaced by the DI-created
        // instance once the service provider is ready.
        var mainWindowViewModel = new MainWindowViewModel(gameInProgressViewModel: null);

        AvClientView.Startup.Run(args, () => BuildServiceProvider(args), mainWindowViewModel);
    }

    private static ServiceProvider BuildServiceProvider(string[] args)
    {
        AvClientViewModel.Startup.Initialize(args);

        var services = new ServiceCollection();
        AvClientView.Startup.ConfigureServices(services);
        AvClientViewModel.Startup.ConfigureServices(services);

        var provider = services.BuildServiceProvider();

        // Register View project's compile-time L10N strings for translation stub generation
        provider.GetRequiredService<ITranslationNotifierService>().Register();

        return provider;
    }
}
