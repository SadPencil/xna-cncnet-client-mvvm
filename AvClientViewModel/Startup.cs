using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using AvClientMvvmContract.Campaign;
using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Generic.OptionPanels;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Campaign;
using AvClientViewModel.Domain;
using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.CnCNet;
using AvClientViewModel.Generic;
using AvClientViewModel.Generic.OptionPanels;
using AvClientViewModel.LAN;
using AvClientViewModel.Multiplayer;
using AvClientViewModel.Multiplayer.CnCNet;
using AvClientViewModel.Multiplayer.GameLobby;
using AvClientViewModel.Online;
using AvClientViewModel.Services;
using AvClientViewModel.Services.Resolutions;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.INIProcessing;
using ClientCore.PlatformShim;
using ClientCore.Settings;

using ClientUpdater;

using Microsoft.Extensions.DependencyInjection;

using Rampastring.Tools;

using Serilog;

using Steamworks;

namespace AvClientViewModel
{
    public static class Startup
    {
        public static void ConfigureServices(ServiceCollection services)
        {
            // Core services
            services.AddSingleton<Random>(_ => new Random()); // TODO: the old client creates the Random instance with a customized method.
            services.AddSingleton<DialogService>();

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
            // Screen info providers tried in order; first applicable wins
            services.AddSingleton<IScreenInfoProvider, LinuxScreenInfoProvider>();
            services.AddSingleton<IScreenInfoProvider, WindowsScreenInfoProvider>();
            services.AddSingleton<IScreenInfoProvider, MacOSScreenInfoProvider>();
            services.AddSingleton<IScreenInfoProvider, DummyScreenInfoProvider>();

            services.AddSingleton<IResolutionProvider, ResolutionProvider>();
            services.AddSingleton<DirectDrawWrapperManager>();
            services.AddSingleton<IFileIntegrityService, FileIntegrityService>();
            services.AddSingleton<ICampaignGameProcessService, CampaignGameProcessService>();

            // CnCNet / Multiplayer domain services
            services.AddSingleton<TunnelHandler>();
            services.AddSingleton<DiscordHandler>();

            // LAN services
            services.AddSingleton<ILANBroadcastManagerService>(sp =>
                new LANBroadcastManagerService(ProgramConstants.LAN_LOBBY_PORT, EncodingExt.UTF8NoBOM));
            services.AddSingleton<ILANPlayerManagerService, LANPlayerManagerService>();
            services.AddSingleton<ILANMessageDeduplicatorService>(sp =>
                new LANMessageDeduplicatorService(sp.GetRequiredService<Random>().Next()));

            // Option panel ViewModels
            services.AddSingleton<DisplayOptionsPanelViewModel>(sp =>
                new DisplayOptionsPanelViewModel(
                    UserINISettings.Instance,
                    sp.GetRequiredService<DirectDrawWrapperManager>(),
                    sp.GetRequiredService<IResolutionProvider>(),
                    sp.GetRequiredService<DialogService>()));
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
                new ComponentsPanelViewModel(sp.GetRequiredService<IUIThreadMarshaller>(),
                    sp.GetRequiredService<DialogService>()));

            services.AddSingleton<IComponentsPanelViewModel>(sp =>
                sp.GetRequiredService<ComponentsPanelViewModel>());

            // OptionsWindowViewModel - resolves all option panels from DI
            services.AddSingleton<OptionsWindowViewModel>(sp => new OptionsWindowViewModel(
                sp.GetRequiredService<DisplayOptionsPanelViewModel>(),
                sp.GetRequiredService<AudioOptionsPanelViewModel>(),
                sp.GetRequiredService<GameOptionsPanelViewModel>(),
                sp.GetRequiredService<CnCNetOptionsPanelViewModel>(),
                sp.GetRequiredService<UpdaterOptionsPanelViewModel>(),
                sp.GetRequiredService<ComponentsPanelViewModel>(),
                sp.GetRequiredService<DialogService>()));

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

