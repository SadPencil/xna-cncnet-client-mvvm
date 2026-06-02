using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using Avalonia;
using Avalonia.Headless;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView;

public class Startup
{
    [STAThread]
    public static void Run(ServiceProvider serviceProvider, string[] args)
    {
        ViewConstants.ServiceProvider = serviceProvider;

        bool headless = args.Contains("--headless");

        if (headless)
        {
            string logFile = Path.Combine(AppContext.BaseDirectory, "av_bindings.log");
            Trace.Listeners.Add(new TextWriterTraceListener(logFile));
            Trace.AutoFlush = true;
        }

        BuildAvaloniaApp(headless)
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp(bool headless = false)
    {
        var app = AppBuilder.Configure<App>();

        if (headless)
            app = app.UseHeadless(new AvaloniaHeadlessPlatformOptions());
        else
            app = app.UsePlatformDetect();

        app = app.WithInterFont().LogToTrace();

        return app;
    }
}
