using AvMainClientMvvmContract.Generic;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;
using AvMainClientViewModel.Domain.Multiplayer.CnCNet;
using AvMainClientViewModel.Online;
using AvMainClientViewModel.Online.EventArguments;
using Rampastring.Tools;
using System;
using System.Threading;
using System.Timers;
using AvMainClientMvvmContract.ViewServices;

namespace AvMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the top bar.
    /// Handles connection status, player count, view switching, and button state.
    /// Self-sufficient: subscribes to connection events directly.
    /// </summary>
    public partial class TopBarViewModel : ObservableObject, ITopBarViewModel
    {
        private const double DOWN_TIME_WAIT_SECONDS = 1.0;
        private const double EVENT_DOWN_TIME_WAIT_SECONDS = 2.0;
        private const double STARTUP_DOWN_TIME_WAIT_SECONDS = 3.5;

        private readonly CnCNetManager connectionManager;
        private readonly PrivateMessageHandler privateMessageHandler;
        private readonly IUIThreadMarshaller uiThreadMarshaller;
        private readonly OptionsWindowViewModel optionsWindowViewModel;

        private CancellationTokenSource? cncnetPlayerCountCancellationSource;
        private static readonly object locker = new object();

        private System.Timers.Timer? _autoHideTimer;

        [ObservableProperty]
        private string connectionStatusText = "OFFLINE".L10N("Client:Main:StatusOffline");

        [ObservableProperty]
        private string playerCountText = "-";

        [ObservableProperty]
        private bool isPlayerCountVisible;

        [ObservableProperty]
        private string playerCountLabel = string.Empty;

        [ObservableProperty]
        private bool areSwitchButtonsClickable = true;

        [ObservableProperty]
        private bool isOptionsButtonClickable = true;

        [ObservableProperty]
        private bool isLogoutButtonClickable;

        [ObservableProperty]
        private bool isLanMode;

        [ObservableProperty]
        private SwitchType lastSwitchType;

        [ObservableProperty]
        private string mainButtonText = "Main Menu".L10N("Client:Main:MainMenu");

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private int unreadMessageCount;

        /// <summary>
        /// Domain event: fired when user logs out. MainMenu subscribes to handle navigation.
        /// </summary>
        public event Action? LogoutPerformed;

        public TopBarViewModel(
            CnCNetManager connectionManager,
            PrivateMessageHandler privateMessageHandler,
            IUIThreadMarshaller uiThreadMarshaller,
            OptionsWindowViewModel optionsWindowViewModel)
        {
            this.connectionManager = connectionManager;
            this.privateMessageHandler = privateMessageHandler;
            this.uiThreadMarshaller = uiThreadMarshaller;
            this.optionsWindowViewModel = optionsWindowViewModel;

            IsPlayerCountVisible = ClientConfiguration.Instance.DisplayPlayerCountInTopBar;

            if (IsPlayerCountVisible)
            {
                PlayerCountLabel = ClientConfiguration.Instance.LocalGame.ToUpper() + " " + "PLAYERS ONLINE:".L10N("Client:Main:OnlinePlayersNumber");
                CnCNetPlayerCountTask.CnCNetGameCountUpdated += OnCnCNetGameCountUpdated;
                cncnetPlayerCountCancellationSource = new CancellationTokenSource();
                CnCNetPlayerCountTask.InitializeService(cncnetPlayerCountCancellationSource);
            }

            connectionManager.Connected += OnConnected;
            connectionManager.Disconnected += OnDisconnected;
            connectionManager.ConnectionLost += OnConnectionLost;
            connectionManager.WelcomeMessageReceived += OnWelcomeMessageReceived;
            connectionManager.AttemptedServerChanged += OnAttemptedServerChanged;
            connectionManager.ConnectAttemptFailed += OnConnectAttemptFailed;

            privateMessageHandler.UnreadMessageCountUpdated += OnUnreadMessageCountUpdated;

            optionsWindowViewModel.PropertyChanged += OnOptionsWindowPropertyChanged;

            // Start expanded (like original: DOWN_TIME_WAIT_SECONDS - STARTUP_DOWN_TIME_WAIT_SECONDS)
            IsExpanded = true;
            StartAutoHideTimer(STARTUP_DOWN_TIME_WAIT_SECONDS);
        }

        partial void OnIsLanModeChanged(bool value)
        {
            AreSwitchButtonsClickable = !value;
            if (value)
                ConnectionStatusText = "LAN MODE".L10N("Client:Main:StatusLanMode");
            else
                ConnectionStatusText = "OFFLINE".L10N("Client:Main:StatusOffline");
        }

        #region Commands

        [RelayCommand]
        private void SwitchToPrimary()
        {
            LastSwitchType = SwitchType.PRIMARY;
        }

        [RelayCommand]
        private void SwitchToSecondary()
        {
            LastSwitchType = SwitchType.SECONDARY;
        }

        [RelayCommand]
        private void SwitchToTertiary()
        {
            LastSwitchType = SwitchType.PRIVATE_MESSAGES;
        }

        [RelayCommand]
        private void OpenOptions()
        {
            optionsWindowViewModel.Open();
        }

        [RelayCommand]
        private void Logout()
        {
            connectionManager.Disconnect();
            LogoutPerformed?.Invoke();
            SwitchToPrimary();
        }

        [RelayCommand]
        private void Expand()
        {
            IsExpanded = true;
            StartAutoHideTimer(DOWN_TIME_WAIT_SECONDS);
        }

        #endregion

        public void Clean()
        {
            cncnetPlayerCountCancellationSource?.Cancel();
            _autoHideTimer?.Dispose();
            _autoHideTimer = null;
        }

        private void StartAutoHideTimer(double seconds)
        {
            _autoHideTimer?.Stop();
            _autoHideTimer?.Dispose();
            _autoHideTimer = new System.Timers.Timer(seconds * 1000);
            _autoHideTimer.Elapsed += (s, e) =>
            {
                _autoHideTimer?.Stop();
                uiThreadMarshaller.AddCallback(() => IsExpanded = false);
            };
            _autoHideTimer.AutoReset = false;
            _autoHideTimer.Start();
        }

        #region Event Handlers

        private void OnOptionsWindowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptionsWindowViewModel.IsVisible))
            {
                bool optionsVisible = optionsWindowViewModel.IsVisible;

                if (!IsLanMode)
                    AreSwitchButtonsClickable = !optionsVisible;

                IsOptionsButtonClickable = !optionsVisible;
            }
        }

        private void OnConnected(object? sender, EventArgs e)
        {
            IsLogoutButtonClickable = true;
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            IsLogoutButtonClickable = false;
            if (!IsLanMode)
                ConnectionStatusText = "OFFLINE".L10N("Client:Main:StatusOffline");
        }

        private void OnConnectionLost(object? sender, ConnectionLostEventArgs e)
        {
            if (!IsLanMode)
                ConnectionStatusText = "OFFLINE".L10N("Client:Main:StatusOffline");
        }

        private void OnConnectAttemptFailed(object? sender, EventArgs e)
        {
            if (!IsLanMode)
                ConnectionStatusText = "OFFLINE".L10N("Client:Main:StatusOffline");
        }

        private void OnAttemptedServerChanged(object? sender, AttemptedServerEventArgs e)
        {
            ConnectionStatusText = "CONNECTING...".L10N("Client:Main:StatusConnecting");
            IsExpanded = true;
            StartAutoHideTimer(EVENT_DOWN_TIME_WAIT_SECONDS);
        }

        private void OnWelcomeMessageReceived(object? sender, ServerMessageEventArgs e)
        {
            ConnectionStatusText = "CONNECTED".L10N("Client:Main:StatusConnected");
        }

        private void OnCnCNetGameCountUpdated(object? sender, PlayerCountEventArgs e)
        {
            lock (locker)
            {
                if (e.PlayerCount == -1)
                    PlayerCountText = "N/A".L10N("Client:Main:N/A");
                else
                    PlayerCountText = e.PlayerCount.ToString();
            }
        }

        private void OnUnreadMessageCountUpdated(object? sender, UnreadMessageCountEventArgs e)
        {
            UnreadMessageCount = e.UnreadMessageCount;
        }

        #endregion
    }
}