            // CampaignTagSelectorViewModel (singleton: MainMenuViewModel holds a reference and sets IsVisible)
            services.AddSingleton<CampaignTagSelectorViewModel>();
            services.AddSingleton<ICampaignTagSelectorViewModel>(sp =>
                sp.GetRequiredService<CampaignTagSelectorViewModel>());

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
                sp.GetRequiredService<CnCNetGameLobbyViewModel>(),
                sp.GetRequiredService<CnCNetGameLoadingLobbyViewModel>(),
                sp.GetRequiredService<IUIThreadMarshaller>(),
                sp.GetRequiredService<IGameProcessService>(),
                sp.GetRequiredService<MapLoader>(),
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
                sp.GetRequiredService<CampaignTagSelectorViewModel>(),
                sp.GetRequiredService<GameLoadingWindowViewModel>(),
                sp.GetRequiredService<ExtrasWindowViewModel>(),
                sp.GetRequiredService<StatisticsWindowViewModel>(),
                sp.GetRequiredService<UpdateWindowViewModel>(),
                sp.GetRequiredService<CnCNetUserData>(),
                sp.GetRequiredService<SkirmishLobbyViewModel>(),
                sp.GetRequiredService<CnCNetLobbyViewModel>(),
                sp.GetRequiredService<LANLobbyViewModel>(),
                sp.GetRequiredService<PrivateMessagingWindowViewModel>(),
                sp.GetRequiredService<DialogService>()));

            services.AddSingleton<IMainMenuViewModel>(sp =>
                sp.GetRequiredService<MainMenuViewModel>());

            services.AddTransient<ILoadingScreenViewModel, LoadingScreenViewModel>();
            services.AddTransient<IPrivacyNotificationViewModel, PrivacyNotificationViewModel>();
            services.AddTransient<IUpdateQueryWindowViewModel, UpdateQueryWindowViewModel>();
            services.AddTransient<IManualUpdateQueryWindowViewModel, ManualUpdateQueryWindowViewModel>();
            services.AddTransient<IUpdateWindowViewModel, UpdateWindowViewModel>();
            services.AddTransient<IGameInProgressWindowViewModel, GameInProgressWindowViewModel>();
            services.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
        }

        public static void Initialize(string[] args)
        {
            Initialize(new StartupParams(noAudio: args.Contains("--noaudio", StringComparer.InvariantCultureIgnoreCase),
                multipleInstanceMode: args.Contains("--multipleinstances", StringComparer.InvariantCultureIgnoreCase),
                unknownParams: args.Except(new[] { "--noaudio", "--multipleinstances" }, StringComparer.InvariantCultureIgnoreCase).ToList()));
        }

        public static void Initialize(StartupParams parameters)
        {
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

            // --- Custom mission initialization (same as DXMainClient PreStartup lines 194-196) ---
            CustomMissionHelper.Initialize();
            CustomMissionHelper.DeleteSupplementalMissionFiles();

            // --- Delete obsolete files from old target project versions (same as DXMainClient PreStartup lines 199-218) ---
            Task.Run(() =>
            {
                DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);

                gameDirectory.EnumerateFiles("mainclient.log").SingleOrDefault()?.Delete();
                gameDirectory.EnumerateFiles("aunchupdt.dat").SingleOrDefault()?.Delete();

                try
                {
                    gameDirectory.EnumerateFiles("wsock32.dll").SingleOrDefault()?.Delete();
                }
                catch (Exception ex)
                {
                    string error = ("Deleting wsock32.dll failed! Please close any " +
                        "applications that could be using the file, and then start the client again." + "\n\n" +
                        "Message:").L10N("Client:Main:DeleteWsock32Failed") + " " + ex.Message;

                    MainClientConstants.DisplayErrorAction(null, error, true);
                }
            });

            // --- Updater initialization (same as DXMainClient Startup.Execute lines 44-48) ---
            Log.Information("Initializing updater.");

            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "version_u");

            Updater.Initialize(
                ProgramConstants.GamePath,
                ProgramConstants.GetBaseResourcePath(),
                ClientConfiguration.Instance.SettingsIniName,
                ClientConfiguration.Instance.LocalGame,
                SafePath.GetFile(ProgramConstants.StartupExecutable).Name);

            // --- OS / Framework info logging (same as DXMainClient Startup.Execute lines 50-55) ---
            Log.Information("OSDescription: " + RuntimeInformation.OSDescription);
            Log.Information("OSArchitecture: " + RuntimeInformation.OSArchitecture);
            Log.Information("ProcessArchitecture: " + RuntimeInformation.ProcessArchitecture);
            Log.Information("FrameworkDescription: " + RuntimeInformation.FrameworkDescription);
            Log.Information("Selected OS profile: " + MainClientConstants.OSId);
            Log.Information("Current culture: " + CultureInfo.CurrentCulture);

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
                Log.Information("Attempting to delete temporary updater directory.");
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
                    Log.Information("Saved Games directory does not exist - attempting to create one.");
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
                Log.Information("Removing partial custom component downloads.");
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

            // --- Steamworks initialization (same as DXMainClient Startup.Execute lines 157-158) ---
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Task.Run(InitSteamworks);

            Log.Information("Startup initialization complete.");
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
                            Log.Warning("PruneFiles: Could not delete file " + fsEntry.Name +
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
                Log.Warning("PruneFiles: An error occurred while pruning files from " +
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
                Log.Warning("MigrateLogFiles: An error occured while moving log files from " +
                    currentDirectory.Name + " to " +
                    newDirectory.Name + ". Message: " + ex.ToString());
            }
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
                        Log.Information("Steam init called");
                        SteamClient.Init(2229850);
                    }
                    else if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
                    {
                        Log.Information("Steam init called");
                        SteamClient.Init(2229880);
                    }
                    else if (ClientConfiguration.Instance.ClientGameType == ClientType.RA)
                    {
                        Log.Information("Steam init called");
                        SteamClient.Init(2229840);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("Steam init failed: " + e.Message);
                    // Couldn't init for some reason (steam is closed etc)
                }
            }
        }

    }

}