using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Messages;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Campaign;
using AvClientViewModel.Domain;
using AvClientViewModel.Domain.Multiplayer.CnCNet;
using AvClientViewModel.Multiplayer;
using AvClientViewModel.Multiplayer.CnCNet;
using AvClientViewModel.Multiplayer.GameLobby;
using AvClientViewModel.Online;
using AvClientViewModel.Services;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.I18N;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the main menu.
    /// Handles update lifecycle, music state, CnCNet player count, file checks,
    /// Discord integration, and navigation commands.
    /// Self-sufficient: subscribes to service events in constructor.
    /// </summary>
    public partial class MainMenuViewModel : ObservableObject, IMainMenuViewModel
    {
        private const double UPDATE_RE_CHECK_THRESHOLD = 30.0;

        private readonly IUpdateService updateService;
        private readonly IGameProcessService gameProcessService;
        private readonly IDiscordHandlerService discordHandler;
        private readonly IMusicPlayerService musicPlayer;
        private readonly IUIThreadMarshaller uiThreadMarshaller;
        private readonly IApplicationLifecycleService lifecycleService;
        private readonly CnCNetManager connectionManager;
        private readonly OptionsWindowViewModel optionsWindowViewModel;
        private readonly TopBarViewModel topBarViewModel;
        private readonly CampaignSelectorViewModel campaignSelectorViewModel;
        private readonly CampaignTagSelectorViewModel campaignTagSelectorViewModel;
        private readonly GameLoadingWindowViewModel gameLoadingWindowViewModel;
        private readonly ExtrasWindowViewModel extrasWindowViewModel;
        private readonly StatisticsWindowViewModel statisticsWindowViewModel;
        private readonly UpdateWindowViewModel updateWindowViewModel;
        private readonly CnCNetUserData cncNetUserData;
        private readonly SkirmishLobbyViewModel skirmishLobbyViewModel;
        private readonly CnCNetLobbyViewModel cncNetLobbyViewModel;
        private readonly LANLobbyViewModel lanLobbyViewModel;
        private readonly PrivateMessagingWindowViewModel privateMessagingWindowViewModel;
        private readonly DialogService dialogService;
        private readonly IProcessLifecycleService processLifecycleService;

        private CancellationTokenSource cncnetPlayerCountCancellationSource;
        private DateTime lastUpdateCheckTime;

        [ObservableProperty]
        public partial string VersionText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string UpdateStatusText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsUpdateStatusEnabled { get; set; }

        [ObservableProperty]
        public partial bool IsUpdateStatusUnderlined { get; set; }

        [ObservableProperty]
        public partial string CnCNetPlayerCountText { get; set; } = "-";

        [ObservableProperty]
        public partial bool AreButtonsEnabled { get; set; } = true;

        [ObservableProperty]
        public partial bool IsUpdateNotificationVisible { get; set; }

        [ObservableProperty]
        public partial string UpdateNotificationText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsMapEditorButtonVisible { get; set; }

        [ObservableProperty]
        public partial bool IsStatisticsButtonVisible { get; set; } = true;

        [ObservableProperty]
        public partial bool ShowVersionInfo { get; set; }

        [ObservableProperty]
        public partial bool IsMusicPlaying { get; set; }

        [ObservableProperty]
        public partial bool IsLanMode { get; set; }

        [ObservableProperty]
        public partial MainMenuPanel ActivePanel { get; set; } = MainMenuPanel.PRIMARY;

        /// <summary>
        /// Domain event: fired when skirmish lobby is exited. Parent subscribes.
        /// </summary>
        public event Action? SkirmishLobbyExited;

        /// <summary>
        /// Domain event: fired when LAN lobby is exited. Parent subscribes.
        /// </summary>
        public event Action? LanLobbyExited;

        /// <summary>
        /// Domain event: fired when options window is closed. Parent subscribes.
        /// </summary>
        public event Action? OptionsWindowClosed;

        public ITopBarViewModel TopBarViewModel => topBarViewModel;

        public MainMenuViewModel(
            IUpdateService updateService,
            IGameProcessService gameProcessService,
            IDiscordHandlerService discordHandler,
            IMusicPlayerService musicPlayer,
            IUIThreadMarshaller uiThreadMarshaller,
            IApplicationLifecycleService lifecycleService,
            CnCNetManager connectionManager,
            OptionsWindowViewModel optionsWindowViewModel,
            TopBarViewModel topBarViewModel,
            CampaignSelectorViewModel campaignSelectorViewModel,
            CampaignTagSelectorViewModel campaignTagSelectorViewModel,
            GameLoadingWindowViewModel gameLoadingWindowViewModel,
            ExtrasWindowViewModel extrasWindowViewModel,
            StatisticsWindowViewModel statisticsWindowViewModel,
            UpdateWindowViewModel updateWindowViewModel,
            CnCNetUserData cncNetUserData,
            SkirmishLobbyViewModel skirmishLobbyViewModel,
            CnCNetLobbyViewModel cncNetLobbyViewModel,
            LANLobbyViewModel lanLobbyViewModel,
            PrivateMessagingWindowViewModel privateMessagingWindowViewModel,
            DialogService dialogService,
            IProcessLifecycleService processLifecycleService)
        {
            this.updateService = updateService;
            this.gameProcessService = gameProcessService;
            this.discordHandler = discordHandler;
            this.musicPlayer = musicPlayer;
            this.uiThreadMarshaller = uiThreadMarshaller;
            this.lifecycleService = lifecycleService;
            this.processLifecycleService = processLifecycleService;
            this.connectionManager = connectionManager;
            this.optionsWindowViewModel = optionsWindowViewModel;
            this.topBarViewModel = topBarViewModel;
            this.campaignSelectorViewModel = campaignSelectorViewModel;
            this.campaignTagSelectorViewModel = campaignTagSelectorViewModel;
            this.gameLoadingWindowViewModel = gameLoadingWindowViewModel;
            this.extrasWindowViewModel = extrasWindowViewModel;
            this.statisticsWindowViewModel = statisticsWindowViewModel;
            this.updateWindowViewModel = updateWindowViewModel;
            this.cncNetUserData = cncNetUserData;
            this.skirmishLobbyViewModel = skirmishLobbyViewModel;
            this.cncNetLobbyViewModel = cncNetLobbyViewModel;
            this.lanLobbyViewModel = lanLobbyViewModel;
            this.privateMessagingWindowViewModel = privateMessagingWindowViewModel;
            this.dialogService = dialogService;

            AppDomain.CurrentDomain.ProcessExit += (_, _) => Clean();
            lifecycleService.ApplicationClosing += (_, _) => Clean();

            // Subscribe to TopBar state changes for panel switching
            topBarViewModel.PropertyChanged += OnTopBarPropertyChanged;

            // Subscribe to options window closed to trigger custom component dialog
            optionsWindowViewModel.PropertyChanged += OnOptionsWindowPropertyChanged;

            // Handle restart request from options window
            optionsWindowViewModel.RestartRequested += OnRestartRequested;

            // Subscribe to child lobby visibility changes for exit detection
            skirmishLobbyViewModel.PropertyChanged += OnSkirmishLobbyPropertyChanged;
            lanLobbyViewModel.PropertyChanged += OnLanLobbyPropertyChanged;
            cncNetLobbyViewModel.PropertyChanged += OnCnCNetLobbyPropertyChanged;

            // Wire CnCNetLobbyViewModel child references
            cncNetLobbyViewModel.SetPrivateMessagingWindow(privateMessagingWindowViewModel);

            // Initialize child ViewModels (one-time setup)
            // Note: StatisticsWindow must be initialized before any lobbies that extend GameLobbyBase,
            // because StatisticsManager is accessed when initializing GameLobbyBase (map rank display).
            statisticsWindowViewModel.Initialize();

            skirmishLobbyViewModel.Initialize();
            lanLobbyViewModel.Initialize();
            cncNetLobbyViewModel.Initialize();
            privateMessagingWindowViewModel.Initialize();
            updateWindowViewModel.Initialize();
            campaignTagSelectorViewModel.Initialize();

            ShowVersionInfo = !ClientConfiguration.Instance.ModMode;

            IsMapEditorButtonVisible = !string.IsNullOrEmpty(ClientConfiguration.Instance.MapEditorExePath);

            VersionText = updateService.GameVersion;

            // Subscribe to events
            gameProcessService.GameProcessStarted += OnGameProcessStarted;
            gameProcessService.GameProcessStarting += OnGameProcessStarting;
            gameProcessService.GameProcessExited += OnGameProcessExitedInternal;

            CnCNetPlayerCountTask.CnCNetGameCountUpdated += OnCnCNetGameCountUpdated;
            cncnetPlayerCountCancellationSource = new CancellationTokenSource();
            CnCNetPlayerCountTask.InitializeService(cncnetPlayerCountCancellationSource);

            UserINISettings.Instance.SettingsSaved += OnSettingsSaved;

            updateService.Restart += OnUpdaterRestart;
            updateService.FileIdentifiersUpdated += OnFileIdentifiersUpdated;
            updateService.OnCustomComponentsOutdated += OnCustomComponentsOutdated;
            updateService.UpdateCompleted += OnUpdateCompleted;
            updateService.UpdateCancelled += OnUpdateCancelled;
            updateService.UpdateFailed += OnUpdateFailed;

            // Music
            LoadAndPlayMusic();

            // Update check
            if (!ClientConfiguration.Instance.ModMode)
            {
                if (updateService.UpdateMirrors.Count < 1)
                {
                    UpdateStatusText = "No update download mirrors available.".L10N("Client:Main:NoUpdateMirrorsAvailable");
                    IsUpdateStatusUnderlined = false;
                }
                else if (UserINISettings.Instance.CheckForUpdates)
                {
                    CheckForUpdates();
                }
                else
                {
                    UpdateStatusText = "Click to check for updates.".L10N("Client:Main:ClickToCheckUpdate");
                }
            }

            CheckRequiredFiles();
            CheckForbiddenFiles();
            CheckIfFirstRun();
            CheckAndApplyTranslationGameFiles();

            Log.Information("Main menu initialization complete.");
        }

        partial void OnIsLanModeChanged(bool value)
        {
            if (value && connectionManager.IsConnected)
                connectionManager.Disconnect();
        }

        #region Commands

        [RelayCommand]
        private void StartCampaign()
        {
            if (ClientConfiguration.Instance.CampaignTagSelectorEnabled)
                campaignTagSelectorViewModel.Open();
            else
                campaignSelectorViewModel.IsVisible = true;
        }

        [RelayCommand]
        private void ContinueCampaign()
        {
            if (ClientConfiguration.Instance.CampaignTagSelectorEnabled)
                campaignTagSelectorViewModel.Open();
            else
                campaignSelectorViewModel.IsVisible = true;
        }

        [RelayCommand]
        private void LoadGame()
        {
            gameLoadingWindowViewModel.IsVisible = true;
        }

        [RelayCommand]
        private void StartSkirmish()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                musicPlayer.Stop();

            skirmishLobbyViewModel.Open();
            skirmishLobbyViewModel.IsVisible = true;
        }

        [RelayCommand]
        private void JoinCnCNet()
        {
            ActivePanel = MainMenuPanel.SECONDARY;
            topBarViewModel.IsExpanded = true;
            cncNetLobbyViewModel.SwitchOn();
        }

        [RelayCommand]
        private void HostLANGame()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                musicPlayer.Stop();

            if (connectionManager.IsConnected)
                connectionManager.Disconnect();

            IsLanMode = true;
            lanLobbyViewModel.Open();
            lanLobbyViewModel.IsVisible = true;
        }

        [RelayCommand]
        private void OpenOptions()
        {
            optionsWindowViewModel.Open();
        }

        [RelayCommand]
        private void OpenMapEditor()
        {
            LaunchMapEditor();
        }

        [RelayCommand]
        private void OpenStatistics()
        {
            statisticsWindowViewModel.IsVisible = true;
        }

        [RelayCommand]
        private void OpenCredits()
        {
            ProcessLauncher.StartShellProcess(ClientConfiguration.Instance.CreditsURL);
        }

        [RelayCommand]
        private void OpenExtras()
        {
            extrasWindowViewModel.IsVisible = true;
        }

        [RelayCommand]
        private void Exit()
        {
            musicPlayer.StartExitFade(0.025f * (float)UserINISettings.Instance.ClientVolume, () =>
            {
                uiThreadMarshaller.AddCallback(new Action(UI_ExitClient));
            });
        }

        [RelayCommand]
        private void CheckForUpdates()
        {
            if (updateService.UpdateMirrors.Count < 1)
                return;

            updateService.CheckForUpdates();
            IsUpdateStatusEnabled = false;
            UpdateStatusText = "Checking for updates...".L10N("Client:Main:CheckingForUpdates");
            lastUpdateCheckTime = DateTime.Now;
        }

        [RelayCommand]
        private void UpdateStatus()
        {
            Log.Information(updateService.VersionState.ToString());

            if (updateService.VersionState == VersionState.OUTDATED ||
                updateService.VersionState == VersionState.MISMATCHED ||
                updateService.VersionState == VersionState.UNKNOWN ||
                updateService.VersionState == VersionState.UPTODATE)
            {
                CheckForUpdates();
            }
        }

        [RelayCommand]
        private void OpenVersion()
        {
            ProcessLauncher.StartShellProcess(ClientConfiguration.Instance.ChangelogURL);
        }

        [RelayCommand]
        private void DeclineUpdate()
        {
            UpdateStatusText = "An update is available, click to install.".L10N("Client:Main:UpdateAvailableClickToInstall");
            IsUpdateStatusEnabled = true;
            IsUpdateStatusUnderlined = true;
        }

        [RelayCommand]
        private void AcceptUpdate()
        {
            updateWindowViewModel.SetData(updateService.ServerGameVersion);
            updateWindowViewModel.IsVisible = true;
            UpdateStatusText = "Updating...".L10N("Client:Main:Updating");
            AreButtonsEnabled = false;
            updateService.StartUpdate();
        }

        [RelayCommand]
        private void ForceUpdateCommand()
        {
            AreButtonsEnabled = false;
            optionsWindowViewModel.OnClosed();
            updateWindowViewModel.IsVisible = true;
            updateWindowViewModel.ForceUpdate();
            UpdateStatusText = "Force updating...".L10N("Client:Main:ForceUpdating");
        }

        #endregion

        #region Lifecycle (called by MainMenu on concrete class)

        public void OnSkirmishLobbyExited()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                musicPlayer.PlayThemeSong();

            SkirmishLobbyExited?.Invoke();
        }

        public void OnLanLobbyExited()
        {
            IsLanMode = false;

            if (UserINISettings.Instance.AutomaticCnCNetLogin)
                connectionManager.Connect();

            if (UserINISettings.Instance.StopMusicOnMenu)
                musicPlayer.PlayThemeSong();

            LanLobbyExited?.Invoke();
        }

        public void OnOptionsWindowClosed()
        {
            OptionsWindowClosed?.Invoke();
        }

        public void SwitchOn()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                musicPlayer.PlayThemeSong();

            if (!ClientConfiguration.Instance.ModMode && UserINISettings.Instance.CheckForUpdates)
            {
                if ((DateTime.Now - lastUpdateCheckTime) > TimeSpan.FromSeconds(UPDATE_RE_CHECK_THRESHOLD))
                    CheckForUpdates();
            }
        }

        public void SwitchOff()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                musicPlayer.StartFadeOut(1.0f, null);
        }

        public void Clean()
        {
            updateService.FileIdentifiersUpdated -= OnFileIdentifiersUpdated;

            cncnetPlayerCountCancellationSource?.Cancel();
            topBarViewModel.Clean();

            if (AreButtonsEnabled == false)
                updateService.StopUpdate();

            if (connectionManager.IsConnected)
                connectionManager.Disconnect();

            cncNetUserData.Save();
            musicPlayer.Dispose();
        }

        #endregion

        #region Event Handlers

        private void OnTopBarPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ITopBarViewModel.LastSwitchType))
            {
                var switchType = topBarViewModel.LastSwitchType;

                // Hide all child views before switching
                cncNetLobbyViewModel.IsVisible = false;
                privateMessagingWindowViewModel.IsVisible = false;

                ActivePanel = switchType switch
                {
                    SwitchType.PRIMARY => MainMenuPanel.PRIMARY,
                    SwitchType.SECONDARY => MainMenuPanel.SECONDARY,
                    _ => MainMenuPanel.PRIMARY
                };

                // Show the appropriate child view
                if (switchType == SwitchType.SECONDARY)
                {
                    cncNetLobbyViewModel.IsVisible = true;
                }
                else if (switchType == SwitchType.PRIVATE_MESSAGES)
                {
                    privateMessagingWindowViewModel.IsVisible = true;
                }
            }
        }

        private void OnOptionsWindowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptionsWindowViewModel.IsVisible) && !optionsWindowViewModel.IsVisible)
            {
                OnOptionsWindowClosed();
            }
        }

        private void OnSkirmishLobbyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SkirmishLobbyViewModel.IsVisible) && !skirmishLobbyViewModel.IsVisible)
            {
                OnSkirmishLobbyExited();
            }
        }

        private void OnLanLobbyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LANLobbyViewModel.IsVisible) && !lanLobbyViewModel.IsVisible)
            {
                OnLanLobbyExited();
            }
        }

        private void OnCnCNetLobbyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CnCNetLobbyViewModel.IsVisible) && !cncNetLobbyViewModel.IsVisible)
            {
                cncNetLobbyViewModel.SwitchOff();
                topBarViewModel.IsExpanded = false;
                ActivePanel = MainMenuPanel.PRIMARY;
            }
        }

        private void OnGameProcessStarted()
        {
            // Original calls MusicOff() which initiates a fade-out
            musicPlayer.StartFadeOut(1.0f, null);
            IsMusicPlaying = false;
        }

        private void OnGameProcessStarting()
        {
            UserINISettings.Instance.ReloadSettings();

            try
            {
                optionsWindowViewModel.RefreshSettings();
            }
            catch (Exception ex)
            {
                Log.Warning("Refreshing settings failed: " + ex.ToString());
            }
        }

        private void OnGameProcessExitedInternal()
        {
            // Music playback on current (threadpool) thread - not on UI thread
            if (!UserINISettings.Instance.StopMusicOnMenu ||
                (!IsLanMode && topBarViewModel.LastSwitchType == SwitchType.PRIMARY))
                musicPlayer.PlayThemeSong();

            uiThreadMarshaller.AddCallback(() => gameLoadingWindowViewModel.UI_ListSaves());
        }

        private void OnUpdaterRestart(object? sender, EventArgs e)
        {
            ExitClient();
        }

        private void OnSettingsSaved(object? sender, EventArgs e)
        {
            // Music state management: if music is playing and user disabled it, fade out.
            // If music is not playing and user enabled it while on main menu, play.
            if (musicPlayer.IsAvailable)
            {
                if (musicPlayer.IsPlaying)
                {
                    if (!UserINISettings.Instance.PlayMainMenuMusic)
                        musicPlayer.StartFadeOut(1.0f, null);
                }
                else if (topBarViewModel.LastSwitchType == SwitchType.PRIMARY && !IsLanMode)
                {
                    musicPlayer.PlayThemeSong();
                }
            }

            if (!connectionManager.IsConnected)
                ProgramConstants.PLAYERNAME = UserINISettings.Instance.PlayerName;

            if (UserINISettings.Instance.DiscordIntegration && !ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled)
                discordHandler.Connect();
            else
                discordHandler.Disconnect();
        }

        private void OnCnCNetGameCountUpdated(object? sender, PlayerCountEventArgs e)
        {
            if (e.PlayerCount == -1)
                CnCNetPlayerCountText = "N/A".L10N("Client:Main:N/A");
            else
                CnCNetPlayerCountText = e.PlayerCount.ToString();
        }

        private void OnFileIdentifiersUpdated()
        {
            uiThreadMarshaller.AddCallback(new Action(UI_HandleFileIdentifierUpdate));
        }

        private void OnCustomComponentsOutdated()
        {
            if (IsUpdateNotificationVisible)
                return;

            if (!AreButtonsEnabled)
                return;

            dialogService.ShowYesNoDialog(
                "Custom Component Updates Available".L10N("Client:Main:CustomUpdateAvailableTitle"),
                "Updates for custom components are available. Do you want to open\nthe Options menu where you can update the custom components?".L10N("Client:Main:CustomUpdateAvailableText"))
                .ContinueWith(task =>
                {
                    bool yes = task.Result; if (yes)
                    {
                        optionsWindowViewModel.Open();
                        optionsWindowViewModel.SwitchToCustomComponentsPanel();
                    }
                });
        }

        private void OnUpdateCompleted(object? sender, EventArgs e)
        {
            AreButtonsEnabled = true;
            UpdateStatusText = string.Format("{0} was succesfully updated to v.{1}".L10N("Client:Main:UpdateSuccess"),
                MainClientConstants.GAME_NAME_SHORT, updateService.GameVersion);
            VersionText = updateService.GameVersion;
            IsUpdateStatusEnabled = true;
            IsUpdateStatusUnderlined = false;

            CheckAndApplyTranslationGameFiles(skipVersionCheck: true);
        }

        private void OnUpdateCancelled(object? sender, EventArgs e)
        {
            AreButtonsEnabled = true;
            UpdateStatusText = "The update was cancelled. Click to retry.".L10N("Client:Main:UpdateCancelledClickToRetry");
            IsUpdateStatusUnderlined = true;
            IsUpdateStatusEnabled = true;
        }

        private void OnUpdateFailed(object? sender, UpdateFailureEventArgs e)
        {
            AreButtonsEnabled = true;
            UpdateStatusText = "Updating failed! Click to retry.".L10N("Client:Main:UpdateFailedClickToRetry");
            IsUpdateStatusUnderlined = true;
            IsUpdateStatusEnabled = true;

            _ = dialogService.ShowOKDialog(
                    "Update failed".L10N("Client:Main:UpdateFailedTitle"),
                    string.Format(("An error occured while updating. Returned error was: {0}\n\nIf you are connected to the Internet and your firewall isn't blocking\n{1}, and the issue is reproducible, contact us at\n{2} for support.").L10N("Client:Main:UpdateFailedText"),
                        e.Reason, Path.GetFileName(ProgramConstants.StartupExecutable), MainClientConstants.SUPPORT_URL_SHORT));
        }

        #endregion

        #region Update UI

        private void UI_HandleFileIdentifierUpdate()
        {
            if (!AreButtonsEnabled)
                return;

            if (updateService.VersionState == VersionState.UPTODATE)
            {
                UpdateStatusText = string.Format("{0} is up to date.".L10N("Client:Main:GameUpToDate"), MainClientConstants.GAME_NAME_SHORT);
                IsUpdateStatusEnabled = true;
                IsUpdateStatusUnderlined = false;
            }
            else if (updateService.VersionState == VersionState.OUTDATED && updateService.ManualUpdateRequired)
            {
                UpdateStatusText = "An update is available. Manual download & installation required.".L10N("Client:Main:UpdateAvailableManualDownloadRequired");
                IsUpdateStatusEnabled = true;
                IsUpdateStatusUnderlined = false;
            }
            else if (updateService.VersionState == VersionState.OUTDATED)
            {
                UpdateStatusText = "An update is available.".L10N("Client:Main:UpdateAvailable");
            }
            else if (updateService.VersionState == VersionState.UNKNOWN)
            {
                UpdateStatusText = "Checking for updates failed! Click to retry.".L10N("Client:Main:CheckUpdateFailedClickToRetry");
                IsUpdateStatusEnabled = true;
                IsUpdateStatusUnderlined = true;
            }
        }

        #endregion

        #region File Checks

        private void CheckRequiredFiles()
        {
            List<string> absentFiles = ClientConfiguration.Instance.RequiredFiles.ToList()
                .FindAll(f => !string.IsNullOrWhiteSpace(f) && !SafePath.GetFile(ProgramConstants.GamePath, f).Exists);

            if (absentFiles.Count > 0)
            {
                string description = string.Empty;
                if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
                {
                    description = ("You are missing Yuri's Revenge files that are required\n" +
                        "to play this mod! Yuri's Revenge mods are not standalone,\n" +
                        "so you need a copy of following Yuri's Revenge (v.1.001)\n" +
                        "files placed in the mod folder to play the mod:").L10N("Client:Main:MissingFilesText1Ares");
                }
                else
                {
                    description = "The following required files are missing:".L10N("Client:Main:MissingFilesText1NonAres");
                }

                description += Environment.NewLine + Environment.NewLine +
                    String.Join(Environment.NewLine, absentFiles) +
                    Environment.NewLine + Environment.NewLine +
                    "You won't be able to play without those files.".L10N("Client:Main:MissingFilesText2");

                _ = dialogService.ShowOKDialog("Missing Files".L10N("Client:Main:MissingFilesTitle"), description);
            }
        }

        private void CheckForbiddenFiles()
        {
            List<string> presentFiles = ClientConfiguration.Instance.ForbiddenFiles.ToList()
                .FindAll(f => !string.IsNullOrWhiteSpace(f) && SafePath.GetFile(ProgramConstants.GamePath, f).Exists);

            if (presentFiles.Count > 0)
            {
                string description;
                if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
                {
                    description = ("You have installed the mod on top of a Tiberian Sun\n" +
                    "copy! This mod is standalone, therefore you have to\n" +
                    "install it in an empty folder. Otherwise the mod won't\n" +
                    "function correctly.\n\n" +
                    "Please reinstall the mod into an empty folder to play.").L10N("Client:Main:InterferingFilesDetectedTextTS");
                }
                else
                {
                    description = "The following interfering files are present:".L10N("Client:Main:InterferingFilesDetectedTextNonTS1") +
                    Environment.NewLine + Environment.NewLine +
                    String.Join(Environment.NewLine, presentFiles) +
                    Environment.NewLine + Environment.NewLine +
                    "The mod won't work correctly without those files removed.".L10N("Client:Main:InterferingFilesDetectedTextNonTS2");
                }

                _ = dialogService.ShowOKDialog("Interfering Files Detected".L10N("Client:Main:InterferingFilesDetectedTitle"), description);
            }
        }

        private void CheckIfFirstRun()
        {
            if (UserINISettings.Instance.IsFirstRun)
            {
                UserINISettings.Instance.IsFirstRun.Value = false;
                UserINISettings.Instance.SaveSettings();

                dialogService.ShowYesNoDialog(
                    "Initial Installation".L10N("Client:Main:InitialInstallationTitle"),
                    string.Format(("You have just installed {0}.\n" +
                        "It's highly recommended that you configure your settings before playing.\n" +
                        "Do you want to configure them now?").L10N("Client:Main:InitialInstallationText"),
                        ClientConfiguration.Instance.LocalGame))
                    .ContinueWith(task =>
                    {
                        bool yes = task.Result;

                        if (yes)
                            optionsWindowViewModel.Open();
                    });
            }

            optionsWindowViewModel.PostInit();
        }

        private void CheckAndApplyTranslationGameFiles(bool skipVersionCheck = false)
        {
            if (!skipVersionCheck && !ClientConfiguration.Instance.ModMode &&
                UserINISettings.Instance.TranslationGameFilesVersion.Value == updateService.GameVersion)
                return;

            try
            {
                Translation.Instance.ApplyTranslationGameFiles();
                UserINISettings.Instance.TranslationGameFilesVersion.Value = updateService.GameVersion;
                UserINISettings.Instance.SaveSettings();
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to apply translation game files. " + ex.ToString());
                _ = dialogService.ShowOKDialog(
                    "Applying Translation Files Failed".L10N("Client:Main:ApplyTranslationFilesFailTitle"),
                    "Applying translation files failed! Error message:".L10N("Client:Main:ApplyTranslationFilesFailText") + " " + ex.Message);
            }
        }

        #endregion

        #region Music

        private void LoadAndPlayMusic()
        {
            if (!musicPlayer.IsAvailable)
                return;

            musicPlayer.PlayThemeSong();
        }

        #endregion

        #region Misc

        private void LaunchMapEditor()
        {
            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();
            using var mapEditorProcess = new System.Diagnostics.Process();

            if (osVersion != OSVersion.UNIX)
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MapEditorExePath);
            else
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.UnixMapEditorExePath);

            mapEditorProcess.StartInfo.UseShellExecute = false;
            mapEditorProcess.Start();
        }

        private void ExitClient()
        {
            UI_ExitClient();
        }

        private void OnRestartRequested(object? sender, EventArgs e)
        {
            Log.Information("Restarting client.");
            Clean();
            processLifecycleService.Restart();
        }

        private void UI_ExitClient()
        {
            Log.Information("Exiting.");
            lifecycleService.Shutdown();
        }

        #endregion
    }
}



