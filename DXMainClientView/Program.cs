using System;
using System.Text;
using Avalonia;
using DXMainClientView.Services;
using DXMainClientViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Non-UI initialization (ViewModel project)
        var services = PreStartup.Initialize();

        // GUI-related services only (View project)
        services.AddSingleton<IUIThreadMarshaller, AvaloniaUIThreadMarshaller>();
        services.AddSingleton<IIniLayoutOverlayService, IniLayoutOverlayService>();

        App.ServiceProvider = services.BuildServiceProvider();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
