// checked
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Online;
using DXMainClientViewModel.Online.EventArguments;
using Rampastring.Tools;
using System;
using System.Threading;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the top bar.
    /// Handles connection status, player count, view switching, and button state.
    /// Self-sufficient: subscribes to connection events directly.
    /// </summary>
    public partial class TopBarViewModel : ObservableObject, ITopBarViewModel
    {
        private readonly CnCNetManager connectionManager;
        private readonly PrivateMessageHandler privateMessageHandler;
        private readonly IUIThreadMarshaller uiThreadMarshaller;

        private CancellationTokenSource? cncnetPlayerCountCancellationSource;
        private static readonly object locker = new object();

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
        private string mainButtonText = "Main Menu (F2)".L10N("Client:Main:MainMenuF2");

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
            IUIThreadMarshaller uiThreadMarshaller)
        {
            this.connectionManager = connectionManager;
            this.privateMessageHandler = privateMessageHandler;
            this.uiThreadMarshaller = uiThreadMarshaller;

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
            // Tertiary is private messages - no switch type change needed
        }

        [RelayCommand]
        private void OpenOptions()
        {
            // Navigation handled by View observing this command
        }

        [RelayCommand]
        private void Logout()
        {
            connectionManager.Disconnect();
            LogoutPerformed?.Invoke();
            SwitchToPrimary();
        }

        #endregion

        public void Clean()
        {
            cncnetPlayerCountCancellationSource?.Cancel();
        }

        #region Event Handlers

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
