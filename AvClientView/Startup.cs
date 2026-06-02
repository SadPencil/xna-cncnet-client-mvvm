using System;
using System.Linq;

using Avalonia;
using Avalonia.Headless;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView;

public class Startup
{
    /// <summary>
    /// Starts the Avalonia application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="initServices">
    /// Optional factory that builds the ServiceProvider on a background thread.
    /// When null, the loading screen will stay visible until
    /// ViewConstants.ServiceProvider is assigned externally.
    /// </param>
    [STAThread]
    public static void Run(string[] args, Func<ServiceProvider>? initServices = null)
    {
        ViewConstants.InitializeServices = initServices;

        bool headless = args.Contains("--headless");

        BuildAvaloniaApp(headless)
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp(bool headless = false)
    {
        var app = AppBuilder.Configure<App>();

        Avalonia.Logging.Logger.Sink = new AvaloniaSerilogSink();

        if (headless)
            app = app.UseHeadless(new AvaloniaHeadlessPlatformOptions());
        else
            app = app.UsePlatformDetect();

        app = app.WithInterFont();

        return app;
    }
}
