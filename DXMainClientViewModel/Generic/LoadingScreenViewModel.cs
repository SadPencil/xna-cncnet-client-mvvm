
using CommunityToolkit.Mvvm.ComponentModel;
using ClientCore;
using ClientCore.Extensions;
using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Online;
using Rampastring.Tools;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the loading screen.
    /// Handles updater initialization, map loading, and startup sequence.
    /// Self-sufficient: polls for task completion internally via timer.
    /// </summary>
    public partial class LoadingScreenViewModel : ObservableObject, ILoadingScreenViewModel
    {
        private readonly CnCNetManager cncnetManager;
        private readonly MapLoader mapLoader;
        private readonly IUpdateService updateService;

        [ObservableProperty]
        private string statusText = "Loading...";

        [ObservableProperty]
        private string currentTaskText = "Initializing...";

        [ObservableProperty]
        private int progressPercentage;

        [ObservableProperty]
        private bool isIndeterminate = true;

        [ObservableProperty]
        private bool isLoading = true;

        /// <summary>
        /// Raised when the loading is complete and the main menu should be shown.
        /// </summary>
        public event Action LoadingComplete;

        /// <summary>
        /// Raised when the privacy notification should be shown.
        /// </summary>
        public event Action PrivacyNotificationRequested;

        /// <summary>
        /// Raised when CnCNet connection should be initiated.
        /// </summary>
        public event Action CnCNetConnectRequested;

        private Task updaterInitTask;
        private Task mapLoadTask;
        private Timer pollingTimer;
        private DateTime lastLogTime = DateTime.MinValue;

        public LoadingScreenViewModel(CnCNetManager cncnetManager, MapLoader mapLoader, IUpdateService updateService)
        {
            this.cncnetManager = cncnetManager;
            this.mapLoader = mapLoader;
            this.updateService = updateService;

            Initialize();
        }

        private void Initialize()
        {
            bool initUpdater = !ClientConfiguration.Instance.ModMode;

            if (initUpdater)
            {
                updaterInitTask = Task.Run(InitUpdater);
            }

            mapLoader.Initialize();
            mapLoadTask = mapLoader.LoadMapsAsync();

            pollingTimer = new Timer(OnPollTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }

        private void OnPollTick(object state)
        {
            PollLoadingStatus();
        }

        private void PollLoadingStatus()
        {
            bool updaterDone = updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion;
            bool mapLoadDone = mapLoadTask.Status == TaskStatus.RanToCompletion;

            if (updaterDone && mapLoadDone)
            {
                pollingTimer?.Dispose();
                pollingTimer = null;
                Finish();
                return;
            }

            bool updaterFaulted = updaterInitTask != null && updaterInitTask.IsFaulted;
            if (updaterFaulted)
            {
                pollingTimer?.Dispose();
                pollingTimer = null;
                throw new Exception("Updater initialization task failed.", updaterInitTask.Exception);
            }

            bool mapLoadFaulted = mapLoadTask.IsFaulted;
            if (mapLoadFaulted)
            {
                pollingTimer?.Dispose();
                pollingTimer = null;
                throw new Exception("Map loading task failed.", mapLoadTask.Exception);
            }

            // Update status text
            if (!updaterDone && !mapLoadDone)
                CurrentTaskText = "Waiting for updater initialization and loading maps...";
            else if (!updaterDone)
                CurrentTaskText = "Waiting for updater initialization...";
            else if (!mapLoadDone)
                CurrentTaskText = "Waiting for loading maps...";
        }

        private void InitUpdater()
        {
            Logger.Log("Updater: Updater initialization task started.");

            updateService.OnLocalFileVersionsChecked += LogGameClientVersion;
            updateService.CheckLocalFileVersions();

            Logger.Log("Updater: Updater initialization task completed.");
        }

        private void LogGameClientVersion()
        {
            Logger.Log($"Game Client Version: {ClientConfiguration.Instance.LocalGame} {updateService.GameVersion}");
            updateService.OnLocalFileVersionsChecked -= LogGameClientVersion;
        }

        private void Finish()
        {
            Logger.Log("LoadingScreen: Finish waiting for updater and map loading tasks. Proceeding to main menu.");

            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ?
                "N/A" : updateService.GameVersion;

            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME, out _) == NameValidationError.None)
            {
                CnCNetConnectRequested?.Invoke();
            }

            if (!UserINISettings.Instance.PrivacyPolicyAccepted)
            {
                PrivacyNotificationRequested?.Invoke();
            }

            IsLoading = false;
            LoadingComplete?.Invoke();

            Logger.Log("Startup complete. Client is ready.");
        }
    }
}
