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
            // Write trace (including Avalonia binding errors) to a file
            string tracePath = Path.GetTempFileName();
            var writer = new StreamWriter(tracePath, append: false, encoding: Encoding.UTF8);
            Trace.Listeners.Add(new TextWriterTraceListener(writer));
            Trace.AutoFlush = true;
            Console.Error.WriteLine($"[AV] Trace path: {tracePath}");
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
