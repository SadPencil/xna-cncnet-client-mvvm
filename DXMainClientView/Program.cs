using System;
using System.Text;
using Avalonia;
using DXMainClientView.Services;
using DXMainClientViewModel;
using DXMainClientViewModel.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // ViewModel project initializes domain services
        var services = PreStartup.Initialize();

        // View project adds GUI-related services only
        services.AddSingleton<IUIThreadMarshaller, AvaloniaUIThreadMarshaller>();
        services.AddSingleton<IIniLayoutOverlayService, IniLayoutOverlayService>();
        services.AddSingleton<IClientSoundService, ClientSoundService>();
        services.AddSingleton<IUrlService, UrlService>();
        services.AddSingleton<IApplicationLifecycleService, ApplicationLifecycleService>();

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
