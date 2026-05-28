using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Threading;
using ClientCore;
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

        // Set RESOURCES_DIR to theme path (same as Startup.Execute()).
        // Without this, GetResourcePath() returns "Resources/" instead of
        // "Resources/Default Theme/" and theme-specific INI/textures aren't found.
        InitializeThemeResourcePath();

        Console.WriteLine($"Resource path: {ProgramConstants.GetResourcePath()}");
        Console.WriteLine($"Base resource path: {ProgramConstants.GetBaseResourcePath()}");

        // Run headless INI overlay test if requested
        if (args.Length > 0 && args[0] == "test-ini")
        {
            IniOverlayTest.Run();
            return;
        }

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

    /// <summary>
    /// Sets ProgramConstants.RESOURCES_DIR to the theme-specific resource path.
    /// This is normally done by Startup.Execute() but we need it for INI layout loading.
    /// </summary>
    private static void InitializeThemeResourcePath()
    {
        try
        {
            // Try reading theme from UserINISettings (if initialized)
            ProgramConstants.RESOURCES_DIR = SafePath.CombineDirectoryPath(
                ProgramConstants.BASE_RESOURCE_PATH,
                UserINISettings.Instance.ThemeFolderPath);
        }
        catch
        {
            // UserINISettings not initialized yet - read theme directly from INI files
            string themePath = ReadThemePathFromIni();
            ProgramConstants.RESOURCES_DIR = SafePath.CombineDirectoryPath(
                ProgramConstants.BASE_RESOURCE_PATH, themePath);
        }

        // Verify the theme directory exists; fall back to base Resources if not
        string resourcePath = ProgramConstants.GetResourcePath();
        if (!Directory.Exists(resourcePath))
        {
            Console.WriteLine($"Theme directory not found: {resourcePath}, falling back to base Resources");
            ProgramConstants.RESOURCES_DIR = ProgramConstants.BASE_RESOURCE_PATH;
        }
    }

    /// <summary>
    /// Reads the theme path directly from ClientDefinitions.ini and UserDefaults.ini
    /// without requiring UserINISettings to be initialized.
    /// </summary>
    private static string ReadThemePathFromIni()
    {
        // Read theme list from ClientDefinitions.ini
        string clientDefsPath = Path.Combine(ProgramConstants.GetBaseResourcePath(), "ClientDefinitions.ini");
        if (!File.Exists(clientDefsPath))
            return string.Empty;

        var clientDefs = new IniFile(clientDefsPath);
        var themesSection = clientDefs.GetSection("Themes");
        if (themesSection == null || themesSection.Keys.Count == 0)
            return string.Empty;

        // Get first theme as default
        string firstThemeEntry = themesSection.Keys[0].Value;
        string defaultThemeName = firstThemeEntry.Split(',')[0];
        string defaultThemePath = firstThemeEntry.Contains(',') ? firstThemeEntry.Split(',')[1] : string.Empty;

        // Check if user has a theme preference in UserDefaults.ini or User.ini
        string themeName = defaultThemeName;
        foreach (string userIniName in new[] { "UserDefaults.ini", "User.ini" })
        {
            string userIniPath = Path.Combine(ProgramConstants.GamePath, userIniName);
            if (File.Exists(userIniPath))
            {
                var userIni = new IniFile(userIniPath);
                string savedTheme = userIni.GetStringValue("MultiPlayer", "Theme", null);
                if (!string.IsNullOrEmpty(savedTheme))
                {
                    themeName = savedTheme;
                    break;
                }
            }
        }

        // Find the matching theme path
        foreach (var key in themesSection.Keys)
        {
            var parts = key.Value.Split(',');
            if (parts.Length >= 2 && parts[0] == themeName)
                return parts[1];
        }

        return defaultThemePath;
    }
}
