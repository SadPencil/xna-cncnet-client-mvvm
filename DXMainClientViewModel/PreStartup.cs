using DXMainClientMvvmContract.Generic;
using DXMainClientMvvmContract.Generic.OptionPanels;
using DXMainClientMvvmContract.Campaign;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Enums;
using ClientCore.I18N;
using ClientCore.INIProcessing;
using ClientCore.Settings;
using ClientUpdater;
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
using DXMainClientMvvmContract.ViewServices;

namespace DXMainClientViewModel;

/// <summary>
/// Initializes client systems before the UI starts.
/// Replicates the original DXMainClient PreStartup + Startup initialization chain.
/// </summary>
public static class PreStartup
{
    /// <summary>
    /// Initializes all non-UI systems.
    /// </summary>
    public static void Initialize()
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

        // --- Delete obsolete files from old target project versions (same as DXMainClient PreStartup) ---
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
                Logger.Log("Deleting wsock32.dll failed! Error message: " + ex.ToString());
            }
        });

        // --- Custom mission initialization (same as DXMainClient PreStartup lines 195-196) ---
        CustomMissionHelper.Initialize();
        CustomMissionHelper.DeleteSupplementalMissionFiles();

        // --- Startup initialization (same as DXMainClient Startup.Execute lines 46-129) ---

        Logger.Log("Initializing updater.");

        SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "version_u");

        Updater.Initialize(
            ProgramConstants.GamePath,
            ProgramConstants.GetBaseResourcePath(),
            ClientConfiguration.Instance.SettingsIniName,
            ClientConfiguration.Instance.LocalGame,
            SafePath.GetFile(ProgramConstants.StartupExecutable).Name);

        Logger.Log("OSDescription: " + RuntimeInformation.OSDescription);
        Logger.Log("OSArchitecture: " + RuntimeInformation.OSArchitecture);
        Logger.Log("ProcessArchitecture: " + RuntimeInformation.ProcessArchitecture);
        Logger.Log("FrameworkDescription: " + RuntimeInformation.FrameworkDescription);
        Logger.Log("Selected OS profile: " + MainClientConstants.OSId);
        Logger.Log("Current culture: " + CultureInfo.CurrentCulture);

        IPreStartupSystemService preStartupSystemService = new PreStartupSystemService();
        preStartupSystemService.StartSystemSpecificationsCheck();
        preStartupSystemService.StartOnlineIdGeneration();

        if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
            Task.Run(() => PruneFiles(SafePath.GetDirectory(ProgramConstants.GamePath, "debug"), DateTime.Now.AddDays(-7)));

        Task.Run(MigrateOldLogFiles);

        // Start INI file preprocessor
        PreprocessorBackgroundTask.Instance.Run();

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

        FinalSunSettings.WriteFinalSunIniAsync();

        preStartupSystemService.WriteInstallPathToRegistryIfNeeded();

        ClientConfiguration.Instance.RefreshSettings();

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
            sp.GetRequiredService<CnCNetUserData>()));
        services.AddSingleton<IMainMenuViewModel>(sp =>
            sp.GetRequiredService<MainMenuViewModel>());

        services.AddTransient<ILoadingScreenViewModel, LoadingScreenViewModel>();
        services.AddTransient<IPrivacyNotificationViewModel, PrivacyNotificationViewModel>();
        services.AddTransient<IUpdateQueryWindowViewModel, UpdateQueryWindowViewModel>();
        services.AddTransient<IManualUpdateQueryWindowViewModel, ManualUpdateQueryWindowViewModel>();
        services.AddTransient<IUpdateWindowViewModel, UpdateWindowViewModel>();
        services.AddTransient<IGameInProgressWindowViewModel, GameInProgressWindowViewModel>();
    }


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
