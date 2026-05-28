// checked
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.I18N;
using DXMainClientViewModel.Domain;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Online;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the main menu.
    /// Handles update lifecycle, music state, CnCNet player count, file checks,
    /// Discord integration, and navigation commands.
    /// </summary>
    public partial class MainMenuViewModel : ObservableObject, IMainMenuViewModel
    {
        private const double UPDATE_RE_CHECK_THRESHOLD = 30.0;

        private readonly IUpdateService updateService;
        private readonly IGameProcessService gameProcessService;
        private readonly IDiscordHandlerService discordHandler;
        private readonly IMusicPlayerService musicPlayer;
        private readonly IUIThreadMarshaller uiThreadMarshaller;
        private readonly CnCNetManager connectionManager;

        private CancellationTokenSource cncnetPlayerCountCancellationSource;
        private DateTime lastUpdateCheckTime;
        private bool customComponentDialogQueued;
        private bool firstRunDialogVisible;

        [ObservableProperty]
        private string versionText = string.Empty;

        [ObservableProperty]
        private string updateStatusText = string.Empty;

        [ObservableProperty]
        private bool isUpdateStatusEnabled;

        [ObservableProperty]
        private bool isUpdateStatusUnderlined;

        [ObservableProperty]
        private string cnCNetPlayerCountText = "-";

        [ObservableProperty]
        private bool areButtonsEnabled = true;

        [ObservableProperty]
        private bool isUpdateNotificationVisible;

        [ObservableProperty]
        private string updateNotificationText = string.Empty;

        [ObservableProperty]
        private bool isMapEditorButtonVisible;

        [ObservableProperty]
        private bool isStatisticsButtonVisible = true;

        [ObservableProperty]
        private bool showVersionInfo;

        [ObservableProperty]
        private bool isMusicPlaying;

        public event Action? ExitRequested;
        public event Action? MusicStopRequested;
        public event Action? MusicPlayRequested;
        public event Action? MusicFadeOutRequested;
        public event Action<string, string>? MessageBoxRequested;
        public event Action<string, string, Action<bool>>? YesNoDialogRequested;
        public event Action? OptionsWindowOpenRequested;
        public event Action? OptionsWindowCustomComponentsRequested;
        public event Action<bool>? LanModeChanged;
        public event Action? CnCNetConnectRequested;
        public event Action? CnCNetDisconnectRequested;
        public event Action? SwitchToSecondaryRequested;
        public event Action? SwitchToPrimaryRequested;

        public MainMenuViewModel(
            IUpdateService updateService,
            IGameProcessService gameProcessService,
            IDiscordHandlerService discordHandler,
            IMusicPlayerService musicPlayer,
            IUIThreadMarshaller uiThreadMarshaller,
            CnCNetManager connectionManager)
        {
            this.updateService = updateService;
            this.gameProcessService = gameProcessService;
            this.discordHandler = discordHandler;
            this.musicPlayer = musicPlayer;
            this.uiThreadMarshaller = uiThreadMarshaller;
            this.connectionManager = connectionManager;
        }

        public void Initialize()
        {
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

            Logger.Log("Main menu initialization complete.");
        }

        #region Commands

        [RelayCommand]
        private void StartCampaign()
        {
            // Navigation handled by View observing this command
        }

        [RelayCommand]
        private void ContinueCampaign()
        {
            // Navigation handled by View observing this command
        }

        [RelayCommand]
        private void LoadGame()
        {
            // Navigation handled by View observing this command
        }

        [RelayCommand]
        private void StartSkirmish()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicStopRequested?.Invoke();
        }

        [RelayCommand]
        private void JoinCnCNet()
        {
            SwitchToSecondaryRequested?.Invoke();
        }

        [RelayCommand]
        private void HostLANGame()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicStopRequested?.Invoke();

            if (connectionManager.IsConnected)
                CnCNetDisconnectRequested?.Invoke();

            LanModeChanged?.Invoke(true);
        }

        [RelayCommand]
        private void OpenOptions()
        {
            OptionsWindowOpenRequested?.Invoke();
        }

        [RelayCommand]
        private void OpenMapEditor()
        {
            LaunchMapEditor();
        }

        [RelayCommand]
        private void OpenStatistics()
        {
            // Navigation handled by View observing this command
        }

        [RelayCommand]
        private void OpenCredits()
        {
            ProcessLauncher.StartShellProcess(ClientConfiguration.Instance.CreditsURL);
        }

        [RelayCommand]
        private void OpenExtras()
        {
            // Navigation handled by View observing this command
        }

        [RelayCommand]
        private void Exit()
        {
            MusicFadeOutRequested?.Invoke();
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
            Logger.Log(updateService.VersionState.ToString());

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
            updateService.StartUpdate();
            UpdateStatusText = "Updating...".L10N("Client:Main:Updating");
            AreButtonsEnabled = false;
        }

        [RelayCommand]
        private void ForceUpdateCommand()
        {
            AreButtonsEnabled = false;
            updateService.ForceUpdate();
            UpdateStatusText = "Force updating...".L10N("Client:Main:ForceUpdating");
        }

        #endregion

        #region Event Handlers

        private void OnGameProcessStarted()
        {
            IsMusicPlaying = false;
        }

        private void OnGameProcessStarting()
        {
            UserINISettings.Instance.ReloadSettings();
        }

        private void OnGameProcessExitedInternal()
        {
            uiThreadMarshaller.AddCallback(new Action(HandleGameProcessExited));
        }

        private void HandleGameProcessExited()
        {
            if (!UserINISettings.Instance.StopMusicOnMenu)
                MusicPlayRequested?.Invoke();
        }

        private void OnUpdaterRestart(object? sender, EventArgs e)
        {
            uiThreadMarshaller.AddCallback(new Action(ExitClient));
        }

        private void OnSettingsSaved(object? sender, EventArgs e)
        {
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
            uiThreadMarshaller.AddCallback(new Action(HandleFileIdentifierUpdate));
        }

        private void OnCustomComponentsOutdated()
        {
            if (IsUpdateNotificationVisible)
                return;

            if (firstRunDialogVisible)
            {
                customComponentDialogQueued = true;
                return;
            }

            customComponentDialogQueued = false;

            YesNoDialogRequested?.Invoke(
                "Custom Component Updates Available".L10N("Client:Main:CustomUpdateAvailableTitle"),
                "Updates for custom components are available. Do you want to open\nthe Options menu where you can update the custom components?".L10N("Client:Main:CustomUpdateAvailableText"),
                yes =>
                {
                    if (yes)
                    {
                        OptionsWindowOpenRequested?.Invoke();
                        OptionsWindowCustomComponentsRequested?.Invoke();
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

            MessageBoxRequested?.Invoke(
                "Update failed".L10N("Client:Main:UpdateFailedTitle"),
                string.Format(("An error occured while updating. Returned error was: {0}\n\nIf you are connected to the Internet and your firewall isn't blocking\n{1}, and the issue is reproducible, contact us at\n{2} for support.").L10N("Client:Main:UpdateFailedText"),
                    e.Reason, Path.GetFileName(ProgramConstants.StartupExecutable), MainClientConstants.SUPPORT_URL_SHORT));
        }

        #endregion

        #region Lifecycle Methods

        public void OnSkirmishLobbyExited()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicPlayRequested?.Invoke();
        }

        public void OnLanLobbyExited()
        {
            LanModeChanged?.Invoke(false);

            if (UserINISettings.Instance.AutomaticCnCNetLogin)
                CnCNetConnectRequested?.Invoke();

            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicPlayRequested?.Invoke();
        }

        public void OnOptionsWindowClosed()
        {
            if (customComponentDialogQueued)
                OnCustomComponentsOutdated();
        }

        public void SwitchOn()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicPlayRequested?.Invoke();

            if (!ClientConfiguration.Instance.ModMode && UserINISettings.Instance.CheckForUpdates)
            {
                if ((DateTime.Now - lastUpdateCheckTime) > TimeSpan.FromSeconds(UPDATE_RE_CHECK_THRESHOLD))
                    CheckForUpdates();
            }
        }

        public void SwitchOff()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicStopRequested?.Invoke();
        }

        public void Clean()
        {
            updateService.FileIdentifiersUpdated -= OnFileIdentifiersUpdated;

            cncnetPlayerCountCancellationSource?.Cancel();

            if (AreButtonsEnabled == false)
                updateService.StopUpdate();

            if (connectionManager.IsConnected)
                connectionManager.Disconnect();
        }

        #endregion

        #region Update UI

        private void HandleFileIdentifierUpdate()
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

                MessageBoxRequested?.Invoke("Missing Files".L10N("Client:Main:MissingFilesTitle"), description);
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

                MessageBoxRequested?.Invoke("Interfering Files Detected".L10N("Client:Main:InterferingFilesDetectedTitle"), description);
            }
        }

        private void CheckIfFirstRun()
        {
            if (UserINISettings.Instance.IsFirstRun)
            {
                UserINISettings.Instance.IsFirstRun.Value = false;
                UserINISettings.Instance.SaveSettings();

                firstRunDialogVisible = true;
                YesNoDialogRequested?.Invoke(
                    "Initial Installation".L10N("Client:Main:InitialInstallationTitle"),
                    string.Format(("You have just installed {0}.\n" +
                        "It's highly recommended that you configure your settings before playing.\n" +
                        "Do you want to configure them now?").L10N("Client:Main:InitialInstallationText"),
                        ClientConfiguration.Instance.LocalGame),
                    yes =>
                    {
                        firstRunDialogVisible = false;
                        if (yes)
                            OptionsWindowOpenRequested?.Invoke();
                        else if (customComponentDialogQueued)
                            OnCustomComponentsOutdated();
                    });
            }
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
                Logger.Log("Failed to apply translation game files. " + ex.ToString());
                MessageBoxRequested?.Invoke(
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
            Logger.Log("Exiting.");
            ExitRequested?.Invoke();
            musicPlayer.Dispose();
        }

        #endregion
    }
}
