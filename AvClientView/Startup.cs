using System;
using System.Linq;

using Avalonia;
using Avalonia.Headless;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView;

public class Startup
{
    internal static Func<ServiceProvider> InitializeServices { get; private set; } = null!;

    [STAThread]
    public static void Run(string[] args, Func<ServiceProvider> initServices)
    {
        InitializeServices = initServices;

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
