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
    /// Provided by the composition root (AvClientExe) before the window
    /// appears. The MainWindow sets this as its DataContext immediately
    /// so the correct WindowState is applied from the first frame.
    /// </summary>
    internal static IMainWindowViewModel? MainWindowViewModel { get; private set; }

    private static UrlService UrlService => field ??= new UrlService();
    internal static IniLayoutOverlayService IniLayoutOverlayService => field ??= new IniLayoutOverlayService(UrlService);

    [STAThread]
    public static void Run(string[] args, IMainWindowViewModel? mainWindowViewModel, Func<ServiceProvider> initServices)
    {
        MainWindowViewModel = mainWindowViewModel;
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
        services.AddSingleton<IViewLifecycleService, ViewLifecycleService>();
        services.AddSingleton<IViewTranslationNotifierService, ViewTranslationNotifierService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
    }
}
