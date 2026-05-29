
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;
using DXMainClientViewModel.Domain;
using Rampastring.Tools;
using System;
using System.Threading;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the update window.
    /// Self-sufficient: subscribes to IUpdateService events, applies progress via timer.
    /// Domain events (UpdateCompleted, UpdateCancelled, UpdateFailed) stay on concrete class
    /// for MainMenu subscription - NOT on interface.
    /// </summary>
    public partial class UpdateWindowViewModel : ObservableObject, IUpdateWindowViewModel
    {
        private readonly IUpdateService updateService;
        private readonly IUIThreadMarshaller uiThreadMarshaller;

        [ObservableProperty]
        private string descriptionText = string.Empty;

        [ObservableProperty]
        private string currentFileName = string.Empty;

        [ObservableProperty]
        private int currentFilePercentage;

        [ObservableProperty]
        private int totalPercentage;

        [ObservableProperty]
        private string updaterStatusText = "Preparing".L10N("Client:Main:StatusPreparing");

        [ObservableProperty]
        private bool isVisible;

        /// <summary>
        /// Domain event: raised when update completes. MainMenu subscribes directly.
        /// </summary>
        public event Action? UpdateCompleted;

        /// <summary>
        /// Domain event: raised when update is cancelled. MainMenu subscribes directly.
        /// </summary>
        public event Action? UpdateCancelled;

        /// <summary>
        /// Domain event: raised when update fails. MainMenu subscribes directly.
        /// </summary>
        public event Action<string>? UpdateFailed;

        private bool isStartingForceUpdate;
        private static readonly object locker = new object();
        private bool infoUpdated;
        private string pendingFileName = string.Empty;
        private int pendingFilePercentage;
        private int pendingTotalPercentage;
        private Timer? progressTimer;

        public UpdateWindowViewModel(
            IUpdateService updateService,
            IUIThreadMarshaller uiThreadMarshaller)
        {
            this.updateService = updateService;
            this.uiThreadMarshaller = uiThreadMarshaller;
        }

        public void Initialize()
        {
            updateService.FileIdentifiersUpdated += OnFileIdentifiersUpdated;
            updateService.UpdateCompleted += OnUpdateCompleted;
            updateService.UpdateFailed += OnUpdateFailed;
            updateService.UpdateProgressChanged += OnUpdateProgressChanged;
            updateService.LocalFileCheckProgressChanged += OnLocalFileCheckProgressChanged;
            updateService.FileDownloadCompleted += OnFileDownloadCompleted;

            progressTimer = new Timer(OnProgressTimerTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(50));
        }

        private void OnProgressTimerTick(object? state)
        {
            lock (locker)
            {
                if (!infoUpdated)
                    return;

                infoUpdated = false;

                uiThreadMarshaller.AddCallback(new Action(() =>
                {
                    CurrentFilePercentage = (pendingFilePercentage < 0 || pendingFilePercentage > 100) ? 0 : pendingFilePercentage;
                    TotalPercentage = (pendingTotalPercentage < 0 || pendingTotalPercentage > 100) ? 0 : pendingTotalPercentage;
                    CurrentFileName = pendingFileName;
                    UpdaterStatusText = "Downloading files".L10N("Client:Main:DownloadingFiles");
                }));
            }
        }

        /// <summary>
        /// Configures the window for a normal update. Called by MainMenu.
        /// </summary>
        public void SetData(string newGameVersion)
        {
            DescriptionText = string.Format(
                "Please wait while {0} is updated to version {1}.\nThis window will automatically close once the update is complete.\n\nThe client may also restart after the update has been downloaded.".L10N("Client:Main:UpdateVersionPleaseWait"),
                MainClientConstants.GAME_NAME_SHORT, newGameVersion);
            UpdaterStatusText = "Preparing".L10N("Client:Main:StatusPreparing");
        }

        /// <summary>
        /// Starts a force update. Called by MainMenu.
        /// </summary>
        public void ForceUpdate()
        {
            isStartingForceUpdate = true;
            DescriptionText = string.Format("Force updating {0} to latest version...".L10N("Client:Main:ForceUpdateToLatest"), MainClientConstants.GAME_NAME_SHORT);
            UpdaterStatusText = "Connecting".L10N("Client:Main:UpdateStatusConnecting");
            updateService.CheckForUpdates();
        }

        [RelayCommand]
        private void Cancel()
        {
            if (!isStartingForceUpdate)
                updateService.StopUpdate();

            CloseWindow();
        }

        #region Service Event Handlers

        private void OnFileIdentifiersUpdated()
        {
            if (!isStartingForceUpdate)
                return;

            if (updateService.VersionState == VersionState.UNKNOWN)
            {
                uiThreadMarshaller.AddCallback(new Action(() =>
                {
                    CloseWindow();
                }));
                return;
            }
            else if (updateService.VersionState == VersionState.OUTDATED && updateService.ManualUpdateRequired)
            {
                uiThreadMarshaller.AddCallback(new Action(() =>
                {
                    CloseWindow();
                }));
                return;
            }

            uiThreadMarshaller.AddCallback(new Action(() =>
            {
                SetData(updateService.ServerGameVersion);
                updateService.StartUpdate();
                isStartingForceUpdate = false;
            }));
        }

        private void OnLocalFileCheckProgressChanged(int checkedFileCount, int totalFileCount)
        {
            uiThreadMarshaller.AddCallback(new Action(() =>
            {
                CurrentFilePercentage = checkedFileCount * 100 / totalFileCount;
            }));
        }

        private void OnUpdateProgressChanged(string currFileName, int currFilePercentage, int totalPercentage)
        {
            lock (locker)
            {
                infoUpdated = true;
                pendingFileName = currFileName;
                pendingFilePercentage = currFilePercentage;
                pendingTotalPercentage = totalPercentage;
            }
        }

        private void OnFileDownloadCompleted(string archiveName)
        {
            uiThreadMarshaller.AddCallback(new Action(() =>
            {
                UpdaterStatusText = "Unpacking archive".L10N("Client:Main:UnpackingArchive");
            }));
        }

        private void OnUpdateCompleted(object? sender, EventArgs e)
        {
            uiThreadMarshaller.AddCallback(new Action(() =>
            {
                UpdateCompleted?.Invoke();
            }));
        }

        private void OnUpdateFailed(object? sender, UpdateFailureEventArgs e)
        {
            uiThreadMarshaller.AddCallback(new Action(() =>
            {
                UpdateFailed?.Invoke(e.Reason);
            }));
        }

        #endregion

        private void CloseWindow()
        {
            isStartingForceUpdate = false;
            IsVisible = false;
            UpdateCancelled?.Invoke();
        }
    }
}

