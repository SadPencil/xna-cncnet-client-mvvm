using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Threading;
using DXMainClientView.Services;
using DXMainClientViewModel;
using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Generic;
using DXMainClientViewModel.Online;
using Microsoft.Extensions.DependencyInjection;
using Rampastring.Tools;

namespace DXMainClientView;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Set working directory to DXMainClient so ClientCore can find resources.
        SetWorkingDirectoryToGameRoot();

        Console.WriteLine("Avalonia client starting.");

        // Build DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Make service provider available to App
        App.ServiceProvider = serviceProvider;

        // Initialize the ViewModel (starts background tasks)
        var loadingScreenVM = serviceProvider.GetRequiredService<ILoadingScreenViewModel>();
        if (loadingScreenVM is LoadingScreenViewModel concrete)
        {
            Console.WriteLine("Initializing LoadingScreenViewModel...");
            concrete.Initialize();
            Console.WriteLine("LoadingScreenViewModel initialized successfully.");
        }

        // Start Avalonia
        Console.WriteLine("Starting Avalonia application...");
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // Core services
        services.AddSingleton<Random>(_ => new Random());
        services.AddSingleton<IUIThreadMarshaller, AvaloniaUIThreadMarshaller>();
        services.AddSingleton<IUpdateService, StubUpdateService>();

        // Domain services
        services.AddSingleton<GameCollection>();
        services.AddSingleton<CnCNetUserData>(_ => new CnCNetUserData(() => { }));
        services.AddSingleton<CnCNetManager>();
        services.AddSingleton<MapLoader>();

        // Layout services
        services.AddSingleton<IIniLayoutOverlayService, IniLayoutOverlayService>();

        // ViewModels
        services.AddTransient<ILoadingScreenViewModel, LoadingScreenViewModel>();
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Sets the working directory to the game root (where ClientDefinitions.ini lives).
    /// The original DXMainClient runs from a directory containing ClientDefinitions.ini,
    /// Resources/DTA/, etc. We need the same setup for ClientCore statics to work.
    /// </summary>
    private static void SetWorkingDirectoryToGameRoot()
    {
        // Walk up from the executable location looking for Resources/ClientDefinitions.ini.
        // The game root is the directory containing Resources/ClientDefinitions.ini.
        // ProgramConstants.GetBaseResourcePath() returns {GamePath}/Resources/.
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        while (dir != null)
        {
            // Check for Resources/ClientDefinitions.ini (the actual location)
            if (File.Exists(Path.Combine(dir.FullName, "Resources", "ClientDefinitions.ini")))
            {
                Directory.SetCurrentDirectory(dir.FullName);
                Console.WriteLine($"Working directory set to: {dir.FullName}");
                return;
            }

            dir = dir.Parent;
        }

        // Fallback: try the DXMainClient directory relative to the repo root
        dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            var dxMainClientDir = Path.Combine(dir.FullName, "DXMainClient");
            if (Directory.Exists(dxMainClientDir) &&
                File.Exists(Path.Combine(dxMainClientDir, "Resources", "ClientDefinitions.ini")))
            {
                Directory.SetCurrentDirectory(dxMainClientDir);
                Console.WriteLine($"Working directory set to DXMainClient: {dxMainClientDir}");
                return;
            }
            dir = dir.Parent;
        }

        Console.WriteLine("WARNING: Could not find game root directory. ClientCore initialization may fail.");
    }
}
