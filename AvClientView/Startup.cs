using System;
using System.Linq;

using Avalonia;
using Avalonia.Headless;

using AvClientMvvmContract.Generic;
using AvClientMvvmContract.ViewServices;

using AvClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView;

public static class Startup
{
    internal static Func<ServiceProvider> InitializeServices { get; private set; } = null!;

    /// <summary>
    /// Initial ViewModel set by the composition root (AvClientExe) before
    /// the window appears. Provides the WindowState (FullScreen/Normal)
    /// from the first frame, before DI is ready.
    /// </summary>
    internal static IMainWindowViewModel? InitialViewModel { get; private set; }

    private static UrlService UrlService => field ??= new UrlService();
    internal static IniLayoutOverlayService IniLayoutOverlayService => field ??= new IniLayoutOverlayService(UrlService);

    [STAThread]
    public static void Run(string[] args, Func<ServiceProvider> initServices, IMainWindowViewModel? initialViewModel = null)
    {
        InitialViewModel = initialViewModel;
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

    public static void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton(UrlService);
        services.AddTransient<IUrlService, UrlService>();

        services.AddSingleton(IniLayoutOverlayService);
        services.AddTransient<IIniLayoutOverlayService, IniLayoutOverlayService>();

        services.AddSingleton<IUIThreadMarshaller, AvaloniaUIThreadMarshaller>();
        services.AddSingleton<IClientSoundService, ClientSoundService>();        
        services.AddSingleton<IApplicationLifecycleService, ApplicationLifecycleService>();
    }
}
