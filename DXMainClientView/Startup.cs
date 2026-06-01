using System;
using System.Text;

using Avalonia;
using Avalonia.Headless;

using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView;

public class Startup
{
    [STAThread]
    public static void Run(ServiceProvider serviceProvider, string[] args)
    {
        // TODO: move it to view model
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        ViewConstants.ServiceProvider = serviceProvider;

        bool headless = args.Contains("--headless");

        BuildAvaloniaApp(headless)
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp(bool headless = false)
    {
        var app = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont().LogToTrace();

        if (headless)
            app = app.UseHeadless(new AvaloniaHeadlessPlatformOptions { });

        return app;
    }
}
