using System;
using System.Text;

using Avalonia;

using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView;

public class Startup
{
    [STAThread]
    public static void Run(ServiceProvider serviceProvider, string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        App.ServiceProvider = serviceProvider;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
