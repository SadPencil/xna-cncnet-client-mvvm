using AvClientMvvmContract.ViewServices;

using AvClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView;

public static class PreStartup
{
    public static void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<IUIThreadMarshaller, AvaloniaUIThreadMarshaller>();
        services.AddSingleton<IIniLayoutOverlayService, IniLayoutOverlayService>();
        services.AddSingleton<IClientSoundService, ClientSoundService>();
        services.AddSingleton<IUrlService, UrlService>();
        services.AddSingleton<IApplicationLifecycleService, ApplicationLifecycleService>();
    }
}
