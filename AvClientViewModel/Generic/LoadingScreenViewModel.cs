using System;
using System.Threading;
using System.Threading.Tasks;

using AvClientMvvmContract.Generic;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.CnCNet;
using AvClientViewModel.Online;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the loading screen.
    /// Handles updater initialization, map loading, and startup sequence.
    /// Self-sufficient: polls for task completion internally via timer.
    /// View observes IsLoading to know when loading is complete.
    /// </summary>
    public partial class LoadingScreenViewModel : ObservableObject, ILoadingScreenViewModel
    {
        private readonly MapLoader mapLoader;
        private readonly IUpdateService updateService;
        private readonly CnCNetManager connectionManager;
        private readonly IUIThreadMarshaller uiThreadMarshaller;

        public event EventHandler? Completed;

        [ObservableProperty]
        public partial string StatusText { get; set; } = "Loading...";

        [ObservableProperty]
        private string currentTaskText = "Initializing...";

        [ObservableProperty]
        private int progressPercentage;

        [ObservableProperty]
        private bool isIndeterminate = true;

        [ObservableProperty]
        private bool isLoading = true;

        public bool IsBorderlessClient => UserINISettings.Instance.BorderlessWindowedClient;

        private Task? updaterInitTask;
        private Task? mapLoadTask;
        private Timer? pollingTimer;

        public LoadingScreenViewModel(MapLoader mapLoader, IUpdateService updateService, CnCNetManager connectionManager, IUIThreadMarshaller uiThreadMarshaller)
        {
            this.mapLoader = mapLoader;
            this.updateService = updateService;
            this.connectionManager = connectionManager;
            this.uiThreadMarshaller = uiThreadMarshaller;

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

        private void OnPollTick(object? state)
        {
            PollLoadingStatus();
        }

        private DateTime lastLogTime = DateTime.MinValue;

        private void PollLoadingStatus()
        {
            bool updaterDone = updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion;
            bool mapLoadDone = mapLoadTask.Status == TaskStatus.RanToCompletion;

            if (updaterDone && mapLoadDone)
            {
                pollingTimer?.Dispose();
                pollingTimer = null;
                CurrentTaskText = "Loading complete.";
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

            // Throttle logging to every 5 seconds (mirrors original Update() behavior)
            var now = DateTime.Now;
            if ((now - lastLogTime).TotalSeconds > 5)
            {
                lastLogTime = now;

                string logMessage;
                if (!updaterDone && !mapLoadDone)
                    logMessage = "LoadingScreen: Waiting for updater initialization and loading maps...";
                else if (!updaterDone)
                    logMessage = "LoadingScreen: Waiting for updater initialization...";
                else if (!mapLoadDone)
                    logMessage = "LoadingScreen: Waiting for loading maps...";
                else
                    throw new Exception("Assert failed. No pending tasks. This should not happen.");

                Log.Information(logMessage);
                CurrentTaskText = logMessage;
            }
        }

        private void InitUpdater()
        {
            Log.Information("Updater: Updater initialization task started.");

            updateService.OnLocalFileVersionsChecked += LogGameClientVersion;
            updateService.CheckLocalFileVersions();

            Log.Information("Updater: Updater initialization task completed.");
        }

        private void LogGameClientVersion()
        {
            Log.Information($"Game Client Version: {ClientConfiguration.Instance.LocalGame} {updateService.GameVersion}");
            updateService.OnLocalFileVersionsChecked -= LogGameClientVersion;
        }

        private void Finish()
        {
            Log.Information("LoadingScreen: Finish waiting for updater and map loading tasks. Proceeding to main menu.");

            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ?
                "N/A" : updateService.GameVersion;

            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME, out _) == NameValidationError.None)
            {
                connectionManager.Connect();
            }

            uiThreadMarshaller.AddCallback(() => IsLoading = false);
            Completed?.Invoke(this, EventArgs.Empty);

            Log.Information("Startup complete. Client is ready.");
        }
    }
}


