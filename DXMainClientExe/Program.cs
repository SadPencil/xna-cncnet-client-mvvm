using System;

using DXMainClientView.Services;

using DXMainClientViewModel;
using DXMainClientViewModel.Services;

using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientExe.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args) => DXMainClientView.Startup.Run(BuildServiceProvider(), args);

    private static ServiceProvider BuildServiceProvider()
    {
        // ViewModel project initializes domain services
        var services = DXMainClientViewModel.PreStartup.Initialize();

        // View project adds GUI-related services only
        services.AddSingleton<IUIThreadMarshaller, AvaloniaUIThreadMarshaller>();
        services.AddSingleton<IIniLayoutOverlayService, IniLayoutOverlayService>();
        services.AddSingleton<IClientSoundService, ClientSoundService>();
        services.AddSingleton<IUrlService, UrlService>();
        services.AddSingleton<IApplicationLifecycleService, ApplicationLifecycleService>();

        return services.BuildServiceProvider();
    }

}
