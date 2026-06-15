using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using AvClientMvvmContract;
using AvClientMvvmContract.Domain.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Mvvm;
using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.Online;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.CnCNet;
using AvClientViewModel.Multiplayer.GameLobby;
using AvClientViewModel.Online;
using AvClientViewModel.Online.EventArguments;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rampastring.Tools;

using Serilog;


namespace AvClientViewModel.Multiplayer.CnCNet;

/// <summary>
/// ViewModel for the CnCNet lobby.
/// Handles game listing, joining, creation, chat, player list, and connection state.
/// </summary>
public partial class CnCNetLobbyViewModel : ObservableObject, ICnCNetLobbyViewModel
{
    private readonly CnCNetManager connectionManager;
    private readonly CnCNetUserData cncnetUserData;
    private readonly GameCollection gameCollection;
    private readonly TunnelHandler tunnelHandler;
    private readonly MapLoader mapLoader;
    private readonly IUIThreadMarshaller uiThreadMarshaller;
    private readonly IGameProcessService gameProcessService;
    private readonly IClipboardService clipboardService;
    private readonly Random random;

    // Services that the lobby interacts with but does not own
    private readonly CnCNetGameLobbyViewModel gameLobby;
    private readonly CnCNetGameLoadingLobbyViewModel gameLoadingLobby;
    private PrivateMessagingWindowViewModel? pmWindow;

    public ICnCNetGameLobbyViewModel GameLobby => gameLobby;
    public ICnCNetGameLoadingLobbyViewModel GameLoadingLobby => gameLoadingLobby;

    private Channel? currentChatChannel;
    private string localGameID;
    private CnCNetGame? localGame;
    private List<string> followedGames = new();
    private bool isInGameRoom = false;
    private bool isJoiningGame = false;
    private HostedCnCNetGame? gameOfLastJoinAttempt;
    private CancellationTokenSource? gameCheckCancellation;
    private bool updateDenied = false;
    private bool ctcpInvalidGameMessageShown = false;
    private bool ctcpNoTunnelMessageShown = false;
    private bool ctcpNoTunnelForGamesMessageShown = false;

    private IRCColor[] chatColors;

    // Invitation tracking
    private Dictionary<Tuple<string, string>, WeakReference> invitationIndex = new();

    // Invite channel info (for sending invites from lobby context menu)
    private string inviteChannelName = string.Empty;
    private string inviteGameName = string.Empty;
    private string inviteChannelPassword = string.Empty;

    // Pending hosted games for the View to display
    private List<HostedCnCNetGame> hostedGames = new();

    [ObservableProperty]
    public partial string CurrentChannelName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OnlinePlayerCountText { get; set; } = "0";

    [ObservableProperty]
    public partial string PlayerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DraftMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedGameIndex { get; set; } = -1;

    [ObservableProperty]
    public partial int HoveredGameIndex { get; set; } = -1;

    [ObservableProperty]
    public partial int SelectedPlayerIndex { get; set; } = -1;

    [ObservableProperty]
    public partial int SelectedChatMessageIndex { get; set; } = -1;

    [ObservableProperty]
    public partial string? PendingLink { get; set; }

    [ObservableProperty]
    public partial int SelectedColorIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedChannelIndex { get; set; }

    [ObservableProperty]
    public partial string GameSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LogoutButtonText { get; set; } = "Log Out".L10N("Client:Main:LogOut");

