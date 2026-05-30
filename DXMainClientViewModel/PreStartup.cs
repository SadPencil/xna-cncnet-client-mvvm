using System;
using System.Globalization;
using System.IO;
using ClientCore;
using ClientCore.I18N;
using DXMainClientViewModel.Domain;
using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Campaign;
using DXMainClientViewModel.Generic;
using DXMainClientViewModel.Generic.OptionPanels;
using DXMainClientViewModel.Online;
using DXMainClientViewModel.Services;
using Microsoft.Extensions.DependencyInjection;
using Rampastring.Tools;

namespace DXMainClientViewModel;

/// <summary>
/// Initializes client systems before the UI starts.
/// Replicates the original DXMainClient PreStartup + Startup initialization chain.
/// </summary>
public static class PreStartup
{
    /// <summary>
    /// Initializes all non-UI systems, registers domain services in the DI container,
    /// and returns the service collection for the View layer to add UI-specific services.
    /// </summary>
    public static ServiceCollection Initialize()
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
        Domain.MainClientConstants.LoggerInitialized = true;

        if (!clientUserFilesDirectory.Exists)
            clientUserFilesDirectory.Create();

        Logger.Log("***Logfile for " + Domain.MainClientConstants.GAME_NAME_LONG + " client***");

        string clientVersion = GitVersionInformation.AssemblySemVer;
        Logger.Log("Client version: " + clientVersion);
        Logger.Log(GitVersionInformation.InformationalVersion);

        // --- Client configuration (same as DXMainClient PreStartup) ---
        Domain.MainClientConstants.Initialize();

        Logger.Log("Loading settings.");
        UserINISettings.Initialize(ClientConfiguration.Instance.SettingsIniName);

        // --- Player name initialization (same as DXMainClient GameClass.Initialize) ---
        string playerName = UserINISettings.Instance.PlayerName.Value.Trim();

        if (UserINISettings.Instance.AutoRemoveUnderscoresFromName)
        {
            while (playerName.EndsWith("_"))
                playerName = playerName.Substring(0, playerName.Length - 1);
        }

        if (string.IsNullOrEmpty(playerName))
        {
            playerName = Environment.UserName;
            playerName = playerName.Substring(playerName.IndexOf("\\") + 1);
        }

        playerName = Domain.Multiplayer.CnCNet.NameValidator.GetValidOfflineName(playerName);

        ProgramConstants.PLAYERNAME = playerName;
        UserINISettings.Instance.PlayerName.Value = playerName;

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

        // --- DI container with domain services ---
        var services = new ServiceCollection();
        ConfigureServices(services);

        Logger.Log("PreStartup initialization complete.");
        return services;
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // Core services
        services.AddSingleton<Random>(_ => new Random());

        // Domain services
        services.AddSingleton<GameCollection>();
        services.AddSingleton<CnCNetUserData>(_ => new CnCNetUserData(() => { }));
        services.AddSingleton<CnCNetManager>();
        services.AddSingleton<MapLoader>();
        services.AddSingleton<PrivateMessageHandler>();

        // Service adapters
        services.AddSingleton<IUpdateService, ClientUpdateService>();
        services.AddSingleton<IGameProcessService, GameProcessService>();
        services.AddSingleton<IGameProcessSettingsService, GameProcessSettingsService>();
        services.AddSingleton<IDiscordHandlerService, DiscordHandlerService>();
        services.AddSingleton<IMusicPlayerService, MusicPlayerService>();
        services.AddSingleton<IResolutionProvider, ResolutionProvider>();
        services.AddSingleton<DirectDrawWrapperManager>();

        // Option panel ViewModels
        services.AddSingleton<IDisplayOptionsPanelViewModel>(sp =>
            new DisplayOptionsPanelViewModel(
                UserINISettings.Instance,
                sp.GetRequiredService<DirectDrawWrapperManager>(),
                sp.GetRequiredService<IResolutionProvider>()));
        services.AddSingleton<IAudioOptionsPanelViewModel>(sp =>
            new AudioOptionsPanelViewModel(UserINISettings.Instance, sp.GetRequiredService<IClientSoundService>()));
        services.AddSingleton<IGameOptionsPanelViewModel>(_ =>
            new GameOptionsPanelViewModel(UserINISettings.Instance));
        services.AddSingleton<ICnCNetOptionsPanelViewModel>(sp =>
            new CnCNetOptionsPanelViewModel(UserINISettings.Instance, sp.GetRequiredService<GameCollection>()));
        services.AddSingleton<IUpdaterOptionsPanelViewModel>(_ =>
            new UpdaterOptionsPanelViewModel(UserINISettings.Instance));
        services.AddSingleton<IComponentsPanelViewModel>(sp =>
            new ComponentsPanelViewModel(sp.GetRequiredService<IUIThreadMarshaller>()));

        // OptionsWindowViewModel - resolves all option panels from DI
        services.AddSingleton<OptionsWindowViewModel>(sp => new OptionsWindowViewModel(
            sp.GetRequiredService<IDisplayOptionsPanelViewModel>(),
            sp.GetRequiredService<IAudioOptionsPanelViewModel>(),
            sp.GetRequiredService<IGameOptionsPanelViewModel>(),
            sp.GetRequiredService<ICnCNetOptionsPanelViewModel>(),
            sp.GetRequiredService<IUpdaterOptionsPanelViewModel>(),
            sp.GetRequiredService<IComponentsPanelViewModel>()));
        services.AddSingleton<IOptionsWindowViewModel>(sp =>
            sp.GetRequiredService<OptionsWindowViewModel>());

        // TopBarViewModel
        services.AddSingleton<TopBarViewModel>(sp => new TopBarViewModel(
            sp.GetRequiredService<CnCNetManager>(),
            sp.GetRequiredService<PrivateMessageHandler>(),
            sp.GetRequiredService<IUIThreadMarshaller>(),
            sp.GetRequiredService<OptionsWindowViewModel>()));
        services.AddSingleton<ITopBarViewModel>(sp =>
            sp.GetRequiredService<TopBarViewModel>());

        // MainMenuViewModel
        services.AddSingleton<IMainMenuViewModel>(sp => new MainMenuViewModel(
            sp.GetRequiredService<IUpdateService>(),
            sp.GetRequiredService<IGameProcessService>(),
            sp.GetRequiredService<IDiscordHandlerService>(),
            sp.GetRequiredService<IMusicPlayerService>(),
            sp.GetRequiredService<IUIThreadMarshaller>(),
            sp.GetRequiredService<CnCNetManager>(),
            sp.GetRequiredService<IOptionsWindowViewModel>(),
            sp.GetRequiredService<ITopBarViewModel>()));

        services.AddTransient<ILoadingScreenViewModel, LoadingScreenViewModel>();
        services.AddTransient<IPrivacyNotificationViewModel, PrivacyNotificationViewModel>();
        services.AddTransient<IUpdateQueryWindowViewModel, UpdateQueryWindowViewModel>();
        services.AddTransient<IManualUpdateQueryWindowViewModel, ManualUpdateQueryWindowViewModel>();
        services.AddTransient<IUpdateWindowViewModel, UpdateWindowViewModel>();
        services.AddTransient<IStatisticsWindowViewModel, StatisticsWindowViewModel>();
        services.AddTransient<IExtrasWindowViewModel, ExtrasWindowViewModel>();
        services.AddTransient<IGameInProgressWindowViewModel, GameInProgressWindowViewModel>();
        services.AddTransient<ICampaignSelectorViewModel, CampaignSelectorViewModel>();
        services.AddTransient<IGameLoadingWindowViewModel, GameLoadingWindowViewModel>();
    }
}
