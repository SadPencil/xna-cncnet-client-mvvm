using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Generic.OptionPanels;
using AvClientMvvmContract.Campaign;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.ViewServices;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.I18N;
using ClientCore.INIProcessing;
using ClientCore.Settings;

using ClientUpdater;

using AvClientViewModel.Domain;
using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.CnCNet;
using AvClientViewModel.Campaign;
using AvClientViewModel.Generic;
using AvClientViewModel.Generic.OptionPanels;
using AvClientViewModel.LAN;
using AvClientViewModel.Multiplayer;
using AvClientViewModel.Multiplayer.CnCNet;
using AvClientViewModel.Multiplayer.GameLobby;
using AvClientViewModel.Online;
using AvClientViewModel.Services;

using Microsoft.Extensions.DependencyInjection;

using Rampastring.Tools;

using Steamworks;

namespace AvClientViewModel;

/// <summary>
/// Contains client startup parameters.
/// </summary>
public struct StartupParams
{
    public StartupParams(bool noAudio, bool multipleInstanceMode,
        List<string> unknownParams)
    {
        NoAudio = noAudio;
        MultipleInstanceMode = multipleInstanceMode;
        UnknownStartupParams = unknownParams ?? new List<string>();
    }

    public bool NoAudio { get; }
    public bool MultipleInstanceMode { get; }
    public List<string> UnknownStartupParams { get; }
}

/// <summary>
/// Initializes client systems before the UI starts.
/// Replicates the original DXMainClient PreStartup + Startup initialization chain.
/// </summary>
public static class PreStartup
{
    private static readonly Stopwatch startupStopwatch = Stopwatch.StartNew();
    public static TimeSpan StartupElapsed => startupStopwatch.Elapsed;

    /// <summary>
    /// Initializes all non-UI systems.
    /// </summary>
    public static void Initialize(StartupParams parameters = default)
    {
        // --- Culture (same as DXMainClient PreStartup lines 60-61) ---
        Translation.InitialUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(ProgramConstants.HARDCODED_LOCALE_CODE);

        IniFile.DisallowDesktopIni = true;

        // --- Exception handling (same as DXMainClient PreStartup lines 65-69) ---
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            HandleException(sender, (Exception)args.ExceptionObject);

        // --- Working directory (same as DXMainClient PreStartup lines 71-73) ---
        DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);
        Environment.CurrentDirectory = gameDirectory.FullName;

        // --- Check permissions (same as DXMainClient PreStartup lines 75-76) ---
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            CheckPermissions();

        // --- Logger setup (same as DXMainClient PreStartup lines 78-97) ---
        DirectoryInfo clientUserFilesDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);
        FileInfo clientLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client.log");
        ProgramConstants.LogFileName = clientLogFile.FullName;

        if (clientLogFile.Exists)
        {
            // Copy client.log file as client_previous.log. Override client_previous.log if it exists.
            FileInfo clientPrevLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client_previous.log");
            if (clientPrevLogFile.Exists)
                File.Delete(clientPrevLogFile.FullName);
            File.Move(clientLogFile.FullName, clientPrevLogFile.FullName);
        }

        Logger.Initialize(clientUserFilesDirectory.FullName, clientLogFile.Name);
        Logger.WriteLogFile = true;
        MainClientConstants.LoggerInitialized = true;

        if (!clientUserFilesDirectory.Exists)
            clientUserFilesDirectory.Create();

        Logger.Log("***Logfile for " + MainClientConstants.GAME_NAME_LONG + " client***");

        // --- Version logging (same as DXMainClient PreStartup lines 100-110) ---
        string clientVersion = GitVersionInformation.AssemblySemVer;
#if DEVELOPMENT_BUILD
        clientVersion = $"{GitVersionInformation.CommitDate} {GitVersionInformation.BranchName}@{GitVersionInformation.ShortSha}";
#endif

        Logger.Log($"Client version: {clientVersion}");
        Logger.Log(GitVersionInformation.InformationalVersion);

#if DEVELOPMENT_BUILD
        Logger.Log("This is a development build of the client. Stability and reliability may not be fully guaranteed.");
