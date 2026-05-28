using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using ClientCore;
using ClientCore.I18N;
using DXMainClientView.Services;
using DXMainClientViewModel;
using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Generic;
using DXMainClientViewModel.Online;
using Microsoft.Extensions.DependencyInjection;
using Rampastring.Tools;

namespace DXMainClientView;

/// <summary>
/// Initializes client systems before the Avalonia UI starts.
/// Replicates the original DXMainClient PreStartup + Startup initialization chain.
/// </summary>
static class PreStartup
{
    /// <summary>
    /// Initializes all non-UI systems and returns the DI service provider.
    /// </summary>
    public static ServiceProvider Initialize()
    {
        // --- Culture (same as DXMainClient PreStartup) ---
        Translation.InitialUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(ProgramConstants.HARDCODED_LOCALE_CODE);

        IniFile.DisallowDesktopIni = true;

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            Logger.Log("Unhandled exception: " + args.ExceptionObject);

        // --- Working directory (same as DXMainClient PreStartup) ---
        DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);
        Environment.CurrentDirectory = gameDirectory.FullName;
        Logger.Log("Game path: " + ProgramConstants.GamePath);

        // --- Logger (same as DXMainClient PreStartup) ---
        DirectoryInfo clientUserFilesDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);
        FileInfo clientLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client.log");
        ProgramConstants.LogFileName = clientLogFile.FullName;

        if (clientLogFile.Exists)
        {
            FileInfo clientPrevLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client_previous.log");
            if (clientPrevLogFile.Exists)
                File.Delete(clientPrevLogFile.FullName);
            File.Move(clientLogFile.FullName, clientPrevLogFile.FullName);
        }

        Logger.Initialize(clientUserFilesDirectory.FullName, clientLogFile.Name);
        Logger.WriteLogFile = true;
        DXMainClientViewModel.Domain.MainClientConstants.LoggerInitialized = true;

        if (!clientUserFilesDirectory.Exists)
            clientUserFilesDirectory.Create();

        Logger.Log("***Logfile for " + DXMainClientViewModel.Domain.MainClientConstants.GAME_NAME_LONG + " client***");

        string clientVersion = GitVersionInformation.AssemblySemVer;
        Logger.Log("Client version: " + clientVersion);
        Logger.Log(GitVersionInformation.InformationalVersion);

        // --- Client configuration (same as DXMainClient PreStartup) ---
        DXMainClientViewModel.Domain.MainClientConstants.Initialize();

        Logger.Log("Loading settings.");
        UserINISettings.Initialize(ClientConfiguration.Instance.SettingsIniName);

        // --- Theme resource path (same as DXMainClient Startup.Execute) ---
        ProgramConstants.RESOURCES_DIR = SafePath.CombineDirectoryPath(
            ProgramConstants.BASE_RESOURCE_PATH,
            UserINISettings.Instance.ThemeFolderPath);

        DirectoryInfo resourcesDirectory = SafePath.GetDirectory(ProgramConstants.GetResourcePath());
        if (!resourcesDirectory.Exists)
        {
            Logger.Log("Theme directory not found: " + ProgramConstants.GetResourcePath());
            Logger.Log("Falling back to base resources.");
            ProgramConstants.RESOURCES_DIR = ProgramConstants.BASE_RESOURCE_PATH;
        }

        Logger.Log("Resource path: " + ProgramConstants.GetResourcePath());
        Logger.Log("Base resource path: " + ProgramConstants.GetBaseResourcePath());

        // --- DI container ---
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // --- ViewModel initialization (starts background tasks) ---
        var loadingScreenVM = serviceProvider.GetRequiredService<ILoadingScreenViewModel>();
        if (loadingScreenVM is LoadingScreenViewModel concrete)
        {
            Logger.Log("Initializing LoadingScreenViewModel...");
            concrete.Initialize();
            Logger.Log("LoadingScreenViewModel initialized.");
        }

        Logger.Log("PreStartup initialization complete.");
        return serviceProvider;
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
}