    [ObservableProperty]
    public partial bool IsNewGameButtonEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsJoinGameButtonEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsChatInputEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsChannelDropdownEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsGameSearchEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    // Observable state for View to react to
    [ObservableProperty]
    public partial string? PendingMessage { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateCheckNeeded { get; set; }

    [ObservableProperty]
    public partial bool IsLoginWindowVisible { get; set; }

    [ObservableProperty]
    public partial bool IsGameCreationPanelVisible { get; set; }

    [ObservableProperty]
    public partial IPendingGameInviteData? PendingGameInvite { get; set; }

    [ObservableProperty]
    public partial string? SoundToPlay { get; set; }

    private readonly CovariantReadOnlyObservableCollectionAdapter<ContextMenuItem, IContextMenuItem> _playerContextMenuItemsAdapter = new();
    public ObservableCollection<ContextMenuItem> PlayerContextMenuItems => _playerContextMenuItemsAdapter.Source;
    IReadOnlyList<IContextMenuItem> ICnCNetLobbyViewModel.PlayerContextMenuItems => _playerContextMenuItemsAdapter.Target;

    private readonly CovariantReadOnlyObservableCollectionAdapter<ContextMenuItem, IContextMenuItem> _chatContextMenuItemsAdapter = new();
    public ObservableCollection<ContextMenuItem> ChatContextMenuItems => _chatContextMenuItemsAdapter.Source;
    IReadOnlyList<IContextMenuItem> ICnCNetLobbyViewModel.ChatContextMenuItems => _chatContextMenuItemsAdapter.Target;

    private readonly CovariantReadOnlyObservableCollectionAdapter<ContextMenuItem, IContextMenuItem> _gameContextMenuItemsAdapter = new();
    public ObservableCollection<ContextMenuItem> GameContextMenuItems => _gameContextMenuItemsAdapter.Source;
    IReadOnlyList<IContextMenuItem> ICnCNetLobbyViewModel.GameContextMenuItems => _gameContextMenuItemsAdapter.Target;

    // Game list — updated in-place via ObservableCollection.
    private readonly CovariantReadOnlyObservableCollectionAdapter<HostedCnCNetGame, IHostedCnCNetGame> _gamesAdapter = new();
    public ObservableCollection<HostedCnCNetGame> Games => _gamesAdapter.Source;
    IReadOnlyList<IHostedCnCNetGame> ICnCNetLobbyViewModel.Games => _gamesAdapter.Target;

    // Player list — updated in-place via ObservableCollection.
    private readonly CovariantReadOnlyObservableCollectionAdapter<PlayerListItem, IPlayerListItem> _playersAdapter = new();
    public ObservableCollection<PlayerListItem> Players => _playersAdapter.Source;
    IReadOnlyList<IPlayerListItem> ICnCNetLobbyViewModel.Players => _playersAdapter.Target;

    private readonly CovariantReadOnlyObservableCollectionAdapter<ChatMessage, IChatMessage> _chatMessagesAdapter = new();
    public ObservableCollection<ChatMessage> ChatMessages => _chatMessagesAdapter.Source;
    IReadOnlyList<IChatMessage> ICnCNetLobbyViewModel.ChatMessages => _chatMessagesAdapter.Target;

    private readonly CovariantReadOnlyObservableCollectionAdapter<IRCColor, IIRCColor> _colorOptionsAdapter = new();
    public ObservableCollection<IRCColor> ColorOptions => _colorOptionsAdapter.Source;
    IReadOnlyList<IIRCColor> ICnCNetLobbyViewModel.ColorOptions => _colorOptionsAdapter.Target;

    private List<string> channelOptions = new();
    public IReadOnlyList<string> ChannelOptions => channelOptions;

    // Parallel list mapping channelOptions index to the actual channel reference.
    // Used by SwitchToChannel to avoid index mismatch with the unfiltered GameList.
    private List<Channel> channelOptionChannels = new();

    // Messages pinned to the top of the chat that persist across channel switches.
    private readonly CovariantReadOnlyObservableCollectionAdapter<ChatMessage, IChatMessage> _pinnedMessagesAdapter = new();
    public ObservableCollection<ChatMessage> PinnedMessages => _pinnedMessagesAdapter.Source;

    private GameCreationWindowViewModel? gameCreationWindowViewModel;
    public IGameCreationWindowViewModel? GameCreationWindowViewModel => gameCreationWindowViewModel;

    private readonly CnCNetLoginWindowViewModel _loginWindowViewModel;
    public ICnCNetLoginWindowViewModel LoginWindowViewModel => _loginWindowViewModel;

    // Computed observable for the currently selected game in the list.
    [ObservableProperty]
    public partial IHostedCnCNetGame? SelectedGame { get; set; }

    public CnCNetLobbyViewModel(
        CnCNetManager connectionManager,
        CnCNetUserData cncnetUserData,
        GameCollection gameCollection,
        TunnelHandler tunnelHandler,
        MapLoader mapLoader,
        CnCNetGameLobbyViewModel gameLobby,
        CnCNetGameLoadingLobbyViewModel gameLoadingLobby,
        IUIThreadMarshaller uiThreadMarshaller,
        IGameProcessService gameProcessService,
        IClipboardService clipboardService,
        Random random)
    {
        this.connectionManager = connectionManager;
        this.cncnetUserData = cncnetUserData;
        this.gameCollection = gameCollection;
        this.tunnelHandler = tunnelHandler;
        this.mapLoader = mapLoader;
        this.gameLobby = gameLobby;
        this.gameLoadingLobby = gameLoadingLobby;
        this.uiThreadMarshaller = uiThreadMarshaller;
        this.gameProcessService = gameProcessService;
        this.clipboardService = clipboardService;
        this.random = random;

        _loginWindowViewModel = new CnCNetLoginWindowViewModel(
            UserINISettings.Instance,
            onConnectRequested: () =>
            {
                IsLoginWindowVisible = false;
                LoginWindowViewModel.IsVisible = false;
                connectionManager.Connect();
            },
            onCancelled: () =>
            {
                IsLoginWindowVisible = false;
                LoginWindowViewModel.IsVisible = false;
            });

        localGameID = ClientConfiguration.Instance.LocalGame;
        localGame = gameCollection.GameList.Find(g => g.InternalName.ToUpper() == localGameID.ToUpper());

        chatColors = connectionManager.GetIRCColors();

        // Build color options list
        foreach (IRCColor color in chatColors)
        {
            if (color.Selectable)
            {
                ColorOptions.Add(color);
            }
        }

        // Set initial color from settings
        int savedColor = UserINISettings.Instance.ChatColor;
        SelectedColorIndex = (savedColor >= ColorOptions.Count || savedColor < 0)
            ? ClientConfiguration.Instance.DefaultPersonalChatColorIndex
            : savedColor;

        // Subscribe to connection events
        connectionManager.WelcomeMessageReceived += ConnectionManager_WelcomeMessageReceived;
        connectionManager.Disconnected += ConnectionManager_Disconnected;
        connectionManager.PrivateCTCPReceived += ConnectionManager_PrivateCTCPReceived;
        connectionManager.BannedFromChannel += ConnectionManager_BannedFromChannel;

        cncnetUserData.UserFriendToggled += OnUserDataChanged;
        cncnetUserData.UserIgnoreToggled += OnUserDataChanged;

        gameProcessService.GameProcessStarted += SharedUILogic_GameProcessStarted;
        gameProcessService.GameProcessExited += SharedUILogic_GameProcessExited;

        UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;

        CnCNetPlayerCountTask.CnCNetGameCountUpdated += OnCnCNetGameCountUpdated;
        OnlinePlayerCountText = CnCNetPlayerCountTask.PlayerCount.ToString();

        gameLobby.GameLeft += (s, e) => OnGameLobbyLeft();
        gameLoadingLobby.GameLeft += (s, e) => OnGameLoadingLobbyLeft();

        invitationIndex = new Dictionary<Tuple<string, string>, WeakReference>();
    }

    /// <summary>
    /// Sets the private messaging window reference for invite handling.
    /// </summary>
    public void SetPrivateMessagingWindow(PrivateMessagingWindowViewModel pmWindow)
    {
        this.pmWindow = pmWindow;
        pmWindow.SetJoinUserAction(JoinUserByName);
        gameLobby.SetPrivateMessageAction(name => pmWindow.InitPM(name));
    }

    private void JoinUserByName(string userName)
    {
        var user = connectionManager.UserList.Find(u => u.Name == userName);
        if (user == null)
        {
            connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, "User is not currently available!".L10N("Client:Main:UserNotAvailable")));
            return;
        }
        var game = GetHostedGameForUser(user);
        if (game == null)
        {
            connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, string.Format("{0} is not in a game!".L10N("Client:Main:UserNotInGame"), user.Name)));
            return;
        }
        int displayIndex = Games.IndexOf(game);
        int hostIndex = hostedGames.IndexOf(game);
        SelectedGameIndex = displayIndex;
        JoinGameByIndex(hostIndex, string.Empty);
    }

    public void Initialize()
    {
        PlayerName = ProgramConstants.PLAYERNAME;

        // Add version info as the very first messages, before any login/connection
        string clientVersion = GitVersionInformation.AssemblySemVer;
#if DEVELOPMENT_BUILD
        clientVersion = $"{GitVersionInformation.CommitDate} {GitVersionInformation.BranchName}@{GitVersionInformation.ShortSha}";
#endif
        PinnedMessages.Add(new ChatMessage(Rgb24Color.White,
            string.Format("*** CnCNet Client version {0} ***".L10N("Client:Main:CnCNetClientVersionMessageV2"), clientVersion)));

#if DEVELOPMENT_BUILD
        if (ClientConfiguration.Instance.ShowDevelopmentBuildWarnings)
        {
            PinnedMessages.Add(new ChatMessage(Rgb24Color.Red,
                "This is a development build of the client. Stability and reliability may not be fully guaranteed.".L10N("Client:Main:DevelopmentBuildWarning")));
        }
#endif

        foreach (var msg in PinnedMessages)
            ChatMessages.Add(msg);

        InitializeChannelList();

        gameLobby.Initialize();
        gameLoadingLobby.Initialize();
    }

    public void SwitchOn()
    {
        IsVisible = true;

        if (!connectionManager.IsConnected && !connectionManager.IsAttemptingConnection)
        {
            IsLoginWindowVisible = true;
            LoginWindowViewModel.IsVisible = true;
            _loginWindowViewModel.LoadSettings();
        }

        UpdateLogoutButtonText();
    }

    public void SwitchOff()
    {
        IsVisible = false;
    }

    public void Clean()
    {
        isJoiningGame = false;
    }

    #region Commands

    [RelayCommand]
    private void CreateGame()
    {
        if (isInGameRoom)
            return;

        // Create the GameCreationWindow view model on first use
        if (gameCreationWindowViewModel == null)
        {
            gameCreationWindowViewModel = new GameCreationWindowViewModel(
                tunnelHandler,
                onGameCreated: e =>
                {
                    string channelName = RandomizeChannelName();
                    OnGameCreated(e.GameRoomName, channelName, e.Password, e.MaxPlayers, e.Tunnel, e.SkillLevel);
                },
                onLoadedGameCreated: e =>
                {
                    string channelName = RandomizeChannelName();
                    OnLoadedGameCreated(e.GameRoomName, channelName, e.Password, e.Tunnel);
                },
                onCancelled: () =>
                {
                    IsGameCreationPanelVisible = false;
                    gameCreationWindowViewModel!.IsWindowVisible = false;
                });
            OnPropertyChanged(nameof(GameCreationWindowViewModel));
        }

        gameCreationWindowViewModel.Refresh();
        gameCreationWindowViewModel.IsWindowVisible = true;
        IsGameCreationPanelVisible = true;
    }

    [RelayCommand]
    private void JoinSelectedGame()
    {
        var listedGame = GetSelectedHostedGame();
        if (listedGame == null)
            return;

        var hostedGameIndex = hostedGames.IndexOf(listedGame);
        JoinGameByIndex(hostedGameIndex, string.Empty);
    }

    [RelayCommand]
    private void RefreshGames()
    {
        SortAndRefreshHostedGames();
    }

    [RelayCommand]
    private void Logout()
    {
        if (isInGameRoom)
        {
            // Switch to game lobby view (equivalent to topBar.SwitchToPrimary() in original)
            gameLobby.IsEnabled = true;
            return;
        }

        if (connectionManager.IsConnected && !UserINISettings.Instance.PersistentMode)
        {
            connectionManager.Disconnect();
        }

        // Navigate back to main menu
        IsVisible = false;
    }

    [RelayCommand]
    private void SendChatMessage()
    {
        if (string.IsNullOrEmpty(DraftMessage) || currentChatChannel == null)
            return;

        IRCColor selectedColor = chatColors[SelectedColorIndex];
        currentChatChannel.SendChatMessage(DraftMessage, selectedColor);
        DraftMessage = string.Empty;
    }

    [RelayCommand]
    private void CycleSortDirection()
    {
        var currentState = (SortDirection)UserINISettings.Instance.SortState.Value;
        var nextState = currentState switch
        {
            SortDirection.None => SortDirection.Asc,
            SortDirection.Asc => SortDirection.Desc,
            SortDirection.Desc => SortDirection.None,
            _ => SortDirection.None
        };
        UserINISettings.Instance.SortState.Value = (int)nextState;
        SortAndRefreshHostedGames();
        UserINISettings.Instance.SaveSettings();
    }

    [RelayCommand]
    private void ToggleGameFilters()
    {
        // View handles showing/hiding the filters panel
        // After filters change, refresh the game list
        SortAndRefreshHostedGames();
    }

    [RelayCommand]
    private void OpenSelectedPlayerPrivateMessage()
    {
        if (SelectedPlayerIndex < 0 || SelectedPlayerIndex >= Players.Count)
            return;
        var player = Players[SelectedPlayerIndex];
        pmWindow?.InitPM(player.Name);
    }

    [RelayCommand]
    private void ToggleSelectedPlayerFriend()
    {
        if (SelectedPlayerIndex < 0 || SelectedPlayerIndex >= Players.Count)
            return;
        var player = Players[SelectedPlayerIndex];
        cncnetUserData.ToggleFriend(player.Name);
    }

    [RelayCommand]
    private void ToggleSelectedPlayerIgnore()
    {
        if (SelectedPlayerIndex < 0 || SelectedPlayerIndex >= Players.Count)
            return;
        var player = Players[SelectedPlayerIndex];
        var ident = connectionManager.UserList.Find(u => u.Name == player.Name)?.Ident;
        if (!string.IsNullOrEmpty(ident))
            cncnetUserData.ToggleIgnoreUser(ident);
    }

    [RelayCommand]
    private void InviteSelectedPlayerToGame()
    {
        if (SelectedPlayerIndex < 0 || SelectedPlayerIndex >= Players.Count)
            return;

        if (ProgramConstants.IsInGame || string.IsNullOrEmpty(inviteChannelName))
            return;

        var player = Players[SelectedPlayerIndex];
        string messageBody = ProgramConstants.GAME_INVITE_CTCP_COMMAND + " "
            + inviteChannelName + ";" + inviteGameName;

        if (!string.IsNullOrEmpty(inviteChannelPassword))
            messageBody += ";" + inviteChannelPassword;

        connectionManager.SendCustomMessage(new QueuedMessage(
            "PRIVMSG " + player.Name + " :\u0001" + messageBody + "\u0001",
            QueuedMessageType.CHAT_MESSAGE, 0));
    }

    [RelayCommand]
    private void JoinSelectedPlayerGame()
    {
        if (SelectedPlayerIndex < 0 || SelectedPlayerIndex >= Players.Count)
            return;
        var player = Players[SelectedPlayerIndex];
        var user = connectionManager.UserList.Find(u => u.Name == player.Name);
        if (user == null)
        {
            connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, "User is not currently available!".L10N("Client:Main:UserNotAvailable")));
            return;
        }
        var game = GetHostedGameForUser(user);
        if (game == null)
        {
            connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, string.Format("{0} is not in a game!".L10N("Client:Main:UserNotInGame"), user.Name)));
            return;
        }
        int displayIndex = Games.IndexOf(game);
        int hostIndex = hostedGames.IndexOf(game);
        SelectedGameIndex = displayIndex;
        JoinGameByIndex(hostIndex, string.Empty);
    }

    // --- Chat message context menu ---

    [RelayCommand]
    private void OpenSelectedChatMessageSenderPrivateMessage()
    {
        if (SelectedChatMessageIndex < 0 || SelectedChatMessageIndex >= ChatMessages.Count)
            return;
        var msg = ChatMessages[SelectedChatMessageIndex];
        if (!string.IsNullOrEmpty(msg.SenderName))
            pmWindow?.InitPM(msg.SenderName);
    }

    [RelayCommand]
    private void ToggleSelectedChatMessageSenderFriend()
    {
        if (SelectedChatMessageIndex < 0 || SelectedChatMessageIndex >= ChatMessages.Count)
            return;
        var msg = ChatMessages[SelectedChatMessageIndex];
        if (!string.IsNullOrEmpty(msg.SenderName))
            cncnetUserData.ToggleFriend(msg.SenderName);
    }

    [RelayCommand]
    private void ToggleSelectedChatMessageSenderIgnore()
    {
        if (SelectedChatMessageIndex < 0 || SelectedChatMessageIndex >= ChatMessages.Count)
            return;
        var msg = ChatMessages[SelectedChatMessageIndex];
        if (string.IsNullOrEmpty(msg.SenderIdent))
            return;
        cncnetUserData.ToggleIgnoreUser(msg.SenderIdent);
    }

    [RelayCommand]
    private void JoinSelectedChatMessageSenderGame()
    {
        if (SelectedChatMessageIndex < 0 || SelectedChatMessageIndex >= ChatMessages.Count)
            return;
        var msg = ChatMessages[SelectedChatMessageIndex];
        if (string.IsNullOrEmpty(msg.SenderName))
            return;
        var user = connectionManager.UserList.Find(u => u.Name == msg.SenderName);
        if (user == null)
        {
            connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, "User is not currently available!".L10N("Client:Main:UserNotAvailable")));
            return;
        }
        var game = GetHostedGameForUser(user);
        if (game == null)
        {
            connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, string.Format("{0} is not in a game!".L10N("Client:Main:UserNotInGame"), user.Name)));
            return;
        }
        int displayIndex = Games.IndexOf(game);
        int hostIndex = hostedGames.IndexOf(game);
        SelectedGameIndex = displayIndex;
        JoinGameByIndex(hostIndex, string.Empty);
    }

    [RelayCommand]
    private void OpenPendingLink()
    {
        if (string.IsNullOrEmpty(PendingLink))
            return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(PendingLink) { UseShellExecute = true }); }
        catch { }
    }

    [RelayCommand]
    private void CopyPendingLink()
    {
        if (string.IsNullOrEmpty(PendingLink))
            return;
        clipboardService.SetTextAsync(PendingLink);
    }

    [RelayCommand]
    private void ChatMessageDoubleClick()
    {
        if (SelectedChatMessageIndex < 0 || SelectedChatMessageIndex >= ChatMessages.Count)
            return;
        var msg = ChatMessages[SelectedChatMessageIndex];
        var links = msg.Message.GetLinks();
        if (links == null || links.Length != 1)
            return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(links[0]) { UseShellExecute = true }); }
        catch { }
    }

    // --- Game list context menu ---

    [RelayCommand]
    private void OpenSelectedGameHostPrivateMessage()
    {
        var game = GetSelectedHostedGame();
        if (game == null) return;
        pmWindow?.InitPM(game.HostName);
    }

    [RelayCommand]
    private void ToggleSelectedGameHostFriend()
    {
        var game = GetSelectedHostedGame();
        if (game == null) return;
        cncnetUserData.ToggleFriend(game.HostName);
    }

    [RelayCommand]
    private void ToggleSelectedGameHostIgnore()
    {
        var game = GetSelectedHostedGame();
        if (game == null) return;
        var ident = connectionManager.UserList.Find(u => u.Name == game.HostName)?.Ident;
        if (!string.IsNullOrEmpty(ident))
            cncnetUserData.ToggleIgnoreUser(ident);
    }

    #endregion

    partial void OnGameSearchTextChanged(string value)
    {
        SortAndRefreshHostedGames();
    }

    partial void OnSelectedColorIndexChanged(int value)
    {
        UserINISettings.Instance.ChatColor.Value = value;
        UserINISettings.Instance.SaveSettings();

        if (value >= 0 && value < chatColors.Length)
        {
            IRCColor selectedColor = chatColors[value];
            gameLobby.ChatColor = selectedColor;
            gameLoadingLobby.ChangeChatColor(selectedColor);
        }
    }

    partial void OnHoveredGameIndexChanged(int value)
    {
        if (value >= hostedGames.Count)
            HoveredGameIndex = -1;
    }

    partial void OnSelectedChannelIndexChanged(int value)
    {
        SwitchToChannel(value);
    }

    /// <summary>
    /// Called when a game creation completes.
    /// </summary>
    public void OnGameCreated(string gameRoomName, string channelName, string password, int maxPlayers, ICnCNetTunnel tunnel, int skillLevel)
    {
        if (gameLobby.IsEnabled || gameLoadingLobby.IsEnabled)
            return;

        bool isCustomPassword = true;
        if (string.IsNullOrEmpty(password))
        {
            password = Utilities.CalculateSHA1ForString(channelName).Substring(0, 10);
            isCustomPassword = false;
        }

        Channel gameChannel = connectionManager.CreateChannel(gameRoomName, channelName, false, true, password);
        connectionManager.AddChannel(gameChannel);
        gameLobby.SetUp(gameChannel, true, maxPlayers, tunnel, ProgramConstants.PLAYERNAME, isCustomPassword, skillLevel);
        gameLobby.IsEnabled = true;
        gameChannel.UserAdded += GameChannel_UserAdded;
        connectionManager.SendCustomMessage(new QueuedMessage("JOIN " + channelName + " " + password,
            QueuedMessageType.INSTANT_MESSAGE, 0));
        connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White,
            string.Format("Creating a game named {0} ...".L10N("Client:Main:CreateGameNamed"), gameRoomName)));

        IsGameCreationPanelVisible = false;
        if (gameCreationWindowViewModel != null)
            gameCreationWindowViewModel.IsWindowVisible = false;

        pmWindow?.SetInviteChannelInfo(channelName, gameRoomName, string.IsNullOrEmpty(password) ? string.Empty : password);
        inviteChannelName = channelName;
        inviteGameName = gameRoomName;
        inviteChannelPassword = string.IsNullOrEmpty(password) ? string.Empty : password;
    }

    /// <summary>
    /// Called when a loaded game creation completes.
    /// </summary>
    public void OnLoadedGameCreated(string gameRoomName, string channelName, string password, ICnCNetTunnel tunnel)
    {
        if (gameLobby.IsEnabled || gameLoadingLobby.IsEnabled)
            return;

        Channel gameLoadingChannel = connectionManager.CreateChannel(gameRoomName, channelName, false, true, password);
        connectionManager.AddChannel(gameLoadingChannel);
        gameLoadingLobby.SetUp(true, tunnel, gameLoadingChannel, ProgramConstants.PLAYERNAME);
        gameLoadingLobby.IsEnabled = true;
        gameLoadingChannel.UserAdded += GameLoadingChannel_UserAdded;
        connectionManager.SendCustomMessage(new QueuedMessage("JOIN " + channelName + " " + password,
            QueuedMessageType.INSTANT_MESSAGE, 0));
        connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White,
            string.Format("Creating a game named {0} ...".L10N("Client:Main:CreateGameNamed"), gameRoomName)));

        IsGameCreationPanelVisible = false;
        if (gameCreationWindowViewModel != null)
            gameCreationWindowViewModel.IsWindowVisible = false;

        pmWindow?.SetInviteChannelInfo(channelName, gameRoomName, string.IsNullOrEmpty(password) ? string.Empty : password);
        inviteChannelName = channelName;
        inviteGameName = gameRoomName;
        inviteChannelPassword = string.IsNullOrEmpty(password) ? string.Empty : password;
    }

    /// <summary>
    /// Called when the user confirms a password for joining a game.
    /// </summary>
    public void OnPasswordEntered(IHostedCnCNetGame game, string password)
    {
        JoinGame((HostedCnCNetGame)game, password, connectionManager.MainChannel);
    }

    [RelayCommand]
    private void AcceptGameInvite()
    {
        if (PendingGameInvite == null)
            return;

        if (isInGameRoom)
        {
            gameLobby.LeaveGameLobby();
            gameLoadingLobby.Clear();
        }

        var gameIndex = hostedGames.FindIndex(g => g.ChannelName == PendingGameInvite.ChannelName);
        JoinGameByIndex(gameIndex, PendingGameInvite.Password);

        var invitationIdentity = new Tuple<string, string>(PendingGameInvite.Sender, PendingGameInvite.ChannelName);
        invitationIndex.Remove(invitationIdentity);
        PendingGameInvite = null;
    }

    [RelayCommand]
    private void DismissGameInvite()
    {
        if (PendingGameInvite == null)
            return;

        var invitationIdentity = new Tuple<string, string>(PendingGameInvite.Sender, PendingGameInvite.ChannelName);
        invitationIndex.Remove(invitationIdentity);
        PendingGameInvite = null;
    }

    [RelayCommand]
    private void AcceptUpdate()
    {
        IsUpdateCheckNeeded = false;
        // The parent (MainMenu) subscribes to IsUpdateCheckNeeded and triggers the update flow
    }

    [RelayCommand]
    private void DenyUpdate()
    {
        IsUpdateCheckNeeded = false;
        updateDenied = true;
    }

    [RelayCommand]
    private void DismissMessage()
    {
        PendingMessage = null;
    }

    /// <summary>
    /// Called when a game lobby is left.
    /// </summary>
    public void OnGameLobbyLeft()
    {
        isInGameRoom = false;
        UpdateLogoutButtonText();
        pmWindow.ClearInviteChannelInfo();
        inviteChannelName = string.Empty;
        inviteGameName = string.Empty;
        inviteChannelPassword = string.Empty;
    }

    /// <summary>
    /// Called when a game loading lobby is left.
    /// </summary>
    public void OnGameLoadingLobbyLeft()
    {
        isInGameRoom = false;
        UpdateLogoutButtonText();
        pmWindow?.ClearInviteChannelInfo();
        inviteChannelName = string.Empty;
        inviteGameName = string.Empty;
        inviteChannelPassword = string.Empty;
    }

    /// <summary>
    /// Called when the game filters panel closes.
    /// </summary>
    public void OnGameFiltersPanelClosed()
    {
        SortAndRefreshHostedGames();
    }

    /// <summary>
    /// Gets the list of hosted games for display.
    /// </summary>
    public IReadOnlyList<HostedCnCNetGame> GetHostedGames() => hostedGames;

    /// <summary>
    /// Gets the IRC color for the selected index.
    /// </summary>
    public IRCColor GetSelectedIRCColor() => chatColors[SelectedColorIndex];

    /// <summary>
    /// Gets the hosted game at the selected index.
    /// </summary>
    public HostedCnCNetGame? GetSelectedHostedGame()
    {
        if (SelectedGameIndex < 0 || SelectedGameIndex >= Games.Count)
            return null;
        return Games[SelectedGameIndex];
    }

    /// <summary>
    /// Gets the sort direction from settings.
    /// </summary>
    public SortDirection GetSortDirection()
    {
        if (Enum.IsDefined(typeof(SortDirection), UserINISettings.Instance.SortState.Value))
            return (SortDirection)UserINISettings.Instance.SortState.Value;
        return SortDirection.None;
    }

    /// <summary>
    /// Whether game filters are currently applied.
    /// </summary>
    public bool AreGameFiltersApplied => UserINISettings.Instance.IsGameFiltersApplied();

    /// <summary>
    /// Checks if a hosted game matches the current filter criteria.
    /// </summary>
    public bool HostedGameMatches(GenericHostedGame hg)
    {
        if (UserINISettings.Instance.ShowFriendGamesOnly)
            return hg.Players.Any(cncnetUserData.IsFriend);

        if (UserINISettings.Instance.HideLockedGames.Value && hg.Locked)
            return false;

        if (UserINISettings.Instance.HideIncompatibleGames.Value && hg.Incompatible)
            return false;

        if (UserINISettings.Instance.HidePasswordedGames.Value && hg.Passworded)
            return false;

        if (hg.MaxPlayers > UserINISettings.Instance.MaxPlayerCount.Value)
            return false;

        if (hg is HostedCnCNetGame cncnetGame && !GameOptionsMatch(cncnetGame))
            return false;

        string textUpper = GameSearchText?.ToUpperInvariant();

        string translatedGameMode = string.IsNullOrEmpty(hg.GameMode)
            ? "Unknown".L10N("Client:Main:Unknown")
            : hg.GameMode.L10N($"INI:GameModes:{hg.GameMode}:UIName", notify: false);

        string translatedMapName = string.IsNullOrEmpty(hg.Map)
            ? "Unknown".L10N("Client:Main:Unknown")
            : null; // MapLoader.TranslatedMapNames is a View concern

        return
            string.IsNullOrWhiteSpace(GameSearchText) ||
            hg.RoomName.ToUpperInvariant().Contains(textUpper) ||
            hg.GameMode.ToUpperInvariant().Equals(textUpper, StringComparison.Ordinal) ||
            translatedGameMode.ToUpperInvariant().Equals(textUpper, StringComparison.Ordinal) ||
            hg.Map.ToUpperInvariant().Contains(textUpper) ||
            (translatedMapName is not null && translatedMapName.ToUpperInvariant().Contains(textUpper)) ||
            hg.Players.Any(pl => pl.ToUpperInvariant().Equals(textUpper, StringComparison.Ordinal));
    }

    /// <summary>
    /// Checks if a game's broadcast options match the current filter criteria.
    /// </summary>
    public bool GameOptionsMatch(HostedCnCNetGame game)
    {
        if (game.BroadcastedGameOptionValues == null)
            return true;

        var broadcastableSettings = gameLobby.GetBroadcastableSettings();
        if (broadcastableSettings == null)
            return true;

        for (int i = 0; i < broadcastableSettings.Count; i++)
        {
            if (i >= game.BroadcastedGameOptionValues.Length)
                break;

            int? filterValue = UserINISettings.Instance.GetGameOptionFilterValue(broadcastableSettings[i].Name);
            if (filterValue == null)
                continue;

            if (game.BroadcastedGameOptionValues[i] != filterValue.Value)
                return false;
        }

        return true;
    }

    #region Private Methods

    private void InitializeChannelList()
    {
        int i = 0;
        foreach (var game in gameCollection.GameList)
        {
            if (!game.Supported || string.IsNullOrEmpty(game.ChatChannel))
                continue;

            channelOptions.Add(game.UIName);

            var chatChannel = connectionManager.FindChannel(game.ChatChannel);
            if (chatChannel == null)
            {
                chatChannel = connectionManager.CreateChannel(game.UIName, game.ChatChannel, true, true, "ra1-derp");
                connectionManager.AddChannel(chatChannel);
            }

            channelOptionChannels.Add(chatChannel);

            if (!string.IsNullOrEmpty(game.GameBroadcastChannel))
            {
                var gameBroadcastChannel = connectionManager.FindChannel(game.GameBroadcastChannel);
                if (gameBroadcastChannel == null)
                {
                    gameBroadcastChannel = connectionManager.CreateChannel(
                        string.Format("{0} Broadcast Channel".L10N("Client:Main:BroadcastChannel"), game.UIName),
                        game.GameBroadcastChannel, true, false, null);
                    connectionManager.AddChannel(gameBroadcastChannel);
                }

                gameBroadcastChannel.CTCPReceived += GameBroadcastChannel_CTCPReceived;
                gameBroadcastChannel.UserLeft += GameBroadcastChannel_UserLeftOrQuit;
                gameBroadcastChannel.UserQuitIRC += GameBroadcastChannel_UserLeftOrQuit;
                gameBroadcastChannel.UserKicked += GameBroadcastChannel_UserLeftOrQuit;
            }

            if (game.InternalName.ToUpper() == localGameID.ToUpper())
            {
                SelectedChannelIndex = i;
            }

            i++;
        }

        if (connectionManager.MainChannel == null && channelOptions.Count > 0)
        {
            SelectedChannelIndex = channelOptions.Count - 1;
        }
    }

    private void SwitchToChannel(int channelIndex)
    {
        if (channelIndex < 0 || channelIndex >= channelOptions.Count)
            return;

        // Unsubscribe from old channel events
        if (currentChatChannel != null)
        {
            currentChatChannel.UserAdded -= RefreshPlayerList;
            currentChatChannel.UserLeft -= RefreshPlayerList;
            currentChatChannel.UserQuitIRC -= RefreshPlayerList;
            currentChatChannel.UserKicked -= RefreshPlayerList;
            currentChatChannel.UserListReceived -= RefreshPlayerList;
            currentChatChannel.MessageAdded -= CurrentChatChannel_MessageAdded;
            currentChatChannel.UserGameIndexUpdated -= CurrentChatChannel_UserGameIndexUpdated;

            if (currentChatChannel.ChannelName != "#cncnet" &&
                currentChatChannel.ChannelName != gameCollection.GetGameChatChannelNameFromIdentifier(localGameID))
            {
                currentChatChannel.Users.DoForAllUsers(user =>
                {
                    connectionManager.RemoveChannelFromUser(user.IRCUser.Name, currentChatChannel.ChannelName);
                });
                currentChatChannel.Leave();
            }
        }

        // Find the channel for the selected game using the parallel channel list
        // (avoids index mismatch between filtered channelOptions and unfiltered GameList)
        currentChatChannel = channelOptionChannels[channelIndex];

        if (currentChatChannel == null)
            return;

        CurrentChannelName = currentChatChannel.UIName;

        // Subscribe to new channel events
        currentChatChannel.UserAdded += RefreshPlayerList;
        currentChatChannel.UserLeft += RefreshPlayerList;
        currentChatChannel.UserQuitIRC += RefreshPlayerList;
        currentChatChannel.UserKicked += RefreshPlayerList;
        currentChatChannel.UserListReceived += RefreshPlayerList;
        currentChatChannel.MessageAdded += CurrentChatChannel_MessageAdded;
        currentChatChannel.UserGameIndexUpdated += CurrentChatChannel_UserGameIndexUpdated;
        connectionManager.SetMainChannel(currentChatChannel);

        // Clear and reload chat messages
        ChatMessages.Clear();
        ctcpInvalidGameMessageShown = false;
        ctcpNoTunnelMessageShown = false;
        ctcpNoTunnelForGamesMessageShown = false;

        // Re-add pinned messages that must persist across channel switches
        foreach (var msg in PinnedMessages)
            ChatMessages.Add(msg);

        if (currentChatChannel.Messages != null)
        {
            foreach (var msg in currentChatChannel.Messages)
                AddMessageToChat(msg);
        }

        RefreshPlayerList(this, EventArgs.Empty);

        if (currentChatChannel.ChannelName != "#cncnet" &&
            currentChatChannel.ChannelName != gameCollection.GetGameChatChannelNameFromIdentifier(localGameID))
        {
            currentChatChannel.Join();
        }
    }

    private void RefreshPlayerList(object sender, EventArgs e)
    {
        if (currentChatChannel == null)
            return;

        var list = new List<PlayerListItem>();
        var current = currentChatChannel.Users.GetFirst();
        while (current != null)
        {
            var user = current.Value;
            user.IRCUser.IsFriend = cncnetUserData.IsFriend(user.IRCUser.Name);
            user.IRCUser.IsIgnored = cncnetUserData.IsIgnored(user.IRCUser.Ident);
            list.Add(new PlayerListItem(
                user.IRCUser.Name,
                user.IsAdmin,
                user.IRCUser.IsFriend,
                user.IRCUser.IsIgnored,
                user.HasVoice,
                user.IRCUser.GameID));
            current = current.Next;
        }
        // Apply only changed positions to ObservableCollection
        int common = Math.Min(Players.Count, list.Count);
        for (int i = 0; i < common; i++)
        {
            if (Players[i].Name != list[i].Name)
                Players[i] = list[i];
        }
        while (Players.Count > list.Count)
            Players.RemoveAt(Players.Count - 1);
        for (int i = Players.Count; i < list.Count; i++)
            Players.Add(list[i]);
    }

    private void UI_RefreshPlayerList()
    {
        RefreshPlayerList(this, EventArgs.Empty);
    }

    private void OnUserDataChanged(object sender, EventArgs e)
    {
        RefreshPlayerList(sender, e);
    }

    private void AddMessageToChat(ChatMessage message)
    {
        UI_AddMessageToChat(message);
    }

    private void UI_AddMessageToChat(ChatMessage message)
    {
        if (!string.IsNullOrEmpty(message.SenderIdent) &&
            cncnetUserData.IsIgnored(message.SenderIdent) &&
            !message.SenderIsAdmin)
        {
            ChatMessages.Add(new ChatMessage(
                string.Format("Message blocked from - {0}".L10N("Client:Main:PMBlockedFrom"), message.SenderName)));
        }
        else
        {
            ChatMessages.Add(message);
        }
    }

    private void CurrentChatChannel_MessageAdded(object sender, IRCMessageEventArgs e)
    {
        uiThreadMarshaller.AddCallback(new Action(() =>
        {
            UI_AddMessageToChat(e.Message);
        }));
    }

    private void CurrentChatChannel_UserGameIndexUpdated(object sender, ChannelUserEventArgs e)
    {
        uiThreadMarshaller.AddCallback(new Action(() =>
        {
            UI_RefreshPlayerList();
        }));
    }

    private void SortAndRefreshHostedGames()
    {
        // Filter and sort the hosted games
        var filtered = hostedGames.Where(g => HostedGameMatches(g)).ToList();

        var sortDir = GetSortDirection();
        if (sortDir == SortDirection.Asc)
            filtered = filtered.OrderBy(g => g.RoomName).ToList();
        else if (sortDir == SortDirection.Desc)
            filtered = filtered.OrderByDescending(g => g.RoomName).ToList();

        // Build lookup and set of channel names still present
        var filteredByChannel = new Dictionary<string, HostedCnCNetGame>();
        foreach (var g in filtered)
            filteredByChannel[g.ChannelName] = g;
        var presentChannels = new HashSet<string>(filteredByChannel.Keys);

        var oldList = Games;

        // Pass 1: build list preserving positions of still-existing games.
        // Use a nullable list internally — gaps (null) mark deleted games.
        List<HostedCnCNetGame?> build = new(oldList.Count);
        var gaps = new List<int>(); // indices in 'build' that need filling

        for (int i = 0; i < oldList.Count; i++)
        {
            var ch = oldList[i].ChannelName;
            if (presentChannels.Contains(ch))
            {
                // Game still exists — keep at its original position
                build.Add(filteredByChannel[ch]);
                filteredByChannel.Remove(ch); // mark consumed
            }
            else
            {
                // Game was deleted — leave a gap
                gaps.Add(build.Count);
                build.Add(null);
            }
        }

        // Remaining new games (sorted, not previously displayed)
        var newGames = filteredByChannel.Values.ToList();
        int ngIdx = 0;

        // Pass 2: fill each gap with a new game, or shift an item from the end
        foreach (int gapPos in gaps)
        {
            if (ngIdx < newGames.Count)
            {
                build[gapPos] = newGames[ngIdx++];
            }
            else
            {
                // No new games left — shift the last non-null item into this gap
                int last = build.Count - 1;
                while (last > gapPos && build[last] == null)
                    last--;
                if (last <= gapPos)
                    break; // nothing left to shift

                // Don't move the selected or hovered game
                while ((last == SelectedGameIndex || last == HoveredGameIndex) && last > gapPos + 1)
                    last--;

                build[gapPos] = build[last];
                build.RemoveAt(last);
            }
        }

        // Trim nulls and append leftover new games
        var result = new List<HostedCnCNetGame>(build.Count);
        foreach (var g in build)
        { if (g != null) result.Add(g); }
        while (ngIdx < newGames.Count)
            result.Add(newGames[ngIdx++]);

        // Apply only changed positions to ObservableCollection.
        // Unchanged items are left alone — zero CollectionChanged events.
        int common = Math.Min(Games.Count, result.Count);
        for (int i = 0; i < common; i++)
        {
            if (Games[i].ChannelName != result[i].ChannelName)
                Games[i] = result[i];
        }
        while (Games.Count > result.Count)
            Games.RemoveAt(Games.Count - 1);
        for (int i = Games.Count; i < result.Count; i++)
            Games.Add(result[i]);

        if (SelectedGameIndex >= 0 && SelectedGameIndex < Games.Count)
            SelectedGame = Games[SelectedGameIndex];
        else
            SelectedGame = null;
    }

    private void UpdateOnlineCount(int playerCount)
    {
        OnlinePlayerCountText = playerCount.ToString();
    }

    private void UpdateLogoutButtonText()
    {
        if (isInGameRoom)
        {
            LogoutButtonText = "Game Lobby".L10N("Client:Main:GameLobby");
            return;
        }

        if (UserINISettings.Instance.PersistentMode)
        {
            LogoutButtonText = "Main Menu".L10N("Client:Main:MainMenu");
            return;
        }

        LogoutButtonText = "Log Out".L10N("Client:Main:LogOut");
    }

    #endregion

    #region Game Joining Logic

    private string? GetJoinGameErrorBase()
    {
        if (isJoiningGame)
            return "Cannot join game - joining game in progress. If you believe this is an error, please log out and back in.".L10N("Client:Main:JoinGameErrorInProgress");

        if (ProgramConstants.IsInGame)
            return "Cannot join game while the main game executable is running.".L10N("Client:Main:JoinGameErrorGameRunning");

        return null;
    }

    private string? GetJoinGameErrorByIndex(int gameIndex)
    {
        if (gameIndex < 0 || gameIndex >= hostedGames.Count)
            return "Invalid game index".L10N("Client:Main:InvalidGameIndex");

        return GetJoinGameErrorBase();
    }

    private string? GetJoinGameError(HostedCnCNetGame hg)
    {
        if (hg.Game.InternalName.ToUpper() != localGameID.ToUpper())
            return string.Format("The selected game is for {0}!".L10N("Client:Main:GameIsOfPurpose"), gameCollection.GetGameNameFromInternalName(hg.Game.InternalName));

        if (hg.Incompatible && ClientConfiguration.Instance.DisallowJoiningIncompatibleGames)
            return "Cannot join game. The host is on a different game version than you.".L10N("Client:Main:DisallowJoiningIncompatibleGames");

        if (hg.Locked)
            return string.Format("The game {0} is locked!".L10N("Client:Main:GameLockedWithName"), hg.RoomName);

        if (hg.IsLoadedGame && !hg.Players.Contains(ProgramConstants.PLAYERNAME))
            return "You do not exist in the saved game!".L10N("Client:Main:NotInSavedGame");

        return GetJoinGameErrorBase();
    }

    private bool JoinGameByIndex(int gameIndex, string password)
    {
        string? error = GetJoinGameErrorByIndex(gameIndex);
        if (!string.IsNullOrEmpty(error))
        {
            connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, error));
            return false;
        }

        return JoinGame(hostedGames[gameIndex], password, connectionManager.MainChannel);
    }

    private bool JoinGame(HostedCnCNetGame hg, string password, IMessageView? messageView)
    {
        string? error = GetJoinGameError(hg);
        if (!string.IsNullOrEmpty(error))
        {
            messageView?.AddMessage(new ChatMessage(Rgb24Color.White, error));
            return false;
        }

        if (isInGameRoom)
            return false;

        if (hg.GameVersion != ProgramConstants.GAME_VERSION)
            messageView?.AddMessage(new ChatMessage(Rgb24Color.Yellow, "The game host is on a different game version than you. Version incompatibilities may cause issues.".L10N("Client:Main:JoinGameVersionMismatch")));

        if (hg.Passworded)
        {
            if (string.IsNullOrEmpty(password))
            {
                // Need to request password from user
                // This is handled via the password request window
                return true;
            }
        }
        else
        {
            if (!hg.IsLoadedGame)
            {
                password = Utilities.CalculateSHA1ForString(hg.ChannelName).Substring(0, 10);
            }
            else
            {
                IniFile spawnSGIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "Saved Games", "spawnSG.ini"));
                password = Utilities.CalculateSHA1ForString(
                    spawnSGIni.GetStringValue("Settings", "GameID", string.Empty)).Substring(0, 10);
            }
        }

        JoinGameInternal(hg, password);
        return true;
    }

    private void JoinGameInternal(HostedCnCNetGame hg, string password)
    {
        connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White,
            string.Format("Attempting to join game {0} ...".L10N("Client:Main:AttemptJoin"), hg.RoomName)));
        isJoiningGame = true;
        gameOfLastJoinAttempt = hg;

        Channel gameChannel = connectionManager.CreateChannel(hg.RoomName, hg.ChannelName, false, true, password);
        connectionManager.AddChannel(gameChannel);

        if (hg.IsLoadedGame)
        {
            gameLoadingLobby.SetUp(false, hg.TunnelServer, gameChannel, hg.HostName);
            gameChannel.UserAdded += GameLoadingChannel_UserAdded;
            gameChannel.InvalidPasswordEntered += GameChannel_InvalidPasswordEntered_LoadedGame;
            isJoiningGame = false;
        }
        else
        {
            gameLobby.SetUp(gameChannel, false, hg.MaxPlayers, hg.TunnelServer, hg.HostName, hg.Passworded, hg.SkillLevel);
            gameChannel.UserAdded += GameChannel_UserAdded;
            gameChannel.InvalidPasswordEntered += GameChannel_InvalidPasswordEntered_NewGame;
            gameChannel.InviteOnlyErrorOnJoin += GameChannel_InviteOnlyErrorOnJoin;
            gameChannel.ChannelFull += GameChannel_ChannelFull;
            gameChannel.TargetChangeTooFast += GameChannel_TargetChangeTooFast;
        }

        connectionManager.SendCustomMessage(new QueuedMessage("JOIN " + hg.ChannelName + " " + password,
            QueuedMessageType.INSTANT_MESSAGE, 0));
    }

    private void GameChannel_TargetChangeTooFast(object sender, MessageEventArgs e)
    {
        connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, e.Message));
        ClearGameJoinAttempt((Channel)sender);
    }

    private void GameChannel_ChannelFull(object sender, EventArgs e) =>
        GameChannel_InviteOnlyErrorOnJoin(sender, e);

    private void GameChannel_InviteOnlyErrorOnJoin(object sender, EventArgs e)
    {
        var channel = (Channel)sender;
        var game = hostedGames.Find(g => g.ChannelName == channel.ChannelName);

        if (game != null)
        {
            connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, string.Format("The game {0} is locked!".L10N("Client:Main:GameLockedWithName"), game.RoomName)));
            game.Locked = true;
            SortAndRefreshHostedGames();
        }
        else
        {
            connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, "The selected game is locked!".L10N("Client:Main:GameLocked")));
        }

        ClearGameJoinAttempt((Channel)sender);
    }

    private void GameChannel_InvalidPasswordEntered_NewGame(object sender, EventArgs e)
    {
        connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, "Incorrect password!".L10N("Client:Main:PasswordWrong")));
        ClearGameJoinAttempt((Channel)sender);
    }

    private void GameChannel_UserAdded(object sender, ChannelUserEventArgs e)
    {
        Channel gameChannel = (Channel)sender;
        if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
        {
            ClearGameChannelEvents(gameChannel);
            gameLobby.OnJoined();
            isInGameRoom = true;
            UpdateLogoutButtonText();
        }
    }

    private void ClearGameJoinAttempt(Channel channel)
    {
        ClearGameChannelEvents(channel);
        gameLobby.Clear();
    }

    private void ClearGameChannelEvents(Channel channel)
    {
        channel.UserAdded -= GameChannel_UserAdded;
        channel.InvalidPasswordEntered -= GameChannel_InvalidPasswordEntered_NewGame;
        channel.InviteOnlyErrorOnJoin -= GameChannel_InviteOnlyErrorOnJoin;
        channel.ChannelFull -= GameChannel_ChannelFull;
        channel.TargetChangeTooFast -= GameChannel_TargetChangeTooFast;
        isJoiningGame = false;
    }

    private void GameChannel_InvalidPasswordEntered_LoadedGame(object sender, EventArgs e)
    {
        var channel = (Channel)sender;
        channel.UserAdded -= GameLoadingChannel_UserAdded;
        channel.InvalidPasswordEntered -= GameChannel_InvalidPasswordEntered_LoadedGame;
        gameLoadingLobby.Clear();
        isJoiningGame = false;
    }

    private void GameLoadingChannel_UserAdded(object sender, ChannelUserEventArgs e)
    {
        Channel gameLoadingChannel = (Channel)sender;
        if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
        {
            gameLoadingChannel.UserAdded -= GameLoadingChannel_UserAdded;
            gameLoadingChannel.InvalidPasswordEntered -= GameChannel_InvalidPasswordEntered_LoadedGame;
            gameLoadingLobby.OnJoined();
            isInGameRoom = true;
            isJoiningGame = false;
        }
    }

    private string RandomizeChannelName()
    {
        int maxTries = 10000;
        for (int i = 0; i < maxTries; i++)
        {
            string channelName = gameCollection.GetGameChatChannelNameFromIdentifier(localGameID) + "-game" + random.Next(1000000, 9999999);
            int index = hostedGames.FindIndex(c => c.ChannelName == channelName);
            if (index == -1)
                return channelName;
        }
        throw new Exception(string.Format("Could not find a random channel name after {0} retries", maxTries));
    }

    #endregion

    #region Event Handlers

    private void OnCnCNetGameCountUpdated(object sender, PlayerCountEventArgs e) =>
        UpdateOnlineCount(e.PlayerCount);

    private void ConnectionManager_WelcomeMessageReceived(object sender, EventArgs e)
    {
        uiThreadMarshaller.AddCallback(new Action(() =>
        {
            IsNewGameButtonEnabled = true;
            IsJoinGameButtonEnabled = SelectedGameIndex >= 0;
            IsChatInputEnabled = true;
            IsChannelDropdownEnabled = true;
            IsGameSearchEnabled = true;
            IsConnected = true;

            Channel cncnetChannel = connectionManager.FindChannel("#cncnet");
            cncnetChannel?.Join();

            string localGameChatChannelName = gameCollection.GetGameChatChannelNameFromIdentifier(localGameID);
            connectionManager.FindChannel(localGameChatChannelName)?.Join();

            string localGameBroadcastChannel = gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGameID);
            connectionManager.FindChannel(localGameBroadcastChannel)?.Join();

            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported)
                    continue;

                if (game.InternalName.ToUpper() != localGameID)
                {
                    if (UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
                    {
                        connectionManager.FindChannel(game.GameBroadcastChannel)?.Join();
                        followedGames.Add(game.InternalName);
                    }
                }
            }

            gameCheckCancellation = new CancellationTokenSource();
            CnCNetGameCheck.Instance.InitializeService(gameCheckCancellation);

        }));
    }

    private void ConnectionManager_Disconnected(object sender, EventArgs e)
    {
        uiThreadMarshaller.AddCallback(new Action(() =>
        {
            IsNewGameButtonEnabled = false;
            IsJoinGameButtonEnabled = false;
            IsChatInputEnabled = false;
            IsChannelDropdownEnabled = false;
            IsGameSearchEnabled = false;
            IsConnected = false;

            Players.Clear();
            Games.Clear();
            hostedGames.Clear();
            followedGames.Clear();

            IsGameCreationPanelVisible = false;

            // Switch channel to default
            if (localGame != null)
            {
                int gameIndex = channelOptions.FindIndex(name => name == localGame.UIName);
                if (gameIndex > -1)
                    SelectedChannelIndex = gameIndex;
            }

            if (gameCheckCancellation != null)
                gameCheckCancellation.Cancel();
        }));
    }

    private void ConnectionManager_BannedFromChannel(object sender, ChannelEventArgs e)
    {
        uiThreadMarshaller.AddCallback(new Action(() =>
        {
            var game = hostedGames.Find(g => g.ChannelName == e.ChannelName);
            if (game != null)
            {
                connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.White, string.Format(
                    "Cannot join game {0}, you've been banned by the game host!".L10N("Client:Main:PlayerBannedByHost"), game.RoomName)));
                isJoiningGame = false;
                if (gameOfLastJoinAttempt != null)
                {
                    if (gameOfLastJoinAttempt.IsLoadedGame)
                        gameLoadingLobby.Clear();
                    else
                        gameLobby.Clear();
                }
            }
            else
            {
                var chatChannel = connectionManager.FindChannel(e.ChannelName);
                chatChannel?.AddMessage(new ChatMessage(Rgb24Color.White, string.Format(
                    "Cannot join chat channel {0}, you're banned!".L10N("Client:Main:PlayerBannedByChannel"), chatChannel?.UIName)));
            }
        }));
    }

    private void ConnectionManager_PrivateCTCPReceived(object sender, PrivateCTCPEventArgs e)
    {
        // Handle game invite commands
        if (e.Message.StartsWith(ProgramConstants.GAME_INVITE_CTCP_COMMAND + " "))
        {
            HandleGameInviteCommand(e.Sender, e.Message.Substring(ProgramConstants.GAME_INVITE_CTCP_COMMAND.Length + 1));
            return;
        }

        if (e.Message == ProgramConstants.GAME_INVITATION_FAILED_CTCP_COMMAND)
        {
            HandleGameInvitationFailedNotification(e.Sender);
            return;
        }

        Log.Information("Unhandled private CTCP command: " + e.Message + " from " + e.Sender);
    }

    private void HandleGameInviteCommand(string sender, string argumentsString)
    {
        var arguments = argumentsString.Split(';');
        if (arguments.Length < 2 || arguments.Length > 3)
            return;

        string channelName = arguments[0];
        string gameName = arguments[1];
        string password = (arguments.Length == 3) ? arguments[2] : string.Empty;

        if (!CanReceiveInvitationMessagesFrom(sender))
            return;

        var gameIndex = hostedGames.FindIndex(g => g.ChannelName == channelName);

        if (!string.IsNullOrEmpty(GetJoinGameErrorByIndex(gameIndex)) ||
            (UserINISettings.Instance.AllowGameInvitesFromFriendsOnly &&
            !cncnetUserData.IsFriend(sender)))
        {
            connectionManager.SendCustomMessage(new QueuedMessage("PRIVMSG " + sender + " :\u0001" +
                ProgramConstants.GAME_INVITATION_FAILED_CTCP_COMMAND + "\u0001",
                QueuedMessageType.CHAT_MESSAGE, 0));
            return;
        }

        var invitationIdentity = new Tuple<string, string>(sender, channelName);
        if (invitationIndex.ContainsKey(invitationIdentity))
            return;

        // Set observable property for View to show invitation UI
        uiThreadMarshaller.AddCallback(new Action(() =>
        {
            PendingGameInvite = new PendingGameInviteData(sender, gameName, channelName, password);
        }));

        invitationIndex[invitationIdentity] = new WeakReference(null);

        // Play sound
        SoundToPlay = "pm.wav";
    }

    private void HandleGameInvitationFailedNotification(string sender)
    {
        if (!CanReceiveInvitationMessagesFrom(sender))
            return;

        if (isInGameRoom && !ProgramConstants.IsInGame)
        {
            gameLobby.AddWarning(
                string.Format(("{0} could not receive your invitation. They might be in game " +
                "or only accepting invitations from friends. Ensure your game is " +
                "unlocked and visible in the lobby before trying again.").L10N("Client:Main:InviteNotDelivered"), sender));
        }
    }

    private bool CanReceiveInvitationMessagesFrom(string username)
    {
        IRCUser? iu = connectionManager.UserList.Find(u => u.Name == username);
        if (iu == null)
            return false;
        if (cncnetUserData.IsIgnored(iu.Ident))
            return false;
        return true;
    }

    private void SharedUILogic_GameProcessStarted()
    {
        connectionManager.SendCustomMessage(new QueuedMessage("AWAY " + (char)58 + "In-game",
            QueuedMessageType.SYSTEM_MESSAGE, 0));
    }

    private void SharedUILogic_GameProcessExited()
    {
        connectionManager.SendCustomMessage(new QueuedMessage("AWAY",
            QueuedMessageType.SYSTEM_MESSAGE, 0));
    }

    private void Instance_SettingsSaved(object sender, EventArgs e)
    {
        if (!connectionManager.IsConnected)
            return;

        foreach (CnCNetGame game in gameCollection.GameList)
        {
            if (!game.Supported)
                continue;

            if (game.InternalName.ToUpper() == localGameID)
                continue;

            if (followedGames.Contains(game.InternalName) &&
                !UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
            {
                connectionManager.FindChannel(game.GameBroadcastChannel)?.Leave();
                followedGames.Remove(game.InternalName);
            }
            else if (!followedGames.Contains(game.InternalName) &&
                UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
            {
                connectionManager.FindChannel(game.GameBroadcastChannel)?.Join();
                followedGames.Add(game.InternalName);
            }
        }
    }

    private void GameBroadcastChannel_UserLeftOrQuit(object sender, UserNameEventArgs e)
    {
        uiThreadMarshaller.AddCallback(new Action(() =>
        {
            int gameIndex = hostedGames.FindIndex(g => g.HostName == e.UserName);
            if (gameIndex > -1)
            {
                hostedGames.RemoveAt(gameIndex);
                SortAndRefreshHostedGames();
                DismissInvalidInvitations();
            }
        }));
    }

    private void GameBroadcastChannel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
    {
        var channel = (Channel)sender;
        var channelUser = channel.Users.Find(e.UserName);
        if (channelUser == null)
            return;

        // Handle update notifications
        if (localGame != null &&
            channel.ChannelName == localGame.GameBroadcastChannel &&
            !updateDenied &&
            channelUser.IsAdmin &&
            !isInGameRoom &&
            e.Message.StartsWith("UPDATE ") &&
            e.Message.Length > 7)
        {
            string version = e.Message.Substring(7);
            if (version != ProgramConstants.GAME_VERSION)
            {
                // Set observable property for View to show update dialog
                uiThreadMarshaller.AddCallback(new Action(() =>
                {
                    IsUpdateCheckNeeded = true;
                }));
            }
        }

        if (!e.Message.StartsWith("GAME "))
            return;

        string msg = e.Message.Substring(5);
        string[] splitMessage = msg.Split(new char[] { ';' });

        if (splitMessage.Length != 14)
        {
            Log.Warning("Ignoring CTCP game message because of an invalid amount of parameters.");

            if (hostedGames.Count == 0 && !ctcpInvalidGameMessageShown)
            {
                ctcpInvalidGameMessageShown = true;
                string message = ("There are no games listed but you are indeed connected. The client did receive a game message but can't add it to the list because the message is invalid. " +
                    "You can ignore this prompt if there are games listed later. " +
                    "Otherwise, this usually means that your client is outdated, or, in a rare case, newer than others. Please check for updates.").L10N("Client:Main:InvalidGameMessage");

                uiThreadMarshaller.AddCallback(new Action(() =>
                {
                    connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.Gray, message));
                }));
            }
            return;
        }

        try
        {
            string revision = splitMessage[0];
            if (revision != ProgramConstants.CNCNET_PROTOCOL_REVISION)
                return;

            string gameVersion = splitMessage[1];
            int maxPlayers = Conversions.IntFromString(splitMessage[2], 0);
            string gameRoomChannelName = splitMessage[3];
            string gameRoomDisplayName = splitMessage[4];
            bool locked = Conversions.BooleanFromString(splitMessage[5].Substring(0, 1), true);
            bool isCustomPassword = Conversions.BooleanFromString(splitMessage[5].Substring(1, 1), false);
            bool isClosed = Conversions.BooleanFromString(splitMessage[5].Substring(2, 1), true);
            bool isLoadedGame = Conversions.BooleanFromString(splitMessage[5].Substring(3, 1), false);
            bool isLadder = Conversions.BooleanFromString(splitMessage[5].Substring(4, 1), false);
            string[] players = splitMessage[6].Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> playerNames = players.ToList();
            string mapName = splitMessage[7];
            string gameMode = splitMessage[8];

            string[] tunnelAddressAndPort = splitMessage[9].Split(':');
            string tunnelAddress = tunnelAddressAndPort[0];
            int tunnelPort = int.Parse(tunnelAddressAndPort[1]);

            string loadedGameId = splitMessage[10];
            int skillLevel = ClientConfiguration.Instance.NormalizeSkillLevel(
                Conversions.IntFromString(splitMessage[11], ClientConfiguration.Instance.DefaultSkillLevelIndex));
            string mapHash = splitMessage[12];

            int[]? gameOptionValues = null;

            if (gameVersion == ProgramConstants.GAME_VERSION && channel.ChannelName == localGame?.GameBroadcastChannel)
            {
                var broadcastableSettings = gameLobby.GetBroadcastableSettings();
                if (broadcastableSettings != null && broadcastableSettings.Count > 0 && !string.IsNullOrEmpty(splitMessage[13]))
                {
                    gameOptionValues = new int[broadcastableSettings.Count];
                    string[] allValueStrings = splitMessage[13].Split(',');

                    int checkboxCount = gameLobby.GetBroadcastableCheckboxCount();
                    int packedCheckboxCount = (checkboxCount + 31) / 32;

                    if (checkboxCount > 0 && allValueStrings.Length >= packedCheckboxCount)
                    {
                        int[] packedCheckboxes = new int[packedCheckboxCount];
                        for (int i = 0; i < packedCheckboxCount; i++)
                            packedCheckboxes[i] = int.Parse(allValueStrings[i]);

                        for (int i = 0; i < checkboxCount; i++)
                        {
                            int packedIndex = i / 32;
                            int bitIndex = i % 32;
                            gameOptionValues[i] = (packedCheckboxes[packedIndex] & (1 << bitIndex)) != 0 ? 1 : 0;
                        }
                    }

                    int dropdownCount = gameLobby.GetBroadcastableDropdownCount();
                    if (dropdownCount > 0)
                    {
                        int count = Math.Min(allValueStrings.Length - packedCheckboxCount, dropdownCount);
                        for (int i = 0; i < count; i++)
                            gameOptionValues[checkboxCount + i] = int.Parse(allValueStrings[packedCheckboxCount + i]);
                    }
                }
            }

            CnCNetGame? cncnetGame = gameCollection.GameList.Find(g => g.GameBroadcastChannel == channel.ChannelName);
            if (cncnetGame == null)
                return;

            if (tunnelHandler.Tunnels.Count == 0)
            {
                Log.Warning("Ignoring CTCP game message because there are no tunnels at all.");
                if (hostedGames.Count == 0 && !ctcpNoTunnelMessageShown)
                {
                    ctcpNoTunnelMessageShown = true;
                    string message = ("There are no games listed. The client did receive a valid game message but can't add it to the list because there are no available tunnels. " +
                        "You can ignore this prompt if there are games listed later. Otherwise, it might indicate a network problem to CnCNet HTTP service.").L10N("Client:Main:NoTunnels");
                    uiThreadMarshaller.AddCallback(new Action(() =>
                    {
                        connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.Gray, message));
                    }));
                }
                return;
            }

            CnCNetTunnel? tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort);
            if (tunnel == null)
            {
                Log.Warning(string.Format("Ignoring CTCP game message because the specified tunnel {0}:{1} is not available.", tunnelAddress, tunnelPort));
                if (hostedGames.Count == 0 && !ctcpNoTunnelForGamesMessageShown)
                {
                    ctcpNoTunnelForGamesMessageShown = true;
                    string message = string.Format(("There are no games listed. The client did receive a valid game message but can't add it to the list because the specified tunnel is not available. " +
                        "You can ignore this prompt if there are games listed later. Otherwise, please contact support at {0}.").L10N("Client:Main:NoTunnelForGames"), ClientConfiguration.Instance.LongSupportURL);
                    uiThreadMarshaller.AddCallback(new Action(() =>
                    {
                        connectionManager.MainChannel?.AddMessage(new ChatMessage(Rgb24Color.Gray, message));
                    }));
                }
                return;
            }

            HostedCnCNetGame game = new HostedCnCNetGame(gameRoomChannelName, revision, gameVersion, maxPlayers,
                gameRoomDisplayName, isCustomPassword, true, players,
                e.UserName, mapName, gameMode, mapHash);
            game.IsLoadedGame = isLoadedGame;
            game.MatchID = loadedGameId;
            game.LastRefreshTime = DateTime.Now;
            game.IsLadder = isLadder;
            game.Game = cncnetGame;
            game.Locked = locked || (game.IsLoadedGame && !game.Players.Contains(ProgramConstants.PLAYERNAME));
            game.Incompatible = cncnetGame == localGame && game.GameVersion != ProgramConstants.GAME_VERSION;
            game.TunnelServer = tunnel;
            game.SkillLevel = skillLevel;
            game.BroadcastedGameOptionValues = gameOptionValues;

            uiThreadMarshaller.AddCallback(new Action(() =>
            {
                if (isClosed)
                {
                    int index = hostedGames.FindIndex(g => g.HostName == e.UserName);
                    if (index > -1)
                    {
                        hostedGames.RemoveAt(index);
                        DismissInvalidInvitations();
                    }
                    return;
                }

                int gameIndex = hostedGames.FindIndex(g => g.HostName == e.UserName);
                if (gameIndex > -1)
                {
                    hostedGames[gameIndex] = game;
                }
                else
                {
                    if (UserINISettings.Instance.PlaySoundOnGameHosted &&
                        cncnetGame.InternalName == localGameID.ToLower() &&
                        !ProgramConstants.IsInGame && !game.Locked)
                    {
                        SoundToPlay = "gamecreated.wav";
                    }
                    hostedGames.Add(game);
                }
                SortAndRefreshHostedGames();
                LoadMapPreviewForGame(game);
            }));
        }
        catch (Exception ex)
        {
            Log.Warning("Game parsing error: " + ex.ToString());
        }
    }

    private void DismissInvalidInvitations()
    {
        var toDismiss = new List<Tuple<string, string>>();

        foreach (var invitation in invitationIndex)
        {
            var gameIndex = hostedGames.FindIndex(g =>
                g.HostName == invitation.Key.Item1 &&
                g.ChannelName == invitation.Key.Item2);

            if (gameIndex == -1)
                toDismiss.Add(invitation.Key);
        }

        foreach (var invitationIdentity in toDismiss)
        {
            invitationIndex.Remove(invitationIdentity);

            if (PendingGameInvite != null &&
                PendingGameInvite.Sender == invitationIdentity.Item1 &&
                PendingGameInvite.ChannelName == invitationIdentity.Item2)
            {
                PendingGameInvite = null;
            }
        }
    }

    private void LoadMapPreviewForGame(HostedCnCNetGame game)
    {
        Task.Run(() =>
        {
            try
            {
                var map = mapLoader.FindMapByHash(game.MapHash);
                if (map == null)
                {
                    var defaultImage = mapLoader.GetDefaultPreviewImage();
                    uiThreadMarshaller.AddCallback(() => game.MapPreviewImage = defaultImage);
                    return;
                }

                using var lease = mapLoader.GetCachedPreviewImageFromMap(map, syncLoadOnCacheMiss: true);
                if (lease?.Value == null)
                {
                    var defaultImage = mapLoader.GetDefaultPreviewImage();
                    uiThreadMarshaller.AddCallback(() => game.MapPreviewImage = defaultImage);
                    return;
                }

                using var ms = new MemoryStream();
                lease.Value.Save(ms, new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder
                {
                    BitsPerPixel = SixLabors.ImageSharp.Formats.Bmp.BmpBitsPerPixel.Pixel32,
                    SupportTransparency = true
                });
                ms.Position = 0;
                var image = SixLabors.ImageSharp.Image.Load(ms);
                uiThreadMarshaller.AddCallback(() => game.MapPreviewImage = image);
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load map preview for game " + game.RoomName + ": " + ex);
                var defaultImage = mapLoader.GetDefaultPreviewImage();
                uiThreadMarshaller.AddCallback(() => game.MapPreviewImage = defaultImage);
            }
        });
    }

    private HostedCnCNetGame? GetHostedGameForUser(IRCUser user)
    {
        return hostedGames.FirstOrDefault(g => g.Players.Contains(user.Name));
    }

    partial void OnSelectedPlayerIndexChanged(int value)
    {
        BuildPlayerContextMenuItems();
    }

    partial void OnSelectedChatMessageIndexChanged(int value)
    {
        BuildChatContextMenuItems();
    }

    partial void OnSelectedGameIndexChanged(int value)
    {
        SelectedGame = value >= 0 && value < Games.Count ? Games[value] : null;
        IsJoinGameButtonEnabled = IsConnected && value >= 0;
        BuildGameContextMenuItems();
    }

    // --- Context menu builders ---

    private void BuildPlayerContextMenuItems()
    {
        var items = new List<ContextMenuItem>();
        if (SelectedPlayerIndex < 0 || SelectedPlayerIndex >= Players.Count)
        {
            PlayerContextMenuItems.Clear();
            foreach (var item in items)
                PlayerContextMenuItems.Add(item);
            return;
        }

        var player = Players[SelectedPlayerIndex];
        var ircUser = connectionManager.UserList.Find(u => u.Name == player.Name);
        bool isOnline = ircUser != null;
        bool showInvite = !string.IsNullOrEmpty(inviteChannelName) && !ProgramConstants.IsInGame;

        if (isOnline)
            items.Add(new ContextMenuItem("Private Message".L10N("Client:Main:PrivateMessage"),
                OpenSelectedPlayerPrivateMessageCommand));

        items.Add(new ContextMenuItem(
            player.IsFriend ? "Remove Friend".L10N("Client:Main:RemoveFriend") : "Add Friend".L10N("Client:Main:AddFriend"),
            ToggleSelectedPlayerFriendCommand));

        items.Add(new ContextMenuItem(
            player.IsIgnored ? "Unblock".L10N("Client:Main:Unblock") : "Block".L10N("Client:Main:Block"),
            ToggleSelectedPlayerIgnoreCommand,
            IsEnabled: !player.IsAdmin));

        if (showInvite && isOnline)
        {
            items.Add(new ContextMenuItem("", IsSeparator: true));
            items.Add(new ContextMenuItem("Invite".L10N("Client:Main:Invite"),
                InviteSelectedPlayerToGameCommand));
        }

        if (isOnline)
        {
            items.Add(new ContextMenuItem("", IsSeparator: true));
            items.Add(new ContextMenuItem("Join".L10N("Client:Main:Join"),
                JoinSelectedPlayerGameCommand));
        }

        PlayerContextMenuItems.Clear();
        foreach (var item in items)
            PlayerContextMenuItems.Add(item);
    }

    private void BuildChatContextMenuItems()
    {
        var items = new List<ContextMenuItem>();
        if (SelectedChatMessageIndex < 0 || SelectedChatMessageIndex >= ChatMessages.Count)
        {
            ChatContextMenuItems.Clear();
            foreach (var item in items)
                ChatContextMenuItems.Add(item);
            return;
        }

        var msg = ChatMessages[SelectedChatMessageIndex];

        if (!string.IsNullOrEmpty(msg.SenderName))
        {
            var ircUser = connectionManager.UserList.Find(u => u.Name == msg.SenderName);
            bool isOnline = ircUser != null;
            bool isFriend = cncnetUserData.IsFriend(msg.SenderName);
            bool isIgnored = !string.IsNullOrEmpty(msg.SenderIdent) && cncnetUserData.IsIgnored(msg.SenderIdent);

            if (isOnline)
                items.Add(new ContextMenuItem("Private Message".L10N("Client:Main:PrivateMessage"),
                    OpenSelectedChatMessageSenderPrivateMessageCommand));

            items.Add(new ContextMenuItem(
                isFriend ? "Remove Friend".L10N("Client:Main:RemoveFriend") : "Add Friend".L10N("Client:Main:AddFriend"),
                ToggleSelectedChatMessageSenderFriendCommand));

            items.Add(new ContextMenuItem(
                isIgnored ? "Unblock".L10N("Client:Main:Unblock") : "Block".L10N("Client:Main:Block"),
                ToggleSelectedChatMessageSenderIgnoreCommand));

            if (isOnline)
            {
                items.Add(new ContextMenuItem("", IsSeparator: true));
                items.Add(new ContextMenuItem("Join".L10N("Client:Main:Join"),
                    JoinSelectedChatMessageSenderGameCommand));
            }
        }

        // Link operations
        var links = msg.Message.GetLinks();
        if (links != null && links.Length > 0)
        {
            if (items.Count > 0)
                items.Add(new ContextMenuItem("", IsSeparator: true));

            foreach (string link in links)
            {
                string displayLink = link.Length > 40 ? link[..30] + "..." + link[^5..] : link;

                items.Add(new ContextMenuItem(
                    string.Format("Open Link {0}".L10N("Client:Main:OpenLink"), displayLink),
                    new RelayCommand(() =>
                    {
                        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link) { UseShellExecute = true }); }
                        catch { }
                    })));

                items.Add(new ContextMenuItem(
                    string.Format("Copy Link {0}".L10N("Client:Main:CopyLink"), displayLink),
                    new RelayCommand(() =>
                    {
                        try { clipboardService.SetTextAsync(link); }
                        catch { }
                    })));
            }
        }

        ChatContextMenuItems.Clear();
        foreach (var item in items)
            ChatContextMenuItems.Add(item);
    }

    private void BuildGameContextMenuItems()
    {
        var items = new List<ContextMenuItem>();
        if (SelectedGameIndex < 0 || SelectedGameIndex >= Games.Count)
        {
            GameContextMenuItems.Clear();
            foreach (var item in items)
                GameContextMenuItems.Add(item);
            return;
        }

        var game = Games[SelectedGameIndex];
        string hostName = game.HostName;
        bool isFriend = cncnetUserData.IsFriend(hostName);
        bool isIgnored = cncnetUserData.IsIgnored(connectionManager.UserList.Find(u => u.Name == hostName)?.Ident);

        items.Add(new ContextMenuItem("Private Message".L10N("Client:Main:PrivateMessage"),
            OpenSelectedGameHostPrivateMessageCommand));

        items.Add(new ContextMenuItem(
            isFriend ? "Remove Friend".L10N("Client:Main:RemoveFriend") : "Add Friend".L10N("Client:Main:AddFriend"),
            ToggleSelectedGameHostFriendCommand));

        items.Add(new ContextMenuItem(
            isIgnored ? "Unblock".L10N("Client:Main:Unblock") : "Block".L10N("Client:Main:Block"),
            ToggleSelectedGameHostIgnoreCommand));

        items.Add(new ContextMenuItem("", IsSeparator: true));
        items.Add(new ContextMenuItem("Join".L10N("Client:Main:Join"),
            JoinSelectedGameCommand));

        GameContextMenuItems.Clear();
        foreach (var item in items)
            GameContextMenuItems.Add(item);
    }

    #endregion
}

/// <summary>
/// Simple DTO implementing IPlayerListItem for the View's player list binding.
/// </summary>
public record PlayerListItem : IPlayerListItem
{
    public PlayerListItem(string name, bool isAdmin, bool isFriend, bool isIgnored, bool hasVoice, int gameId)
    {
        Name = name;
        IsAdmin = isAdmin;
        IsFriend = isFriend;
        IsIgnored = isIgnored;
        HasVoice = hasVoice;
        GameId = gameId;
    }

    public string Name { get; }
    public bool IsAdmin { get; }
    public bool IsFriend { get; }
    public bool IsIgnored { get; }
    public bool HasVoice { get; }
    public int GameId { get; }
}


/// <summary>
/// Sort direction for game list.
/// </summary>
public enum SortDirection
{
    None,
    Asc,
    Desc
}

/// <summary>
/// Data for a pending game invite notification.
/// </summary>
public record PendingGameInviteData(string Sender, string GameName, string ChannelName, string Password) : IPendingGameInviteData;