#endif

        // --- Client configuration (same as DXMainClient PreStartup line 111) ---
        MainClientConstants.Initialize();

        // --- Startup params logging (same as DXMainClient PreStartup lines 114-125) ---
        if (parameters.NoAudio)
        {
            Logger.Log("Startup parameter: No audio");

            // TODO fix
            throw new NotImplementedException("-NOAUDIO is currently not implemented, please run the client without it.".L10N("Client:Main:NoAudio"));
        }

        if (parameters.MultipleInstanceMode)
            Logger.Log("Startup parameter: Allow multiple client instances");

        parameters.UnknownStartupParams.ForEach(p => Logger.Log("Unknown startup parameter: " + p));

        Logger.Log("Loading settings.");

        // --- Settings initialization (same as DXMainClient PreStartup lines 127-129) ---
        UserINISettings.Initialize(ClientConfiguration.Instance.SettingsIniName);

        // --- Translation loading (same as DXMainClient PreStartup lines 131-164) ---
        try
        {
            Translation translation;
            FileInfo translationThemeFile = SafePath.GetFile(UserINISettings.Instance.TranslationThemeFolderPath, ClientConfiguration.Instance.TranslationIniName);
            FileInfo translationFile = SafePath.GetFile(UserINISettings.Instance.TranslationFolderPath, ClientConfiguration.Instance.TranslationIniName);

            if (translationFile.Exists)
            {
                Logger.Log($"Loading generic translation file at {translationFile.FullName}");
                translation = new Translation(translationFile.FullName, UserINISettings.Instance.Translation);
                if (translationThemeFile.Exists)
                {
                    Logger.Log($"Loading theme-specific translation file at {translationThemeFile.FullName}");
                    translation.AppendValuesFromIniFile(translationThemeFile.FullName);
                }

                Translation.Instance = translation;
            }
            else
            {
                Logger.Log($"Failed to load a translation file. " +
                    $"Neither {translationThemeFile.FullName} nor {translationFile.FullName} exist.");
            }

            Logger.Log("Loaded translation: " + Translation.Instance.Name);
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to load the translation file. " + ex.ToString());
            Translation.Instance = new Translation(UserINISettings.Instance.Translation);
        }

        CultureInfo.CurrentUICulture = Translation.Instance.Culture;

        // --- Translation stub generation (same as DXMainClient PreStartup lines 166-192) ---
        try
        {
            if (UserINISettings.Instance.GenerateTranslationStub)
            {
                string stubPath = SafePath.CombineFilePath(
                    ProgramConstants.ClientUserFilesPath, ClientConfiguration.Instance.TranslationIniName);

                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Logger.Log("Writing the translation stub file.");
                    var ini = Translation.Instance.DumpIni(UserINISettings.Instance.GenerateOnlyNewValuesInTranslationStub);
                    ini.WriteIniFile(stubPath);
                };

                Logger.Log("Translation stub generation feature is now enabled. The stub file will be written when the client exits.");

                // Lookup all compile-time available strings
                ClientCore.Generated.TranslationNotifier.Register();
                ClientUpdater.Generated.TranslationNotifier.Register();
                AvClientViewModel.Generated.TranslationNotifier.Register();
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to generate the translation stub: " + ex.ToString());
        }

        // --- Custom mission initialization (same as DXMainClient PreStartup lines 194-196) ---
        CustomMissionHelper.Initialize();
        CustomMissionHelper.DeleteSupplementalMissionFiles();

        // --- Delete obsolete files from old target project versions (same as DXMainClient PreStartup lines 199-218) ---
        Task.Run(() =>
        {
            gameDirectory.EnumerateFiles("mainclient.log").SingleOrDefault()?.Delete();
            gameDirectory.EnumerateFiles("aunchupdt.dat").SingleOrDefault()?.Delete();

            try
            {
                gameDirectory.EnumerateFiles("wsock32.dll").SingleOrDefault()?.Delete();
            }
            catch (Exception ex)
            {
                LogException(ex);

                string error = ("Deleting wsock32.dll failed! Please close any " +
                    "applications that could be using the file, and then start the client again." + "\n\n" +
                    "Message:").L10N("Client:Main:DeleteWsock32Failed") + " " + ex.Message;

                MainClientConstants.DisplayErrorAction(null, error, true);
            }
        });

        // --- Resource path (same as DXMainClient Startup.Execute lines 37-41) ---
        ProgramConstants.RESOURCES_DIR = SafePath.CombineDirectoryPath(
            ProgramConstants.BASE_RESOURCE_PATH,
            UserINISettings.Instance.ThemeFolderPath);

        DirectoryInfo resourcesDirectory = SafePath.GetDirectory(ProgramConstants.GetResourcePath());
        if (!resourcesDirectory.Exists)
            throw new DirectoryNotFoundException("Theme directory not found!" + Environment.NewLine + ProgramConstants.RESOURCES_DIR);

        Logger.Log("Resource path: " + ProgramConstants.GetResourcePath());
        Logger.Log("Base resource path: " + ProgramConstants.GetBaseResourcePath());

        // --- Player name initialization (same as DXMainClient GameClass.Initialize lines 222-240) ---
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

        playerName = NameValidator.GetValidOfflineName(playerName);

        ProgramConstants.PLAYERNAME = playerName;
        UserINISettings.Instance.PlayerName.Value = playerName;

        // --- Client resolution initialization (same as DXMainClient Startup.Execute lines 133-147) ---
        if (!UserINISettings.Instance.BorderlessWindowedClient)
        {
            var (bestWidth, bestHeight) = GetBestRecommendedResolution();
            UserINISettings.Instance.ClientResolutionX = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionX", bestWidth);
            UserINISettings.Instance.ClientResolutionY = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionY", bestHeight);
        }
        else
        {
            var (safeWidth, safeHeight) = GetSafeFullScreenResolution();
            UserINISettings.Instance.ClientResolutionX = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionX", safeWidth);
            UserINISettings.Instance.ClientResolutionY = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionY", safeHeight);
        }

        // --- Updater initialization (same as DXMainClient Startup.Execute lines 44-48) ---
        Logger.Log("Initializing updater.");

        SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "version_u");

        Updater.Initialize(
            ProgramConstants.GamePath,
            ProgramConstants.GetBaseResourcePath(),
            ClientConfiguration.Instance.SettingsIniName,
            ClientConfiguration.Instance.LocalGame,
            SafePath.GetFile(ProgramConstants.StartupExecutable).Name);

        // --- OS / Framework info logging (same as DXMainClient Startup.Execute lines 50-55) ---
        Logger.Log("OSDescription: " + RuntimeInformation.OSDescription);
        Logger.Log("OSArchitecture: " + RuntimeInformation.OSArchitecture);
        Logger.Log("ProcessArchitecture: " + RuntimeInformation.ProcessArchitecture);
        Logger.Log("FrameworkDescription: " + RuntimeInformation.FrameworkDescription);
        Logger.Log("Selected OS profile: " + MainClientConstants.OSId);
        Logger.Log("Current culture: " + CultureInfo.CurrentCulture);

        // --- System specifications check (same as DXMainClient Startup.Execute lines 57-63) ---
        IPreStartupSystemService preStartupSystemService = new PreStartupSystemService();
        preStartupSystemService.StartSystemSpecificationsCheck();
        preStartupSystemService.StartOnlineIdGeneration();

        // --- Ares debug file pruning (same as DXMainClient Startup.Execute lines 69-70) ---
        if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
            Task.Run(() => PruneFiles(SafePath.GetDirectory(ProgramConstants.GamePath, "debug"), DateTime.Now.AddDays(-7)));

        // --- Log file migration (same as DXMainClient Startup.Execute line 72) ---
        Task.Run(MigrateOldLogFiles);

        // --- INI file preprocessor (same as DXMainClient Startup.Execute line 75) ---
        PreprocessorBackgroundTask.Instance.Run();

        // --- Delete temporary updater directory (same as DXMainClient Startup.Execute lines 77-89) ---
        DirectoryInfo updaterFolder = SafePath.GetDirectory(ProgramConstants.GamePath, "Updater");

        if (updaterFolder.Exists)
        {
            Logger.Log("Attempting to delete temporary updater directory.");
            try
            {
                updaterFolder.Delete(true);
            }
            catch
            {
            }
        }

        // --- Create Saved Games directory (same as DXMainClient Startup.Execute lines 91-106) ---
        if (ClientConfiguration.Instance.CreateSavedGamesDirectory)
        {
            DirectoryInfo savedGamesFolder = SafePath.GetDirectory(ProgramConstants.GamePath, "Saved Games");

            if (!savedGamesFolder.Exists)
            {
                Logger.Log("Saved Games directory does not exist - attempting to create one.");
                try
                {
                    savedGamesFolder.Create();
                }
                catch
                {
                }
            }
        }

        // --- Remove partial custom component downloads (same as DXMainClient Startup.Execute lines 108-122) ---
        if (Updater.CustomComponents != null)
        {
            Logger.Log("Removing partial custom component downloads.");
            foreach (var component in Updater.CustomComponents)
            {
                try
                {
                    SafePath.DeleteFileIfExists(ProgramConstants.GamePath, FormattableString.Invariant($"{component.LocalPath}_u"));
                }
                catch
                {
                }
            }
        }

        // --- FinalSun settings (same as DXMainClient Startup.Execute line 124) ---
        FinalSunSettings.WriteFinalSunIniAsync();

        // --- Write install path to registry (same as DXMainClient Startup.Execute lines 126-127) ---
        preStartupSystemService.WriteInstallPathToRegistryIfNeeded();

        // --- Refresh settings (same as DXMainClient Startup.Execute line 129) ---
        ClientConfiguration.Instance.RefreshSettings();

        // --- Steamworks initialization (same as DXMainClient Startup.Execute lines 157-158) ---
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Task.Run(InitSteamworks);

        Logger.Log("PreStartup initialization complete.");
    }

    public static void ConfigureServices(ServiceCollection services)
    {
        // Core services
        services.AddSingleton<Random>(_ => new Random());

        // Domain services
        services.AddSingleton<GameCollection>();
        services.AddSingleton<CnCNetUserData>(_ => new CnCNetUserData(() => { })); // TODO: empty callback? check it
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
        services.AddSingleton<IFileIntegrityService, FileIntegrityService>();
        services.AddSingleton<ICampaignGameProcessService, CampaignGameProcessService>();

        // CnCNet / Multiplayer domain services
        services.AddSingleton<TunnelHandler>();
        services.AddSingleton<DiscordHandler>();

        // LAN services
        services.AddSingleton<ILANBroadcastManagerService>(sp =>
            new LANBroadcastManagerService(ProgramConstants.LAN_LOBBY_PORT, System.Text.Encoding.UTF8));
        services.AddSingleton<ILANPlayerManagerService, LANPlayerManagerService>();
        services.AddSingleton<ILANMessageDeduplicatorService>(sp =>
            new LANMessageDeduplicatorService(sp.GetRequiredService<Random>().Next()));

        // Option panel ViewModels
        services.AddSingleton<DisplayOptionsPanelViewModel>(sp =>
            new DisplayOptionsPanelViewModel(
                UserINISettings.Instance,
                sp.GetRequiredService<DirectDrawWrapperManager>(),
                sp.GetRequiredService<IResolutionProvider>()));
        services.AddSingleton<IDisplayOptionsPanelViewModel>(sp =>
            sp.GetRequiredService<DisplayOptionsPanelViewModel>());

        services.AddSingleton<AudioOptionsPanelViewModel>(sp =>
            new AudioOptionsPanelViewModel(UserINISettings.Instance, sp.GetRequiredService<IClientSoundService>()));
        services.AddSingleton<IAudioOptionsPanelViewModel>(sp =>
            sp.GetRequiredService<AudioOptionsPanelViewModel>());

        services.AddSingleton<GameOptionsPanelViewModel>(_ =>
            new GameOptionsPanelViewModel(UserINISettings.Instance));
        services.AddSingleton<IGameOptionsPanelViewModel>(sp =>
            sp.GetRequiredService<GameOptionsPanelViewModel>());

        services.AddSingleton<CnCNetOptionsPanelViewModel>(sp =>
            new CnCNetOptionsPanelViewModel(UserINISettings.Instance, sp.GetRequiredService<GameCollection>()));
        services.AddSingleton<ICnCNetOptionsPanelViewModel>(sp =>
            sp.GetRequiredService<CnCNetOptionsPanelViewModel>());

        services.AddSingleton<UpdaterOptionsPanelViewModel>(_ =>
            new UpdaterOptionsPanelViewModel(UserINISettings.Instance));
        services.AddSingleton<IUpdaterOptionsPanelViewModel>(sp =>
            sp.GetRequiredService<UpdaterOptionsPanelViewModel>());

        services.AddSingleton<ComponentsPanelViewModel>(sp =>
            new ComponentsPanelViewModel(sp.GetRequiredService<IUIThreadMarshaller>()));
        services.AddSingleton<IComponentsPanelViewModel>(sp =>
            sp.GetRequiredService<ComponentsPanelViewModel>());

        // OptionsWindowViewModel - resolves all option panels from DI
        services.AddSingleton<OptionsWindowViewModel>(sp => new OptionsWindowViewModel(
            sp.GetRequiredService<DisplayOptionsPanelViewModel>(),
            sp.GetRequiredService<AudioOptionsPanelViewModel>(),
            sp.GetRequiredService<GameOptionsPanelViewModel>(),
            sp.GetRequiredService<CnCNetOptionsPanelViewModel>(),
            sp.GetRequiredService<UpdaterOptionsPanelViewModel>(),
            sp.GetRequiredService<ComponentsPanelViewModel>()));
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

        // CampaignSelectorViewModel (singleton: MainMenuViewModel holds a reference and sets IsVisible)
        services.AddSingleton<CampaignSelectorViewModel>();
        services.AddSingleton<ICampaignSelectorViewModel>(sp =>
            sp.GetRequiredService<CampaignSelectorViewModel>());

        // GameLoadingWindowViewModel (singleton: MainMenuViewModel holds a reference and sets IsVisible)
        services.AddSingleton<GameLoadingWindowViewModel>();
        services.AddSingleton<IGameLoadingWindowViewModel>(sp =>
            sp.GetRequiredService<GameLoadingWindowViewModel>());

        // StatisticsWindowViewModel (singleton: MainMenuViewModel holds a reference and sets IsVisible)
        services.AddSingleton<StatisticsWindowViewModel>();
        services.AddSingleton<IStatisticsWindowViewModel>(sp =>
            sp.GetRequiredService<StatisticsWindowViewModel>());

        // ExtrasWindowViewModel (singleton: MainMenuViewModel holds a reference and sets IsVisible)
        services.AddSingleton<ExtrasWindowViewModel>();
        services.AddSingleton<IExtrasWindowViewModel>(sp =>
            sp.GetRequiredService<ExtrasWindowViewModel>());

        // UpdateWindowViewModel
        services.AddSingleton<UpdateWindowViewModel>();

        // SkirmishLobbyViewModel
        services.AddSingleton<SkirmishLobbyViewModel>(sp => new SkirmishLobbyViewModel(
            sp.GetRequiredService<MapLoader>(),
            sp.GetRequiredService<DiscordHandler>(),
            sp.GetRequiredService<IGameProcessService>(),
            sp.GetRequiredService<IUIThreadMarshaller>(),
            sp.GetRequiredService<Random>()));
        services.AddSingleton<ISkirmishLobbyViewModel>(sp =>
            sp.GetRequiredService<SkirmishLobbyViewModel>());

        // GameHostInactiveCheckerService
        services.AddSingleton<IGameHostInactiveCheckerService, GameHostInactiveCheckerService>();

        // CnCNetGameLobbyViewModel
        services.AddSingleton<CnCNetGameLobbyViewModel>(sp => new CnCNetGameLobbyViewModel(
            sp.GetRequiredService<MapLoader>(),
            sp.GetRequiredService<DiscordHandler>(),
            sp.GetRequiredService<IGameProcessService>(),
            sp.GetRequiredService<IUIThreadMarshaller>(),
            sp.GetRequiredService<Random>(),
            sp.GetRequiredService<CnCNetManager>(),
            sp.GetRequiredService<TunnelHandler>(),
            sp.GetRequiredService<GameCollection>(),
            sp.GetRequiredService<CnCNetUserData>(),
            sp.GetRequiredService<IGameHostInactiveCheckerService>()));

        // CnCNetGameLoadingLobbyViewModel
        services.AddSingleton<CnCNetGameLoadingLobbyViewModel>(sp => new CnCNetGameLoadingLobbyViewModel(
            sp.GetRequiredService<DiscordHandler>(),
            sp.GetRequiredService<IGameProcessService>(),
            sp.GetRequiredService<IUIThreadMarshaller>(),
            sp.GetRequiredService<CnCNetManager>(),
            sp.GetRequiredService<CnCNetUserData>(),
            sp.GetRequiredService<TunnelHandler>(),
            sp.GetRequiredService<GameCollection>()));

        // CnCNetLobbyViewModel
        services.AddSingleton<CnCNetLobbyViewModel>(sp => new CnCNetLobbyViewModel(
            sp.GetRequiredService<CnCNetManager>(),
            sp.GetRequiredService<CnCNetUserData>(),
            sp.GetRequiredService<GameCollection>(),
            sp.GetRequiredService<TunnelHandler>(),
            sp.GetRequiredService<IUIThreadMarshaller>(),
            sp.GetRequiredService<IGameProcessService>(),
            sp.GetRequiredService<Random>()));
        services.AddSingleton<ICnCNetLobbyViewModel>(sp =>
            sp.GetRequiredService<CnCNetLobbyViewModel>());

        // LANLobbyViewModel
        services.AddSingleton<LANLobbyViewModel>(sp => new LANLobbyViewModel(
            sp.GetRequiredService<ILANBroadcastManagerService>(),
            sp.GetRequiredService<ILANPlayerManagerService>(),
            sp.GetRequiredService<ILANMessageDeduplicatorService>(),
            sp.GetRequiredService<IUIThreadMarshaller>(),
            sp.GetRequiredService<IApplicationLifecycleService>(),
            sp.GetRequiredService<IGameProcessService>(),
            sp.GetRequiredService<GameCollection>(),
            sp.GetRequiredService<MapLoader>(),
            sp.GetRequiredService<DiscordHandler>(),
            sp.GetRequiredService<Random>()));
        services.AddSingleton<ILANLobbyViewModel>(sp =>
            sp.GetRequiredService<LANLobbyViewModel>());

        // PrivateMessagingWindowViewModel
        services.AddSingleton<PrivateMessagingWindowViewModel>(sp => new PrivateMessagingWindowViewModel(
            sp.GetRequiredService<CnCNetManager>(),
            sp.GetRequiredService<CnCNetUserData>(),
            sp.GetRequiredService<PrivateMessageHandler>(),
            sp.GetRequiredService<IUIThreadMarshaller>(),
            sp.GetRequiredService<IGameProcessService>()));
        services.AddSingleton<IPrivateMessagingWindowViewModel>(sp =>
            sp.GetRequiredService<PrivateMessagingWindowViewModel>());

        // MainMenuViewModel
        services.AddSingleton<MainMenuViewModel>(sp => new MainMenuViewModel(
            sp.GetRequiredService<IUpdateService>(),
            sp.GetRequiredService<IGameProcessService>(),
            sp.GetRequiredService<IDiscordHandlerService>(),
            sp.GetRequiredService<IMusicPlayerService>(),
            sp.GetRequiredService<IUIThreadMarshaller>(),
            sp.GetRequiredService<IApplicationLifecycleService>(),
            sp.GetRequiredService<CnCNetManager>(),
            sp.GetRequiredService<OptionsWindowViewModel>(),
            sp.GetRequiredService<TopBarViewModel>(),
            sp.GetRequiredService<CampaignSelectorViewModel>(),
            sp.GetRequiredService<GameLoadingWindowViewModel>(),
            sp.GetRequiredService<ExtrasWindowViewModel>(),
            sp.GetRequiredService<StatisticsWindowViewModel>(),
            sp.GetRequiredService<UpdateWindowViewModel>(),
            sp.GetRequiredService<CnCNetUserData>(),
            sp.GetRequiredService<SkirmishLobbyViewModel>(),
            sp.GetRequiredService<CnCNetLobbyViewModel>(),
            sp.GetRequiredService<LANLobbyViewModel>(),
            sp.GetRequiredService<PrivateMessagingWindowViewModel>(),
            sp.GetRequiredService<CnCNetGameLobbyViewModel>(),
            sp.GetRequiredService<CnCNetGameLoadingLobbyViewModel>()));
        services.AddSingleton<IMainMenuViewModel>(sp =>
            sp.GetRequiredService<MainMenuViewModel>());

        services.AddTransient<ILoadingScreenViewModel, LoadingScreenViewModel>();
        services.AddTransient<IPrivacyNotificationViewModel, PrivacyNotificationViewModel>();
        services.AddTransient<IUpdateQueryWindowViewModel, UpdateQueryWindowViewModel>();
        services.AddTransient<IManualUpdateQueryWindowViewModel, ManualUpdateQueryWindowViewModel>();
        services.AddTransient<IUpdateWindowViewModel, UpdateWindowViewModel>();
        services.AddTransient<IGameInProgressWindowViewModel, GameInProgressWindowViewModel>();
    }

    // ===================================================================
    // Exception handling (same as DXMainClient PreStartup lines 239-286)
    // ===================================================================

    public static void LogException(Exception ex, bool innerException = false)
    {
        if (!innerException)
            Logger.Log("KABOOOOOOM!!! Info:");
        else
            Logger.Log("InnerException info:");

        Logger.Log("Type: " + ex.GetType());
        Logger.Log("Message: " + ex.Message);
        Logger.Log("Source: " + ex.Source);
        Logger.Log("TargetSite.Name: " + ex.TargetSite?.Name);
        Logger.Log("Stacktrace: " + ex.StackTrace);

        if (ex.InnerException is not null)
            LogException(ex.InnerException, true);
    }

    public static void HandleException(object sender, Exception ex)
    {
        LogException(ex, innerException: false);

        string errorLogPath = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs", FormattableString.Invariant($"ClientCrashLog{DateTime.Now.ToString("_yyyy_MM_dd_HH_mm")}.txt"));
        bool crashLogCopied = false;

        try
        {
            DirectoryInfo crashLogsDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs");

            if (!crashLogsDirectoryInfo.Exists)
                crashLogsDirectoryInfo.Create();

            File.Copy(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "client.log"), errorLogPath, true);
            crashLogCopied = true;
        }
        catch { }

        string error = string.Format("{0} has crashed. Error message:".L10N("Client:Main:FatalErrorText1") + Environment.NewLine + Environment.NewLine +
            ex.Message + Environment.NewLine + Environment.NewLine + (crashLogCopied ?
            "A crash log has been saved to the following file:".L10N("Client:Main:FatalErrorText2") + " " + Environment.NewLine + Environment.NewLine +
            errorLogPath + Environment.NewLine + Environment.NewLine : "") +
            (crashLogCopied ? "If the issue is repeatable, contact the {1} staff at {2} and provide the crash log file.".L10N("Client:Main:FatalErrorText3") :
            "If the issue is repeatable, contact the {1} staff at {2}.".L10N("Client:Main:FatalErrorText4")),
            MainClientConstants.GAME_NAME_LONG,
            MainClientConstants.GAME_NAME_SHORT,
            MainClientConstants.SUPPORT_URL_SHORT);

        MainClientConstants.DisplayErrorAction("KABOOOOOOOM".L10N("Client:Main:FatalErrorTitle"), error, true);
    }

    // ===================================================================
    // Permissions check (same as DXMainClient PreStartup lines 288-378)
    // ===================================================================

    [SupportedOSPlatform("windows")]
    private static void CheckPermissions()
    {
        if (UserHasDirectoryAccessRights(ProgramConstants.GamePath, FileSystemRights.Modify))
            return;

        string error = string.Format(("You seem to be running {0} from a write-protected directory.\n\n" +
            "For {1} to function properly when run from a write-protected directory, it needs administrative privileges.\n\n" +
            "Please also make sure that your security software isn't blocking {1}.").L10N("Client:Main:AdminRequiredExplanation"),
            MainClientConstants.GAME_NAME_LONG, MainClientConstants.GAME_NAME_SHORT);

        string title = "Administrative privileges required".L10N("Client:Main:AdminRequiredTitle");

        MainClientConstants.DisplayErrorAction(title, error, true);

        Environment.Exit(1);
    }

    /// <summary>
    /// Checks whether the client has specific file system rights to a directory.
    /// See ssds's answer at https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="accessRights">The file system rights.</param>
    [SupportedOSPlatform("windows")]
    private static bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights)
    {
        var currentUser = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(currentUser);

        // If the user is not running the client with administrator privileges in Program Files, they need to be prompted to do so.
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            string progfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (ProgramConstants.GamePath.Contains(progfiles) || ProgramConstants.GamePath.Contains(progfilesx86))
                return false;
        }

        var isInRoleWithAccess = false;

        try
        {
            var di = new DirectoryInfo(path);
            var acl = di.GetAccessControl();
            var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

            foreach (AuthorizationRule rule in rules)
            {
                var fsAccessRule = rule as FileSystemAccessRule;
                if (fsAccessRule == null)
                    continue;

                if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                {
                    var ntAccount = rule.IdentityReference as NTAccount;
                    if (ntAccount == null)
                        continue;

                    try
                    {
                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
                        }
                    }
                    catch (Exception)
                    {
                        //IsInRole may throw for selected roles when running in Wine, keep iterating other rules
                        continue;
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        return isInRoleWithAccess;
    }

    // ===================================================================
    // Steamworks (same as DXMainClient Startup lines 163-192)
    // ===================================================================

    [SupportedOSPlatform("windows")]
    private static void InitSteamworks()
    {
        if (UserINISettings.Instance.SteamIntegration)
        {
            try
            {
                if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares || ClientConfiguration.Instance.ClientGameType == ClientType.YR)
                {
                    Logger.Log("Steam init called");
                    SteamClient.Init(2229850);
                }
                else if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
                {
                    Logger.Log("Steam init called");
                    SteamClient.Init(2229880);
                }
                else if (ClientConfiguration.Instance.ClientGameType == ClientType.RA)
                {
                    Logger.Log("Steam init called");
                    SteamClient.Init(2229840);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Steam init failed: " + e.Message);
                // Couldn't init for some reason (steam is closed etc)
            }
        }
    }

    // ===================================================================
    // File pruning and log migration (same as DXMainClient Startup lines 198-291)
    // ===================================================================

    /// <summary>
    /// Recursively deletes all files from the specified directory that were created at <paramref name="pruneThresholdTime"/> or before.
    /// If directory is empty after deleting files, the directory itself will also be deleted.
    /// </summary>
    private static void PruneFiles(DirectoryInfo directory, DateTime pruneThresholdTime)
    {
        if (!directory.Exists)
            return;

        try
        {
            foreach (FileSystemInfo fsEntry in directory.EnumerateFileSystemInfos())
            {
                if ((fsEntry.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    PruneFiles(new DirectoryInfo(fsEntry.FullName), pruneThresholdTime);
                else
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(fsEntry.FullName);
                        if (fileInfo.CreationTime <= pruneThresholdTime)
                            fileInfo.Delete();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("PruneFiles: Could not delete file " + fsEntry.Name +
                            ". Error message: " + ex.ToString());
                        continue;
                    }
                }
            }

            if (!directory.EnumerateFileSystemInfos().Any())
                directory.Delete();
        }
        catch (Exception ex)
        {
            Logger.Log("PruneFiles: An error occurred while pruning files from " +
               directory.Name + ". Message: " + ex.ToString());
        }
    }

    /// <summary>
    /// Move log files from obsolete directories to currently used ones and adjust filenames.
    /// </summary>
    private static void MigrateOldLogFiles()
    {
        MigrateLogFiles(SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs"), "ClientCrashLog*.txt");
        MigrateLogFiles(SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "GameCrashLogs"), "EXCEPT*.txt");
        MigrateLogFiles(SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "SyncErrorLogs"), "SYNC*.txt");
    }

    /// <summary>
    /// Move log files matching given search pattern from ErrorLogs to the new directory.
    /// </summary>
    private static void MigrateLogFiles(DirectoryInfo newDirectory, string searchPattern)
    {
        DirectoryInfo currentDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ErrorLogs");
        try
        {
            if (!currentDirectory.Exists)
                return;

            if (!newDirectory.Exists)
                newDirectory.Create();

            foreach (FileInfo file in currentDirectory.EnumerateFiles(searchPattern))
            {
                string filenameTS = Path.GetFileNameWithoutExtension(file.Name);
                string[] ts = filenameTS.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);

                string timestamp = string.Empty;
                string baseFilename = Path.GetFileNameWithoutExtension(ts[0]);

                if (ts.Length >= 6)
                {
                    timestamp = string.Format("_{0}_{1}_{2}_{3}_{4}",
                        ts[3], ts[2].PadLeft(2, '0'), ts[1].PadLeft(2, '0'), ts[4].PadLeft(2, '0'), ts[5].PadLeft(2, '0'));
                }

                string newFilename = SafePath.CombineFilePath(newDirectory.FullName, baseFilename, timestamp, file.Extension);
                file.MoveTo(newFilename);
            }

            if (!currentDirectory.EnumerateFiles().Any())
                currentDirectory.Delete();
        }
        catch (Exception ex)
        {
            Logger.Log("MigrateLogFiles: An error occured while moving log files from " +
                currentDirectory.Name + " to " +
                newDirectory.Name + ". Message: " + ex.ToString());
        }
    }

    // ===================================================================
    // Resolution helpers (same as DXMainClient ScreenResolution)
    // ===================================================================

    /// <summary>
    /// Gets the best recommended resolution from ClientConfiguration.
    /// Replicates ScreenResolution.GetBestRecommendedResolution() without XNA dependency.
    /// </summary>
    private static (int Width, int Height) GetBestRecommendedResolution()
    {
        var recommended = ClientConfiguration.Instance.RecommendedResolutions
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToList();

        if (recommended.Count > 0)
        {
            string best = recommended[0];
            int bestArea = 0;
            foreach (var res in recommended)
            {
                var parts = res.Split('x');
                if (parts.Length == 2
                    && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                {
                    int area = w * h;
                    if (area > bestArea)
                    {
                        best = res;
                        bestArea = area;
                    }
                }
            }
            var bestParts = best.Split('x');
            if (bestParts.Length == 2
                && int.TryParse(bestParts[0], out int bw) && int.TryParse(bestParts[1], out int bh))
                return (bw, bh);
        }

        return (1920, 1080);
    }

    /// <summary>
    /// Gets a safe full-screen resolution default.
    /// Replicates ScreenResolution.SafeFullScreenResolution without XNA dependency.
    /// </summary>
    private static (int Width, int Height) GetSafeFullScreenResolution()
    {
        return (3840, 2160);
    }
}
