using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

using ClientCore;
using ClientCore.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DXMainClientViewModel.Domain;
using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Multiplayer.GameLobby.CommandHandlers;
using DXMainClientViewModel.Online;
using DXMainClientViewModel.Online.EventArguments;
using Rampastring.Tools;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// ViewModel for CnCNet multiplayer game lobby.
/// Contains all business logic from CnCNetGameLobby.cs except XNA UI rendering.
/// </summary>
public partial class CnCNetGameLobbyViewModel : MultiplayerGameLobbyViewModel, ICnCNetGameLobbyViewModel
{
    private const int HUMAN_PLAYER_OPTIONS_LENGTH = 3;
    private const int AI_PLAYER_OPTIONS_LENGTH = 2;

    private const double GAME_BROADCAST_INTERVAL = 30.0;
    private const double GAME_BROADCAST_ACCELERATION = 10.0;
    private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;

    private const string MAP_SHARING_FAIL_MESSAGE = "MAPFAIL";
    private const string MAP_SHARING_DOWNLOAD_REQUEST = "MAPOK";
    private const string MAP_SHARING_UPLOAD_REQUEST = "MAPREQ";
    private const string MAP_SHARING_DISABLED_MESSAGE = "MAPSDISABLED";
    private const string CHEAT_DETECTED_MESSAGE = "CD";
    private const string DICE_ROLL_MESSAGE = "DR";
    private const string CHANGE_TUNNEL_SERVER_MESSAGE = "CHTNL";

    // --- Dependencies ---
    private readonly TunnelHandler tunnelHandler;
    private readonly GameCollection gameCollection;
    private readonly CnCNetUserData cncnetUserData;
    private readonly CnCNetManager connectionManager;
    private readonly string localGame;
    private readonly IGameHostInactiveCheckerService gameHostInactiveChecker;

    // --- State ---
    private Channel channel;
    private string hostName;
    private string _gameRoomNameValue;
    private string gameFilesHash;
    private bool closed;
    private bool tunnelErrorModeInternal;
    private string lastMapSHA1;
    private string lastMapName;
    private string lastGameMode;
    private IRCColor chatColor;
    private Timer gameBroadcastTimer;

    private readonly List<string> hostUploadedMaps = new();
    private readonly List<string> chatCommandDownloadedMaps = new();

    // --- CTCP command handlers ---
    private CommandHandlerBase[] ctcpCommandHandlers;

    // --- Observable state ---
    [ObservableProperty]
    private string _channelName = string.Empty;

    [ObservableProperty]
    private string _selectedTunnelName = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _gameRoomName = string.Empty;

    [ObservableProperty]
    private int _playerLimit;

    [ObservableProperty]
    private int _skillLevel;

    [ObservableProperty]
    private bool _isCustomPassword;

    [ObservableProperty]
    private bool _tunnelErrorMode;

    // --- IsHost ---
    private bool _isHost;
    public override bool IsHost
    {
        get => _isHost;
        set
        {
            if (SetProperty(ref _isHost, value))
                OnPropertyChanged(nameof(IsHost));
        }
    }

    // --- Events ---
    public event EventHandler GameLeft;
    public event EventHandler<string> TunnelSelectionRequested;
    public event EventHandler<string> GameLobbySettingsRequested;

    // --- Constructor ---

    public CnCNetGameLobbyViewModel(
        MapLoader mapLoader,
        DiscordHandler discordHandler,
        IGameProcessService gameProcessService,
        IUIThreadMarshaller uiThreadMarshaller,
        Random random,
        CnCNetManager connectionManager,
        TunnelHandler tunnelHandler,
        GameCollection gameCollection,
        CnCNetUserData cncnetUserData,
        IGameHostInactiveCheckerService gameHostInactiveChecker)
        : base(mapLoader, discordHandler, gameProcessService, uiThreadMarshaller, random)
    {
        this.connectionManager = connectionManager;
        this.tunnelHandler = tunnelHandler;
        this.gameCollection = gameCollection;
        this.cncnetUserData = cncnetUserData;
        this.gameHostInactiveChecker = gameHostInactiveChecker;
        this.localGame = ClientConfiguration.Instance.LocalGame;

        ctcpCommandHandlers = new CommandHandlerBase[]
        {
            new IntCommandHandler("OR", HandleOptionsRequest),
            new IntCommandHandler("R", HandleReadyRequest),
            new StringCommandHandler("PO", ApplyPlayerOptions),
            new StringCommandHandler(PlayerExtraOptions.CNCNET_MESSAGE_KEY, ApplyPlayerExtraOptions),
            new StringCommandHandler("GO", ApplyGameOptions),
            new StringCommandHandler("START", NonHostLaunchGame),
            new NotificationHandler("AISPECS", HandleNotification, AISpectatorsNotification),
            new NotificationHandler("GETREADY", HandleNotification, GetReadyNotification),
            new NotificationHandler("INSFSPLRS", HandleNotification, InsufficientPlayersNotification),
            new NotificationHandler("TMPLRS", HandleNotification, TooManyPlayersNotification),
            new NotificationHandler("CLRS", HandleNotification, SharedColorsNotification),
            new NotificationHandler("SLOC", HandleNotification, SharedStartingLocationNotification),
            new NotificationHandler("LCKGME", HandleNotification, LockGameNotification),
            new IntNotificationHandler("NVRFY", HandleIntNotification, NotVerifiedNotification),
            new IntNotificationHandler("INGM", HandleIntNotification, StillInGameNotification),
            new StringCommandHandler(MAP_SHARING_UPLOAD_REQUEST, HandleMapUploadRequest),
            new StringCommandHandler(MAP_SHARING_FAIL_MESSAGE, HandleMapTransferFailMessage),
            new StringCommandHandler(MAP_SHARING_DOWNLOAD_REQUEST, HandleMapDownloadRequest),
            new NoParamCommandHandler(MAP_SHARING_DISABLED_MESSAGE, HandleMapSharingBlockedMessage),
            new NoParamCommandHandler("STRTD", GameStartedNotification),
            new NoParamCommandHandler("RETURN", ReturnNotification),
            new IntCommandHandler("TNLPNG", HandleTunnelPing),
            new StringCommandHandler("FHSH", FileHashNotification),
            new StringCommandHandler("MM", CheaterNotification),
            new StringCommandHandler(DICE_ROLL_MESSAGE, (sender, msg) => HandleDiceRollResult(sender, msg)),
            new NoParamCommandHandler(CHEAT_DETECTED_MESSAGE, HandleCheatDetectedMessage),
            new StringCommandHandler(CHANGE_TUNNEL_SERVER_MESSAGE, HandleTunnelServerChangeMessage),
            new StringCommandHandler("GSETTINGS", ApplyGameLobbySettings)
        };

        AddChatBoxCommand(new ChatBoxCommand("TUNNELINFO",
            "View tunnel server information".L10N("Client:Main:TunnelInfoCommand"), false, PrintTunnelServerInformation));
        AddChatBoxCommand(new ChatBoxCommand("CHANGETUNNEL",
            "Change the used CnCNet tunnel server (game host only)".L10N("Client:Main:ChangeTunnelCommand"),
            true, _ => TunnelSelectionRequested?.Invoke(this, "Select tunnel server:".L10N("Client:Main:SelectTunnelServer"))));
        AddChatBoxCommand(new ChatBoxCommand("DOWNLOADMAP",
            "Download a map from CNCNet's map server using a map ID and an optional filename.\nExample: \"/downloadmap MAPID [2] My Battle Map\"".L10N("Client:Main:DownloadMapCommandDescription"),
            true, DownloadMapByIdCommand));

        MapSharer.MapDownloadFailed += MapSharer_MapDownloadFailed;
        MapSharer.MapDownloadComplete += MapSharer_MapDownloadComplete;
        MapSharer.MapUploadFailed += MapSharer_MapUploadFailed;
        MapSharer.MapUploadComplete += MapSharer_MapUploadComplete;

        if (gameHostInactiveChecker != null)
        {
            gameHostInactiveChecker.CloseEvent += GameHostInactiveChecker_CloseEvent;
            gameHostInactiveChecker.WarningRequested += GameHostInactiveChecker_WarningRequested;
        }

        gameBroadcastTimer = new Timer(GAME_BROADCAST_INTERVAL * 1000);
        gameBroadcastTimer.AutoReset = true;
        gameBroadcastTimer.Elapsed += GameBroadcastTimer_Elapsed;
    }

    protected override int MaxPlayerCount => PlayerLimit;

    // --- Lifecycle ---

    public override void Initialize()
    {
        base.Initialize();
        MapLoader.MapChanged += MapLoader_MapChanged;
    }

    protected override void Clean()
    {
        base.Clean();
        MapLoader.MapChanged -= MapLoader_MapChanged;
        MapSharer.MapDownloadFailed -= MapSharer_MapDownloadFailed;
        MapSharer.MapDownloadComplete -= MapSharer_MapDownloadComplete;
        MapSharer.MapUploadFailed -= MapSharer_MapUploadFailed;
        MapSharer.MapUploadComplete -= MapSharer_MapUploadComplete;

        if (gameHostInactiveChecker != null)
        {
            gameHostInactiveChecker.CloseEvent -= GameHostInactiveChecker_CloseEvent;
            gameHostInactiveChecker.WarningRequested -= GameHostInactiveChecker_WarningRequested;
        }
    }

    public void SetUp(Channel channel, bool isHost, int playerLimit,
        CnCNetTunnel tunnel, string hostName, bool isCustomPassword,
        int skillLevel)
    {
        this.channel = channel;
        channel.MessageAdded += Channel_MessageAdded;
        channel.CTCPReceived += Channel_CTCPReceived;
        channel.UserKicked += Channel_UserKicked;
        channel.UserQuitIRC += Channel_UserQuitIRC;
        channel.UserLeft += Channel_UserLeft;
        channel.UserAdded += Channel_UserAdded;
        channel.UserNameChanged += Channel_UserNameChanged;
        channel.UserListReceived += Channel_UserListReceived;

        this.hostName = hostName;
        PlayerLimit = playerLimit;
        IsCustomPassword = isCustomPassword;
        SkillLevel = ClientConfiguration.Instance.NormalizeSkillLevel(skillLevel);
        this._gameRoomNameValue = channel.UIName;

        hostUploadedMaps.Clear();
        chatCommandDownloadedMaps.Clear();

        ChannelName = channel.ChannelName;
        GameRoomName = _gameRoomNameValue;
        PlayerLimit = playerLimit;
        SkillLevel = skillLevel;
        IsCustomPassword = isCustomPassword;

        IsHost = isHost;
        if (isHost)
        {
            RandomSeed = random.Next();
            RefreshMapSelectionUI();
            StartInactiveCheck();
        }
        else
        {
            channel.ChannelModesChanged += Channel_ChannelModesChanged;
            AIPlayers.Clear();
        }

        tunnelHandler.CurrentTunnel = tunnel;
        tunnelHandler.CurrentTunnelPinged += TunnelHandler_CurrentTunnelPinged;
        SelectedTunnelName = tunnel?.Name ?? string.Empty;

        connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
        connectionManager.Disconnected += ConnectionManager_Disconnected;

        Refresh(isHost);
    }

    public void OnJoined()
    {
        FileHashCalculator fhc = new FileHashCalculator();
        fhc.CalculateHashes();

        gameFilesHash = fhc.GetCompleteHash();

        if (IsHost)
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} +klnNs {1} {2}", channel.ChannelName,
                channel.Password, PlayerLimit),
                QueuedMessageType.SYSTEM_MESSAGE, 50));

            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("TOPIC {0} :{1}", channel.ChannelName,
                ProgramConstants.CNCNET_PROTOCOL_REVISION + ";" + localGame.ToLower()),
                QueuedMessageType.SYSTEM_MESSAGE, 50));

            gameBroadcastTimer.Interval = INITIAL_GAME_BROADCAST_DELAY * 1000;
            gameBroadcastTimer.Start();
        }
        else
        {
            channel.SendCTCPMessage("FHSH " + gameFilesHash, QueuedMessageType.SYSTEM_MESSAGE, 10);
        }

        ResetAutoReadyCheckbox();
        UpdatePing();
        UpdateDiscordPresence(true);
    }

    public override void Clear()
    {
        base.Clear();

        if (channel != null)
        {
            channel.MessageAdded -= Channel_MessageAdded;
            channel.CTCPReceived -= Channel_CTCPReceived;
            channel.UserKicked -= Channel_UserKicked;
            channel.UserQuitIRC -= Channel_UserQuitIRC;
            channel.UserLeft -= Channel_UserLeft;
            channel.UserAdded -= Channel_UserAdded;
            channel.UserNameChanged -= Channel_UserNameChanged;
            channel.UserListReceived -= Channel_UserListReceived;

            if (!IsHost)
                channel.ChannelModesChanged -= Channel_ChannelModesChanged;

            connectionManager.RemoveChannel(channel);
        }

        IsEnabled = false;

        connectionManager.ConnectionLost -= ConnectionManager_ConnectionLost;
        connectionManager.Disconnected -= ConnectionManager_Disconnected;

        gameBroadcastTimer?.Stop();
        closed = false;
        DraftMessage = string.Empty;

        tunnelHandler.CurrentTunnel = null;
        tunnelHandler.CurrentTunnelPinged -= TunnelHandler_CurrentTunnelPinged;

        GameLeft?.Invoke(this, EventArgs.Empty);
        ResetDiscordPresence();
    }

    [RelayCommand]
    public void LeaveGameLobby()
    {
        if (IsHost)
        {
            StopInactiveCheck();
            closed = true;
            BroadcastGame();
        }

        Clear();
        channel?.Leave();
    }

    // --- Inactive host checker ---

    private void StartInactiveCheck()
    {
        if (IsCustomPassword)
            return;

        gameHostInactiveChecker?.Start();
    }

    private void StopInactiveCheck() => gameHostInactiveChecker?.Stop();

    private void GameHostInactiveChecker_CloseEvent(object sender, EventArgs e) => LeaveGameLobbyCommand.Execute(null);

    private void GameHostInactiveChecker_WarningRequested(object sender, EventArgs e)
    {
        AddNotice("Your game may be closed due to inactivity.".L10N("Client:Main:InactiveHostWarningText"), NoticeSeverity.Warning);
    }

    // --- Ping ---

    private void TunnelHandler_CurrentTunnelPinged(object sender, EventArgs e) => UpdatePing();

    private void UpdatePing()
    {
        if (tunnelHandler.CurrentTunnel == null)
            return;

        channel.SendCTCPMessage("TNLPNG " + tunnelHandler.CurrentTunnel.PingInMs, QueuedMessageType.SYSTEM_MESSAGE, 10);

        PlayerInfo pInfo = Players.Find(p => p.Name.Equals(ProgramConstants.PLAYERNAME));
        if (pInfo != null)
        {
            pInfo.Ping = tunnelHandler.CurrentTunnel.PingInMs;
            CopyPlayerDataToUI();
        }
    }

    // --- Tunnel ---

    private void PrintTunnelServerInformation(string s)
    {
        if (tunnelHandler.CurrentTunnel == null)
        {
            AddNotice("Tunnel server unavailable!".L10N("Client:Main:TunnelUnavailable"));
        }
        else
        {
            AddNotice(string.Format("Current tunnel server: {0} {1} (Players: {2}/{3}) (Official: {4})".L10N("Client:Main:TunnelInfo"),
                    tunnelHandler.CurrentTunnel.Name, tunnelHandler.CurrentTunnel.Country, tunnelHandler.CurrentTunnel.Clients, tunnelHandler.CurrentTunnel.MaxClients, tunnelHandler.CurrentTunnel.Official
                ));
        }
    }

    [RelayCommand]
    private void ChangeTunnel()
    {
        TunnelSelectionRequested?.Invoke(this, "Select tunnel server:".L10N("Client:Main:SelectTunnelServer"));
    }

    [RelayCommand]
    private void InvitePlayer()
    {
        // View handles the invite UI via InvitePlayerCommand binding
    }

    public void OnTunnelSelected(CnCNetTunnel tunnel)
    {
        channel.SendCTCPMessage($"{CHANGE_TUNNEL_SERVER_MESSAGE} {tunnel.Address}:{tunnel.Port}",
            QueuedMessageType.SYSTEM_MESSAGE, 10);
        HandleTunnelServerChange(tunnel);
    }

    // --- Game lobby settings ---

    public void OnGameLobbySettingsChanged(string newGameRoomName, int newMaxPlayers, int newSkillLevel, string newPassword)
    {
        if (!IsHost)
            return;

        bool gameNameChanged = _gameRoomNameValue != newGameRoomName;
        bool maxPlayersChanged = PlayerLimit != newMaxPlayers;
        int normalizedSkillLevel = ClientConfiguration.Instance.NormalizeSkillLevel(newSkillLevel);
        bool skillLevelChanged = SkillLevel != normalizedSkillLevel;

        string currentUserPassword = IsCustomPassword ? channel.Password : string.Empty;
        bool passwordChanged = currentUserPassword != newPassword;

        if (newMaxPlayers < Players.Count + AIPlayers.Count)
        {
            AddNotice(string.Format("Cannot reduce maximum players to {0} with {1} players currently in game."
                .L10N("Client:Main:CannotReduceMaxPlayers"), newMaxPlayers, Players.Count + AIPlayers.Count));
            return;
        }

        string oldGameRoomName = _gameRoomNameValue;
        bool oldIsCustomPassword = IsCustomPassword;
        _gameRoomNameValue = newGameRoomName;
        channel.UIName = newGameRoomName;
        GameRoomName = _gameRoomNameValue;
        PlayerLimit = newMaxPlayers;
        SkillLevel = normalizedSkillLevel;

        if (passwordChanged)
        {
            string actualNewPassword = newPassword;
            if (string.IsNullOrEmpty(newPassword))
            {
                actualNewPassword = Utilities.CalculateSHA1ForString(channel.ChannelName).Substring(0, 10);
                IsCustomPassword = false;
            }
            else
            {
                IsCustomPassword = true;
            }
            channel.ChangePassword(actualNewPassword, 10);
        }

        BroadcastGameLobbySettings();

        if (gameNameChanged)
            AddNotice(string.Format("Game room name changed from \"{0}\" to \"{1}\"."
                .L10N("Client:Main:GameNameChanged"), oldGameRoomName, _gameRoomNameValue));

        if (maxPlayersChanged)
        {
            CopyPlayerDataToUI();
            AddNotice(string.Format("Maximum players changed to {0}."
                .L10N("Client:Main:MaxPlayersChanged"), newMaxPlayers));
        }

        if (skillLevelChanged)
        {
            string[] skillLevelOptions = ClientConfiguration.Instance.GetSkillLevelOptions();
            string skillLevelName = skillLevelOptions[SkillLevel];
            string localizedSkillLevel = skillLevelName.L10N($"INI:ClientDefinitions:SkillLevel:{SkillLevel}");
            AddNotice(string.Format("Skill level changed to {0}."
                .L10N("Client:Main:SkillLevelChanged"), localizedSkillLevel));
        }

        if (passwordChanged)
        {
            if (string.IsNullOrEmpty(newPassword))
                AddNotice("Password removed from the game.".L10N("Client:Main:PasswordRemoved"));
            else if (!oldIsCustomPassword)
                AddNotice("Password added to the game.".L10N("Client:Main:PasswordAdded"));
            else
                AddNotice("Password changed.".L10N("Client:Main:PasswordChanged"));
        }

        BroadcastGame();
    }

    private void BroadcastGameLobbySettings()
    {
        if (!IsHost)
            return;

        StringBuilder sb = new StringBuilder("GSETTINGS ");
        sb.Append(_gameRoomNameValue);
        sb.Append(";");
        sb.Append(PlayerLimit);
        sb.Append(";");
        sb.Append(SkillLevel);
        sb.Append(";");
        sb.Append(Convert.ToInt32(IsCustomPassword));

        channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAME_SETTINGS_MESSAGE, 11);
    }

    private void ApplyGameLobbySettings(string sender, string message)
    {
        if (IsHost)
            return;

        string[] parts = message.Split(';');

        if (parts.Length < 4)
            return;

        string newGameRoomName = parts[0];
        int newMaxPlayers = Conversions.IntFromString(parts[1], PlayerLimit);
        int newSkillLevel = ClientConfiguration.Instance.NormalizeSkillLevel(
            Conversions.IntFromString(parts[2], SkillLevel));
        bool newIsCustomPassword = Convert.ToBoolean(Conversions.IntFromString(parts[3], 0));

        bool gameNameChanged = _gameRoomNameValue != newGameRoomName;
        bool maxPlayersChanged = PlayerLimit != newMaxPlayers;
        bool skillLevelChanged = SkillLevel != newSkillLevel;

        _gameRoomNameValue = newGameRoomName;
        channel.UIName = newGameRoomName;
        GameRoomName = _gameRoomNameValue;
        PlayerLimit = newMaxPlayers;
        SkillLevel = newSkillLevel;
        IsCustomPassword = newIsCustomPassword;

        if (gameNameChanged)
            AddNotice(string.Format("{0} changed game room name to \"{1}\"."
                .L10N("Client:Main:HostChangedGameName"), sender, _gameRoomNameValue));

        if (maxPlayersChanged)
        {
            CopyPlayerDataToUI();
            AddNotice(string.Format("{0} changed maximum players to {1}."
                .L10N("Client:Main:HostChangedMaxPlayers"), sender, newMaxPlayers));
        }

        if (skillLevelChanged)
        {
            string[] skillLevelOptions = ClientConfiguration.Instance.GetSkillLevelOptions();
            string skillLevelName = skillLevelOptions[SkillLevel];
            string localizedSkillLevel = skillLevelName.L10N($"INI:ClientDefinitions:SkillLevel:{SkillLevel}");
            AddNotice(string.Format("{0} changed skill level to {1}."
                .L10N("Client:Main:HostChangedSkillLevel"), sender, localizedSkillLevel));
        }
    }

    // --- Channel event handlers ---

    private void Channel_UserNameChanged(object sender, UserNameChangedEventArgs e)
    {
        Logger.Log("CnCNetGameLobby: Nickname change: " + e.OldUserName + " to " + e.User.Name);
        int index = Players.FindIndex(p => p.Name == e.OldUserName);
        if (index > -1)
        {
            PlayerInfo player = Players[index];
            player.Name = e.User.Name;
            AddNotice(string.Format("Player {0} changed their name to {1}".L10N("Client:Main:PlayerRename"), e.OldUserName, e.User.Name));
        }
    }

    private void Channel_UserQuitIRC(object sender, UserNameEventArgs e)
    {
        RemovePlayer(e.UserName);

        if (e.UserName == hostName)
        {
            AddNotice("The game host abandoned the game.".L10N("Client:Main:HostAbandoned"), NoticeSeverity.Warning);
            LeaveGameLobbyCommand.Execute(null);
        }
        else
            UpdateDiscordPresence();
    }

    private void Channel_UserLeft(object sender, UserNameEventArgs e)
    {
        RemovePlayer(e.UserName);

        if (e.UserName == hostName)
        {
            AddNotice("The game host abandoned the game.".L10N("Client:Main:HostAbandoned"), NoticeSeverity.Warning);
            LeaveGameLobbyCommand.Execute(null);
        }
        else
            UpdateDiscordPresence();
    }

    private void Channel_UserKicked(object sender, UserNameEventArgs e)
    {
        if (e.UserName == ProgramConstants.PLAYERNAME)
        {
            AddNotice("You were kicked from the game!".L10N("Client:Main:YouWereKicked"), NoticeSeverity.Warning);
            Clear();
            IsEnabled = false;
            return;
        }

        int index = Players.FindIndex(p => p.Name == e.UserName);

        if (index > -1)
        {
            Players.RemoveAt(index);
            CopyPlayerDataToUI();
            UpdateDiscordPresence();
            ClearReadyStatuses();
        }
    }

    private void Channel_UserListReceived(object sender, EventArgs e)
    {
        if (!IsHost)
        {
            if (channel.Users.Find(hostName) == null)
            {
                AddNotice("The game host has abandoned the game.".L10N("Client:Main:HostHasAbandoned"), NoticeSeverity.Warning);
                LeaveGameLobbyCommand.Execute(null);
            }
        }
        UpdateDiscordPresence();
    }

    private void Channel_UserAdded(object sender, ChannelUserEventArgs e)
    {
        PlayerInfo pInfo = new PlayerInfo(e.User.IRCUser.Name);
        Players.Add(pInfo);

        if (Players.Count + AIPlayers.Count > MAX_PLAYER_COUNT && AIPlayers.Count > 0)
            AIPlayers.RemoveAt(AIPlayers.Count - 1);

        if (!IsHost)
        {
            CopyPlayerDataToUI();
            return;
        }

        if (e.User.IRCUser.Name != ProgramConstants.PLAYERNAME)
        {
            ChangeMap(GameModeMap);
            BroadcastPlayerOptions();
            BroadcastPlayerExtraOptions();
            UpdateDiscordPresence();
        }
        else
        {
            Players[0].Ready = true;
            CopyPlayerDataToUI();
        }

        if (Players.Count >= PlayerLimit)
        {
            AddNotice("Player limit reached. The game room has been locked.".L10N("Client:Main:GameRoomNumberLimitReached"));
            PerformLockGame();
        }
    }

    private void RemovePlayer(string playerName)
    {
        PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

        if (pInfo != null)
        {
            Players.Remove(pInfo);
            CopyPlayerDataToUI();

            if (IsHost)
                BroadcastPlayerOptions();
        }

        if (IsHost && Locked && !ProgramConstants.IsInGame)
            PerformUnlockGame(true);
    }

    private void Channel_ChannelModesChanged(object sender, ChannelModeEventArgs e)
    {
        if (e.ModeString == "+i")
        {
            if (Players.Count >= PlayerLimit)
                AddNotice("Player limit reached. The game room has been locked.".L10N("Client:Main:GameRoomNumberLimitReached"));
            else
                AddNotice("The game host has locked the game room.".L10N("Client:Main:RoomLockedByHost"));
            Locked = true;
        }
        else if (e.ModeString == "-i")
        {
            AddNotice("The game room has been unlocked.".L10N("Client:Main:GameRoomUnlocked"));
            Locked = false;
        }
    }

    private void Channel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
    {
        Logger.Log("CnCNetGameLobby_CTCPReceived");

        foreach (CommandHandlerBase cmdHandler in ctcpCommandHandlers)
        {
            if (cmdHandler.Handle(e.UserName, e.Message))
            {
                UpdateDiscordPresence();
                return;
            }
        }

        Logger.Log("Unhandled CTCP command: " + e.Message + " from " + e.UserName);
    }

    private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
    {
        if (cncnetUserData.IsIgnored(e.Message.SenderIdent))
        {
            AddChatMessage(string.Format("Message blocked from {0}".L10N("Client:Main:MessageBlockedFromPlayer"), e.Message.SenderName));
        }
        else
        {
            AddChatMessage($"{e.Message.SenderName}: {e.Message.Message}");
        }
    }

    // --- Connection ---

    private void ConnectionManager_Disconnected(object sender, EventArgs e) => HandleConnectionLoss();

    private void ConnectionManager_ConnectionLost(object sender, ConnectionLostEventArgs e) => HandleConnectionLoss();

    private void HandleConnectionLoss()
    {
        Clear();
        IsEnabled = false;
    }

    // --- Game launch ---

    protected override void HostLaunchGame()
    {
        if (Players.Count > 1)
        {
            AddNotice("Contacting tunnel server...".L10N("Client:Main:ConnectingTunnel"));

            List<int> playerPorts = tunnelHandler.CurrentTunnel.GetPlayerPortInfo(Players.Count);

            if (playerPorts.Count < Players.Count)
            {
                TunnelSelectionRequested?.Invoke(this, ("An error occured while contacting " +
                    "the CnCNet tunnel server.\nTry picking a different tunnel server:").L10N("Client:Main:ConnectTunnelError1"));
                AddNotice(("An error occured while contacting the specified CnCNet " +
                    "tunnel server. Please try using a different tunnel server").L10N("Client:Main:ConnectTunnelError2"), NoticeSeverity.Warning);
                return;
            }

            StringBuilder sb = new StringBuilder("START ");
            sb.Append(UniqueGameID);
            for (int pId = 0; pId < Players.Count; pId++)
            {
                Players[pId].Port = playerPorts[pId];
                sb.Append(";");
                sb.Append(Players[pId].Name);
                sb.Append(";");
                sb.Append(tunnelHandler.CurrentTunnel.Address + ":");
                sb.Append(playerPorts[pId]);
            }
            channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 10);
        }
        else
        {
            Logger.Log("One player MP -- starting!");
        }

        cncnetUserData.AddRecentPlayers(Players.Select(p => p.Name), _gameRoomNameValue);

        StartGame();
    }

    // --- Player options ---

    protected override void RequestPlayerOptions(int side, int color, int start, int team)
    {
        byte[] value = new byte[]
        {
            (byte)side,
            (byte)color,
            (byte)start,
            (byte)team
        };

        int intValue = BinaryPrimitives.ReadInt32LittleEndian(value);

        channel.SendCTCPMessage(
            string.Format("OR {0}", intValue),
            QueuedMessageType.GAME_SETTINGS_MESSAGE, 6);
    }

    protected override void RequestReadyStatus()
    {
        if (Map == null || GameMode == null)
        {
            AddNotice(("The game host needs to select a different map or " +
                "you will be unable to participate in the match.").L10N("Client:Main:HostMustReplaceMap"));

            if (IsAutoReadyChecked)
                channel.SendCTCPMessage("R 0", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);

            return;
        }

        PlayerInfo pInfo = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
        if (pInfo == null)
            return;

        int readyState = 0;

        if (IsAutoReadyChecked)
            readyState = 2;
        else if (!pInfo.Ready)
            readyState = 1;

        channel.SendCTCPMessage($"R {readyState}", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);
    }

    // --- Broadcast ---

    protected override void BroadcastPlayerOptions()
    {
        StringBuilder sb = new StringBuilder("PO ");
        foreach (PlayerInfo pInfo in Players.Concat(AIPlayers))
        {
            if (pInfo.IsAI)
                sb.Append(pInfo.AILevel);
            else
                sb.Append(pInfo.Name);
            sb.Append(";");

            byte[] byteArray = new byte[]
            {
                (byte)pInfo.TeamId,
                (byte)pInfo.StartingLocation,
                (byte)pInfo.ColorId,
                (byte)pInfo.SideId,
            };

            int value = BinaryPrimitives.ReadInt32LittleEndian(byteArray);
            sb.Append(value);
            sb.Append(";");
            if (!pInfo.IsAI)
            {
                if (pInfo.AutoReady && !pInfo.IsInGame && !LastMapChangeWasInvalid)
                    sb.Append(2);
                else
                    sb.Append(Convert.ToInt32(pInfo.Ready));
                sb.Append(';');
            }
        }

        channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAME_PLAYERS_MESSAGE, 11);
    }

    protected override void BroadcastPlayerExtraOptions()
    {
        if (!IsHost)
            return;

        var playerExtraOpts = PlayerExtraOptions;
        channel.SendCTCPMessage(playerExtraOpts.ToCncnetMessage(), QueuedMessageType.GAME_PLAYERS_EXTRA_MESSAGE, 11, true);
    }

    // --- CTCP command handlers ---

    private void HandleOptionsRequest(string playerName, int options)
    {
        if (!IsHost)
            return;

        if (ProgramConstants.IsInGame)
            return;

        PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

        if (pInfo == null)
            return;

        byte[] bytes = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, options);

        int side = bytes[0];
        int color = bytes[1];
        int start = bytes[2];
        int team = bytes[3];

        if (side < 0 || side > SideCount + RandomSelectorCount)
            return;

        if (color < 0 || color > MPColors.Count)
            return;

        var randomDisallowedSides = new List<bool>(RandomSelectorCount);
        for (int i = 0; i < RandomSelectorCount; i++)
            randomDisallowedSides.Add(false);

        var disallowedSides = randomDisallowedSides.Concat(GetDisallowedSides()).ToArray();

        if (0 < side && side < SideCount && disallowedSides[side])
            return;

        if (GameModeMap?.CoopInfo != null)
        {
            if (GameModeMap.CoopInfo.DisallowedPlayerSides.Contains(side - 1) || side == SideCount + RandomSelectorCount)
                return;

            if (GameModeMap.CoopInfo.DisallowedPlayerColors.Contains(color - 1))
                return;
        }

        if (!(start == 0 || (GameModeMap?.AllowedStartingLocations?.Contains(start) ?? true)))
            return;

        if (team < 0 || team > 4)
            return;

        if (side != pInfo.SideId
            || start != pInfo.StartingLocation
            || team != pInfo.TeamId)
        {
            ClearReadyStatuses();
        }

        pInfo.SideId = side;
        pInfo.ColorId = color;
        pInfo.StartingLocation = start;
        pInfo.TeamId = team;

        CopyPlayerDataToUI();
        BroadcastPlayerOptions();
    }

    private void HandleReadyRequest(string playerName, int readyStatus)
    {
        if (!IsHost)
            return;

        PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

        if (pInfo == null)
            return;

        pInfo.Ready = readyStatus > 0;
        pInfo.AutoReady = readyStatus > 1;

        CopyPlayerDataToUI();
        BroadcastPlayerOptions();
    }

    private void ApplyPlayerOptions(string sender, string message)
    {
        if (sender != hostName)
            return;

        Players.Clear();
        AIPlayers.Clear();

        string[] parts = message.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length;)
        {
            PlayerInfo pInfo = new PlayerInfo();

            string pName = parts[i];
            int converted = Conversions.IntFromString(pName, -1);

            if (converted > -1)
            {
                pInfo.IsAI = true;
                pInfo.AILevel = converted;
                pInfo.Name = AILevelToName(converted);
            }
            else
            {
                pInfo.Name = pName;

                if (channel.Users.Find(pName) == null)
                {
                    i += HUMAN_PLAYER_OPTIONS_LENGTH;
                    continue;
                }
            }

            if (parts.Length <= i + 1)
                return;

            int playerOptions = Conversions.IntFromString(parts[i + 1], -1);
            if (playerOptions == -1)
                return;

            byte[] byteArray = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(byteArray, playerOptions);

            int team = byteArray[0];
            int start = byteArray[1];
            int color = byteArray[2];
            int side = byteArray[3];

            if (side < 0 || side > SideCount + RandomSelectorCount)
                return;

            if (color < 0 || color > MPColors.Count)
                return;

            if (start < 0 || start > MAX_PLAYER_COUNT)
                return;

            if (team < 0 || team > 4)
                return;

            pInfo.TeamId = byteArray[0];
            pInfo.StartingLocation = byteArray[1];
            pInfo.ColorId = byteArray[2];
            pInfo.SideId = byteArray[3];

            if (pInfo.IsAI)
            {
                pInfo.Ready = true;
                AIPlayers.Add(pInfo);
                i += AI_PLAYER_OPTIONS_LENGTH;
            }
            else
            {
                if (parts.Length <= i + 2)
                    return;

                int readyStatus = Conversions.IntFromString(parts[i + 2], -1);

                if (readyStatus == -1)
                    return;

                pInfo.Ready = readyStatus > 0;
                pInfo.AutoReady = readyStatus > 1;

                if (pInfo.Name == ProgramConstants.PLAYERNAME)
                    LaunchButtonText = pInfo.Ready ? BTN_LAUNCH_NOT_READY : BTN_LAUNCH_READY;

                Players.Add(pInfo);
                i += HUMAN_PLAYER_OPTIONS_LENGTH;
            }
        }

        CopyPlayerDataToUI();
    }

    private void ApplyPlayerExtraOptions(string sender, string message)
    {
        if (sender != hostName)
            return;

        PlayerExtraOptions = PlayerExtraOptions.FromMessage(message);
    }

    // --- Game options broadcast ---

    protected override void OnGameOptionChanged()
    {
        base.OnGameOptionChanged();

        if (!IsHost)
            return;

        bool[] optionValues = new bool[CheckBoxes.Count];
        for (int i = 0; i < CheckBoxes.Count; i++)
            optionValues[i] = CheckBoxes[i].IsChecked;

        List<byte> byteList = Conversions.BoolArrayIntoBytes(optionValues).ToList();

        while (byteList.Count % 4 != 0)
            byteList.Add(0);

        int integerCount = byteList.Count / 4;
        byte[] byteArray = byteList.ToArray();

        ExtendedStringBuilder sb = new ExtendedStringBuilder("GO ", true, ';');

        for (int i = 0; i < integerCount; i++)
            sb.Append(BinaryPrimitives.ReadInt32LittleEndian(byteArray.AsSpan(i * 4)));

        foreach (GameOptionDropDown dd in DropDowns)
            sb.Append(dd.SelectedIndex);

        sb.Append(Convert.ToInt32(Map?.Official ?? false));
        sb.Append(Map?.SHA1 ?? string.Empty);
        sb.Append(GameMode?.Name ?? string.Empty);
        sb.Append(FrameSendRate);
        sb.Append(MaxAhead);
        sb.Append(ProtocolVersion);
        sb.Append(RandomSeed);
        sb.Append(Convert.ToInt32(RemoveStartingLocations));
        sb.Append(Map?.UntranslatedName ?? string.Empty);

        channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAME_SETTINGS_MESSAGE, 11);
    }

    private void ApplyGameOptions(string sender, string message)
    {
        if (sender != hostName)
            return;

        string[] parts = message.Split(';');

        int checkBoxIntegerCount = (CheckBoxes.Count / 32) + 1;

        int partIndex = checkBoxIntegerCount + DropDowns.Count;

        if (parts.Length < partIndex + 6)
        {
            AddNotice(("The game host has sent an invalid game options message! " +
                "The game host's game version might be different from yours.").L10N("Client:Main:HostGameOptionInvalid"), NoticeSeverity.Error);
            return;
        }

        string mapOfficial = parts[partIndex];
        bool isMapOfficial = Conversions.BooleanFromString(mapOfficial, true);

        string mapSHA1 = parts[partIndex + 1];

        string gameMode = parts[partIndex + 2];

        int frameSendRate = Conversions.IntFromString(parts[partIndex + 3], FrameSendRate);
        if (frameSendRate != FrameSendRate)
        {
            FrameSendRate = frameSendRate;
            AddNotice(string.Format("The game host has changed FrameSendRate (order lag) to {0}".L10N("Client:Main:HostChangeFrameSendRate"), frameSendRate));
        }

        int maxAhead = Conversions.IntFromString(parts[partIndex + 4], MaxAhead);
        if (maxAhead != MaxAhead)
        {
            MaxAhead = maxAhead;
            AddNotice(string.Format("The game host has changed MaxAhead to {0}".L10N("Client:Main:HostChangeMaxAhead"), maxAhead));
        }

        int protocolVersion = Conversions.IntFromString(parts[partIndex + 5], ProtocolVersion);
        if (protocolVersion != ProtocolVersion)
        {
            ProtocolVersion = protocolVersion;
            AddNotice(string.Format("The game host has changed ProtocolVersion to {0}".L10N("Client:Main:HostChangeProtocolVersion"), protocolVersion));
        }

        string mapName = parts[partIndex + 8];
        GameModeMap currentGameModeMap = GameModeMap;

        lastGameMode = gameMode;
        lastMapSHA1 = mapSHA1;
        lastMapName = mapName;

        GameModeMap = GameModeMaps.FirstOrDefault(gmm => gmm.GameMode.Name == gameMode && gmm.Map.SHA1 == mapSHA1);
        if (GameModeMap == null)
        {
            ChangeMap(null);

            if (!string.IsNullOrEmpty(mapSHA1))
            {
                if (!isMapOfficial)
                    RequestMap(mapSHA1);
                else
                    ShowOfficialMapMissingMessage(mapSHA1);
            }
        }
        else if (GameModeMap != currentGameModeMap)
        {
            ChangeMap(GameModeMap);
        }

        for (int i = 0; i < checkBoxIntegerCount; i++)
        {
            if (parts.Length <= i)
                return;

            bool success = int.TryParse(parts[i], out int checkBoxStatusInt);

            if (!success)
            {
                AddNotice(("Failed to parse check box options sent by game host!" +
                    "The game host's game version might be different from yours.").L10N("Client:Main:HostCheckBoxParseError"), NoticeSeverity.Error);
                return;
            }

            byte[] byteArray2 = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(byteArray2, checkBoxStatusInt);
            bool[] boolArray = Conversions.BytesIntoBoolArray(byteArray2);

            for (int optionIndex = 0; optionIndex < boolArray.Length; optionIndex++)
            {
                int gameOptionIndex = i * 32 + optionIndex;

                if (gameOptionIndex >= CheckBoxes.Count)
                    break;

                GameOptionCheckBox checkBox = CheckBoxes[gameOptionIndex];

                if (checkBox.IsChecked != boolArray[optionIndex])
                {
                    if (boolArray[optionIndex])
                        AddNotice(string.Format("The game host has enabled {0}".L10N("Client:Main:HostEnableOption"), checkBox.Name));
                    else
                        AddNotice(string.Format("The game host has disabled {0}".L10N("Client:Main:HostDisableOption"), checkBox.Name));
                }

                CheckBoxes[gameOptionIndex].IsChecked = boolArray[optionIndex];
            }
        }

        for (int i = checkBoxIntegerCount; i < DropDowns.Count + checkBoxIntegerCount; i++)
        {
            if (parts.Length <= i)
            {
                AddNotice(("The game host has sent an invalid game options message! " +
                "The game host's game version might be different from yours.").L10N("Client:Main:HostGameOptionInvalid"), NoticeSeverity.Error);
                return;
            }

            bool success = int.TryParse(parts[i], out int ddSelectedIndex);

            if (!success)
            {
                AddNotice(("Failed to parse drop down options sent by game host (2)! " +
                    "The game host's game version might be different from yours.").L10N("Client:Main:HostDropDownParseError"), NoticeSeverity.Error);
                return;
            }

            GameOptionDropDown dd = DropDowns[i - checkBoxIntegerCount];

            if (ddSelectedIndex < -1 || ddSelectedIndex >= dd.Items.Count)
                continue;

            if (dd.SelectedIndex != ddSelectedIndex)
            {
                string ddName = dd.Name;
                AddNotice(string.Format("The game host has set {0} to {1}".L10N("Client:Main:HostSetOption"), ddName, dd.Items[ddSelectedIndex]));
            }

            DropDowns[i - checkBoxIntegerCount].SelectedIndex = ddSelectedIndex;
        }

        bool parseSuccess = int.TryParse(parts[partIndex + 6], out int randomSeed);

        if (!parseSuccess)
        {
            AddNotice(("Failed to parse random seed from game options message! " +
                "The game host's game version might be different from yours.").L10N("Client:Main:HostRandomSeedError"), NoticeSeverity.Error);
        }

        bool removeStartingLocations = Convert.ToBoolean(Conversions.IntFromString(parts[partIndex + 7],
            Convert.ToInt32(RemoveStartingLocations)));
        SetRandomStartingLocations(removeStartingLocations);

        RandomSeed = randomSeed;
    }

    // --- Map sharing ---

    private void RequestMap(string mapSHA1)
    {
        if (UserINISettings.Instance.EnableMapSharing)
        {
            AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("Client:Main:MapNotExist"));
            // View will handle showing the download confirmation
        }
        else
        {
            AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("Client:Main:MapNotExist") + " " +
                ("Because you've disabled map sharing, it cannot be transferred. The game host needs " +
                "to change the map or you will be unable to participate in the match.").L10N("Client:Main:MapSharingDisabledNotice"));
            channel.SendCTCPMessage(MAP_SHARING_DISABLED_MESSAGE, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }
    }

    private void ShowOfficialMapMissingMessage(string sha1)
    {
        AddNotice(("The game host has selected an official map that doesn't exist on your installation. " +
            "This could mean that the game host has modified game files, or is running a different game version. " +
            "They need to change the map or you will be unable to participate in the match.").L10N("Client:Main:OfficialMapNotExist"));
        channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + sha1, QueuedMessageType.SYSTEM_MESSAGE, 9);
    }

    public void OnMapDownloadConfirmed()
    {
        Logger.Log("Map sharing confirmed.");
        AddNotice("Attempting to download map.".L10N("Client:Main:DownloadingMap"));
        MapSharer.DownloadMap(lastMapSHA1, localGame, lastMapName);
    }

    protected override void ChangeMap(GameModeMap gameModeMap)
    {
        base.ChangeMap(gameModeMap);
    }

    protected override void HandleMapUpdated(Map updatedMap, string previousSHA1)
    {
        base.HandleMapUpdated(updatedMap, previousSHA1);

        if (IsHost && Map != null && Map.SHA1 == updatedMap.SHA1)
            OnGameOptionChanged();
    }

    // --- Game process ---

    protected override void OnGameProcessExited()
    {
        ResetGameState();
    }

    private void GameStartAborted()
    {
        ResetGameState();
    }

    private void ResetGameState()
    {
        base.OnGameProcessExited();

        channel.SendCTCPMessage("RETURN", QueuedMessageType.SYSTEM_MESSAGE, 20);
        ReturnNotification(ProgramConstants.PLAYERNAME);

        if (IsHost)
        {
            RandomSeed = random.Next();
            OnGameOptionChanged();
            ClearReadyStatuses();
            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
            BroadcastPlayerExtraOptions();
            StartInactiveCheck();

            if (Players.Count < PlayerLimit)
                PerformUnlockGame(true);
        }
    }

    private void NonHostLaunchGame(string sender, string message)
    {
        if (sender != hostName)
            return;

        if (Map == null)
        {
            GameStartAborted();
            return;
        }

        string[] parts = message.Split(';');

        if (parts.Length < 1)
            return;

        UniqueGameID = Conversions.IntFromString(parts[0], -1);
        if (UniqueGameID < 0)
            return;

        var recentPlayers = new List<string>();

        for (int i = 1; i < parts.Length; i += 2)
        {
            if (parts.Length <= i + 1)
                return;

            string pName = parts[i];
            string[] ipAndPort = parts[i + 1].Split(':');

            if (ipAndPort.Length < 2)
                return;

            bool success = int.TryParse(ipAndPort[1], out int port);

            if (!success)
                return;

            if (pName == ProgramConstants.PLAYERNAME)
            {
                var matchedTunnel = tunnelHandler.Tunnels
                    .FirstOrDefault(t =>
                        string.Equals(t.Address, ipAndPort[0], StringComparison.OrdinalIgnoreCase));

                if (matchedTunnel != null)
                {
                    tunnelHandler.CurrentTunnel = matchedTunnel;
                }
                else
                {
                    AddNotice("Failed to match the tunnel address provided by the host to any available tunnel. The game cannot be started.".L10N("Client:Main:TunnelErrorMessage"), NoticeSeverity.Error);
                    Logger.Log("Failed to match tunnel address: " + ipAndPort[0]);
                    return;
                }
            }

            PlayerInfo pInfo = Players.Find(p => p.Name == pName);

            if (pInfo == null)
                return;

            pInfo.Port = port;
            recentPlayers.Add(pName);
        }
        cncnetUserData.AddRecentPlayers(recentPlayers, _gameRoomNameValue);

        StartGame();
    }

    protected override void StartGame()
    {
        AddNotice("Starting game...".L10N("Client:Main:StartingGame"));

        FileHashCalculator fhc = new FileHashCalculator();
        fhc.CalculateHashes();

        if (gameFilesHash != fhc.GetCompleteHash())
        {
            Logger.Log("Game files modified during client session!");
            channel.SendCTCPMessage(CHEAT_DETECTED_MESSAGE, QueuedMessageType.INSTANT_MESSAGE, 0);
            HandleCheatDetectedMessage(ProgramConstants.PLAYERNAME);
        }

        StopInactiveCheck();
        channel.SendCTCPMessage("STRTD", QueuedMessageType.SYSTEM_MESSAGE, 20);

        base.StartGame();
    }

    protected override void WriteSpawnIniAdditions(IniFile iniFile)
    {
        base.WriteSpawnIniAdditions(iniFile);

        iniFile.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
        iniFile.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);

        iniFile.SetIntValue("Settings", "GameID", UniqueGameID);
        iniFile.SetBooleanValue("Settings", "Host", IsHost);

        PlayerInfo localPlayer = FindLocalPlayer();

        if (localPlayer == null)
            return;

        iniFile.SetIntValue("Settings", "Port", localPlayer.Port);
    }

    protected override void SendChatMessage(string message) => channel.SendChatMessage(message, chatColor);

    public void ChangeChatColor(IRCColor newChatColor) => chatColor = newChatColor;

    // --- Notifications ---

    private void HandleNotification(string sender, Action handler)
    {
        if (sender != hostName)
            return;

        handler();
    }

    private void HandleIntNotification(string sender, int parameter, Action<int> handler)
    {
        if (sender != hostName)
            return;

        handler(parameter);
    }

    protected override void GetReadyNotification()
    {
        base.GetReadyNotification();

        if (IsHost)
            channel.SendCTCPMessage("GETREADY", QueuedMessageType.GAME_GET_READY_MESSAGE, 0);
    }

    protected override void AISpectatorsNotification()
    {
        base.AISpectatorsNotification();

        if (IsHost)
            channel.SendCTCPMessage("AISPECS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override void InsufficientPlayersNotification()
    {
        base.InsufficientPlayersNotification();

        if (IsHost)
            channel.SendCTCPMessage("INSFSPLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override void TooManyPlayersNotification()
    {
        base.TooManyPlayersNotification();

        if (IsHost)
            channel.SendCTCPMessage("TMPLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override void SharedColorsNotification()
    {
        base.SharedColorsNotification();

        if (IsHost)
            channel.SendCTCPMessage("CLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override void SharedStartingLocationNotification()
    {
        base.SharedStartingLocationNotification();

        if (IsHost)
            channel.SendCTCPMessage("SLOC", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override void LockGameNotification()
    {
        base.LockGameNotification();

        if (IsHost)
            channel.SendCTCPMessage("LCKGME", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override void NotVerifiedNotification(int playerIndex)
    {
        base.NotVerifiedNotification(playerIndex);

        if (IsHost)
            channel.SendCTCPMessage("NVRFY " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override void StillInGameNotification(int playerIndex)
    {
        base.StillInGameNotification(playerIndex);

        if (IsHost)
            channel.SendCTCPMessage("INGM " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    private void GameStartedNotification(string sender)
    {
        PlayerInfo pInfo = Players.Find(p => p.Name == sender);

        if (pInfo != null)
            pInfo.IsInGame = true;

        CopyPlayerDataToUI();
    }

    private void ReturnNotification(string sender)
    {
        AddNotice(string.Format("{0} has returned from the game.".L10N("Client:Main:PlayerReturned"), sender));

        PlayerInfo pInfo = Players.Find(p => p.Name == sender);

        if (pInfo != null)
            pInfo.IsInGame = false;

        CopyPlayerDataToUI();
    }

    private void HandleTunnelPing(string sender, int ping)
    {
        PlayerInfo pInfo = Players.Find(p => p.Name.Equals(sender));
        if (pInfo != null)
        {
            pInfo.Ping = ping;
            CopyPlayerDataToUI();
        }
    }

    private void FileHashNotification(string sender, string filesHash)
    {
        if (!IsHost)
            return;

        PlayerInfo pInfo = Players.Find(p => p.Name == sender);

        if (pInfo != null)
            pInfo.HashReceived = true;
        CopyPlayerDataToUI();

        if (filesHash != gameFilesHash)
        {
            channel.SendCTCPMessage("MM " + sender, QueuedMessageType.GAME_CHEATER_MESSAGE, 10);
            CheaterNotification(ProgramConstants.PLAYERNAME, sender);
        }
    }

    private void CheaterNotification(string sender, string cheaterName)
    {
        if (sender != hostName)
            return;

        AddNotice(string.Format("Player {0} has different files compared to the game host. Either {0} or the game host could be cheating.".L10N("Client:Main:DifferentFileCheating"), cheaterName), NoticeSeverity.Error);
    }

    private void HandleCheatDetectedMessage(string sender) =>
        AddNotice(string.Format("{0} has modified game files during the client session. They are likely attempting to cheat!".L10N("Client:Main:PlayerModifyFileCheat"), sender), NoticeSeverity.Error);

    protected override void BroadcastDiceRoll(int dieSides, int[] results)
    {
        string resultString = string.Join(",", results);
        channel.SendCTCPMessage($"{DICE_ROLL_MESSAGE} {dieSides},{resultString}", QueuedMessageType.CHAT_MESSAGE, 0);
        PrintDiceRollResult(ProgramConstants.PLAYERNAME, dieSides, results);
    }

    // --- Lock/Unlock ---

    protected override void PerformLockGame()
    {
        AddNotice("You've locked the game room.".L10N("Client:Main:RoomLockedByYou"));

        connectionManager.SendCustomMessage(new QueuedMessage(
            string.Format("MODE {0} +i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));

        Locked = true;
        LockGameButtonText = "Unlock Game".L10N("Client:Main:UnlockGame");
        AccelerateGameBroadcasting();
    }

    protected override void PerformUnlockGame(bool announce)
    {
        if (Players.Count >= PlayerLimit)
        {
            AddNotice(string.Format(
                "Cannot unlock game; the player limit ({0}) has been reached.".L10N("Client:Main:RoomCantUnlockAsLimit"), PlayerLimit));
            return;
        }

        connectionManager.SendCustomMessage(new QueuedMessage(
            string.Format("MODE {0} -i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));

        Locked = false;
        if (announce)
            AddNotice("The game room has been unlocked.".L10N("Client:Main:GameRoomUnlocked"));
        LockGameButtonText = "Lock Game".L10N("Client:Main:LockGame");
        AccelerateGameBroadcasting();
    }

    // --- Game broadcast timer ---

    private void GameBroadcastTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (IsHost && !closed)
            BroadcastGame();
    }

    private void AccelerateGameBroadcasting()
    {
        if (gameBroadcastTimer != null)
        {
            gameBroadcastTimer.Stop();
            gameBroadcastTimer.Interval = GAME_BROADCAST_ACCELERATION * 1000;
            gameBroadcastTimer.Start();
        }
    }

    // --- Kick/Ban ---

    protected override void KickPlayer(int playerIndex)
    {
        if (playerIndex >= Players.Count)
            return;

        var pInfo = Players[playerIndex];

        AddNotice(string.Format("Kicking {0} from the game...".L10N("Client:Main:KickPlayer"), pInfo.Name));
        channel.SendKickMessage(pInfo.Name, 8);
    }

    protected override void BanPlayer(int playerIndex)
    {
        if (playerIndex >= Players.Count)
            return;

        var pInfo = Players[playerIndex];

        var user = connectionManager.UserList.Find(u => u.Name == pInfo.Name);

        if (user != null)
        {
            AddNotice(string.Format("Banning and kicking {0} from the game...".L10N("Client:Main:BanAndKickPlayer"), pInfo.Name));
            channel.SendBanMessage(user.Hostname, 8);
            channel.SendKickMessage(user.Name, 8);
        }
    }

    // --- Tunnel server change ---

    private void HandleTunnelServerChangeMessage(string sender, string tunnelAddressAndPort)
    {
        if (sender != hostName)
            return;

        string[] split = tunnelAddressAndPort.Split(':');
        string tunnelAddress = split[0];
        int tunnelPort = int.Parse(split[1]);

        CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort);
        if (tunnel == null)
        {
            tunnelErrorModeInternal = true;
            TunnelErrorMode = true;
            AddNotice(("The game host has selected an invalid tunnel server! " +
                "The game host needs to change the server or you will be unable " +
                "to participate in the match.").L10N("Client:Main:HostInvalidTunnel"),
                NoticeSeverity.Warning);
            UpdateLaunchGameButtonStatus();
            return;
        }

        tunnelErrorModeInternal = false;
        TunnelErrorMode = false;
        HandleTunnelServerChange(tunnel);
        UpdateLaunchGameButtonStatus();
    }

    private void HandleTunnelServerChange(CnCNetTunnel tunnel)
    {
        tunnelHandler.CurrentTunnel = tunnel;
        SelectedTunnelName = tunnel.Name;
        AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("Client:Main:HostChangeTunnel"), tunnel.Name));

        foreach (PlayerInfo pInfo in Players)
            pInfo.Ping = -1;

        CopyPlayerDataToUI();
        UpdatePing();
    }

    // --- Launch button ---

    protected override bool UpdateLaunchGameButtonStatus()
    {
        bool canLaunch = base.UpdateLaunchGameButtonStatus() && !tunnelErrorModeInternal;
        CanLaunchGame = canLaunch;
        return canLaunch;
    }

    // --- Map sharing event handlers ---

    private void MapSharer_MapDownloadFailed(object sender, SHA1EventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action(() => MapSharer_HandleMapDownloadFailed(e)));
    }

    private void MapSharer_HandleMapDownloadFailed(SHA1EventArgs e)
    {
        if (hostUploadedMaps.Contains(e.SHA1))
        {
            AddNotice("Download of the custom map failed. The host needs to change the map or you will be unable to participate in this match.".L10N("Client:Main:DownloadCustomMapFailed"));

            channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            return;
        }
        else if (chatCommandDownloadedMaps.Contains(e.SHA1))
        {
            AddNotice("Downloading map via chat command has failed. Check the map ID and try again.".L10N("Client:Main:DownloadMapCommandFailedGeneric"));
            return;
        }

        AddNotice("Requesting the game host to upload the map to the CnCNet map database.".L10N("Client:Main:RequestHostUploadMapToDB"));

        channel.SendCTCPMessage(MAP_SHARING_UPLOAD_REQUEST + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
    }

    private void MapSharer_MapDownloadComplete(object sender, SHA1EventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action(() =>
        {
            string mapFileName = MapSharer.GetMapFileName(e.SHA1, e.MapName);
            Logger.Log("Map " + mapFileName + " downloaded successfully.");
        }));
    }

    private void MapLoader_MapChanged(object sender, MapChangedEventArgs e)
    {
        if (e.ChangeType != MapChangeType.Added)
            return;

        bool isFromChatCommand = chatCommandDownloadedMaps.Contains(e.Map.SHA1);
        bool isFromHostSharing = lastMapSHA1 == e.Map.SHA1 && !isFromChatCommand;

        if (!isFromChatCommand && !isFromHostSharing)
            return;

        AddNotice($"Map {e.Map.Name} loaded successfully.");

        GameModeMap = GameModeMaps.FirstOrDefault(gmm => gmm.Map.SHA1 == e.Map.SHA1);
        ChangeMap(GameModeMap);

        if (isFromChatCommand)
            chatCommandDownloadedMaps.Remove(e.Map.SHA1);
    }

    protected override void HandleMapAdded(Map addedMap)
    {
        bool isFromChatCommand = chatCommandDownloadedMaps.Contains(addedMap.SHA1);
        bool isFromHostSharing = lastMapSHA1 == addedMap.SHA1 && !isFromChatCommand;

        if (isFromChatCommand || isFromHostSharing)
        {
            AddNotice($"Map {addedMap.Name} loaded successfully.");

            RefreshGameModeFilter();

            GameModeMap gameModeMap = GameModeMaps.FirstOrDefault(gmm => gmm.Map.SHA1 == addedMap.SHA1);

            if (gameModeMap != null)
                ChangeMap(gameModeMap);

            if (isFromChatCommand)
                chatCommandDownloadedMaps.Remove(addedMap.SHA1);
        }
        else
        {
            base.HandleMapAdded(addedMap);
        }
    }

    private void MapSharer_MapUploadFailed(object sender, MapEventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action(() => MapSharer_HandleMapUploadFailed(e)));
    }

    private void MapSharer_HandleMapUploadFailed(MapEventArgs e)
    {
        Map map = e.Map;

        AddNotice(string.Format("Uploading map {0} to the CnCNet map database failed.".L10N("Client:Main:UpdateMapToDBFailed"), map.Name));
        if (map == Map)
        {
            AddNotice("You need to change the map or some players won't be able to participate in this match.".L10N("Client:Main:YouMustReplaceMap"));
            channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }
    }

    private void MapSharer_MapUploadComplete(object sender, MapEventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action(() => MapSharer_HandleMapUploadComplete(e)));
    }

    private void MapSharer_HandleMapUploadComplete(MapEventArgs e)
    {
        AddNotice(string.Format("Uploading map {0} to the CnCNet map database complete.".L10N("Client:Main:UpdateMapToDBSuccess"), e.Map.Name));
        if (e.Map == Map)
        {
            channel.SendCTCPMessage(MAP_SHARING_DOWNLOAD_REQUEST + " " + Map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }
    }

    private void HandleMapUploadRequest(string sender, string mapSHA1)
    {
        if (MapSharer.IsMapUploaded(mapSHA1))
        {
            Logger.Log("HandleMapUploadRequest: Map " + mapSHA1 + " is already uploaded, sending download notification.");

            if (Map != null && Map.SHA1 == mapSHA1)
                channel.SendCTCPMessage(MAP_SHARING_DOWNLOAD_REQUEST + " " + mapSHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);

            return;
        }

        Map map = null;

        foreach (GameMode gm in GameModeMaps.GameModes)
        {
            map = gm.Maps.Find(m => m.SHA1 == mapSHA1);

            if (map != null)
                break;
        }

        if (map == null)
        {
            Logger.Log("Unknown map upload request from " + sender + ": " + mapSHA1);
            return;
        }

        if (map.Official)
        {
            Logger.Log("HandleMapUploadRequest: Map is official, so skip request");

            AddNotice(string.Format(("{0} doesn't have the map '{1}' on their local installation. " +
                "The map needs to be changed or {0} is unable to participate in the match.").L10N("Client:Main:PlayerMissingMap"),
                sender, map.Name));

            return;
        }

        if (!IsHost)
            return;

        AddNotice(string.Format(("{0} doesn't have the map '{1}' on their local installation. " +
            "Attempting to upload the map to the CnCNet map database.").L10N("Client:Main:UpdateMapToDBPrompt"),
            sender, map.Name));

        MapSharer.UploadMap(map, localGame);
    }

    private void HandleMapTransferFailMessage(string sender, string sha1)
    {
        if (sender == hostName)
        {
            AddNotice("The game host failed to upload the map to the CnCNet map database.".L10N("Client:Main:HostUpdateMapToDBFailed"));

            hostUploadedMaps.Add(sha1);

            if (lastMapSHA1 == sha1 && Map == null)
                AddNotice("The game host needs to change the map or you won't be able to participate in this match.".L10N("Client:Main:HostMustChangeMap"));

            return;
        }

        if (lastMapSHA1 == sha1)
        {
            if (!IsHost)
                AddNotice(string.Format("{0} has failed to download the map from the CnCNet map database.".L10N("Client:Main:PlayerDownloadMapFailed") + " " +
                    "The host needs to change the map or {0} won't be able to participate in this match.".L10N("Client:Main:HostNeedChangeMapForPlayer"), sender));
            else
                AddNotice(string.Format("{0} has failed to download the map from the CnCNet map database.".L10N("Client:Main:PlayerDownloadMapFailed") + " " +
                    "You need to change the map or {0} won't be able to participate in this match.".L10N("Client:Main:YouNeedChangeMapForPlayer"), sender));
        }
    }

    private void HandleMapDownloadRequest(string sender, string sha1)
    {
        if (sender != hostName)
            return;

        hostUploadedMaps.Add(sha1);

        if (lastMapSHA1 == sha1 && Map == null)
        {
            Logger.Log("The game host has uploaded the map into the database. Re-attempting download...");
            MapSharer.DownloadMap(sha1, localGame, lastMapName);
        }
    }

    private void HandleMapSharingBlockedMessage(string sender)
    {
        AddNotice(string.Format(("The selected map doesn't exist on {0}'s installation, and they " +
            "have map sharing disabled in settings. The game host needs to change to a non-custom map or " +
            "they will be unable to participate in this match.").L10N("Client:Main:PlayerMissingMapDisabledSharing"), sender));
    }

    private void DownloadMapByIdCommand(string parameters)
    {
        string sha1;
        string mapName;
        string message;

        parameters = parameters.Trim();
        int firstSpaceIndex = parameters.IndexOf(' ');

        if (firstSpaceIndex == -1)
        {
            sha1 = parameters;
            mapName = "user_chat_command_download";
        }
        else
        {
            sha1 = parameters.Substring(0, firstSpaceIndex);
            mapName = parameters.Substring(firstSpaceIndex + 1);
            mapName = mapName.Trim();
        }

        sha1 = sha1.Replace("?", "");

        GameModeMap loadedMap = GameModeMaps.FirstOrDefault(gmm => gmm.Map.SHA1 == sha1);

        if (loadedMap != null)
        {
            message = String.Format(
                "The map for ID \"{0}\" is already loaded from \"{1}.{2}\", delete the existing file before trying again.".L10N("Client:Main:DownloadMapCommandSha1AlreadyExists"),
                sha1,
                loadedMap.Map.BaseFilePath,
                ClientConfiguration.Instance.MapFileExtension);
            AddNotice(message, NoticeSeverity.Warning);
            Logger.Log(message);
            return;
        }

        char replaceUnsafeCharactersWith = '-';
        HashSet<char> invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
        string safeMapName = new String(mapName.Select(c => invalidChars.Contains(c) ? replaceUnsafeCharactersWith : c).ToArray());

        chatCommandDownloadedMaps.Add(sha1);

        message = String.Format("Attempting to download map via chat command: sha1={0}, mapName={1}".L10N("Client:Main:DownloadMapCommandStartingDownload"), sha1, mapName);
        Logger.Log(message);
        AddNotice(message);

        MapSharer.DownloadMap(sha1, localGame, safeMapName);
    }

    // --- Game broadcasting ---

    private void BroadcastGame()
    {
        Channel broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

        if (broadcastChannel == null)
            return;

        if (ProgramConstants.IsInGame && broadcastChannel.Users.Count > 500)
            return;

        StringBuilder sb = new StringBuilder("GAME ");
        sb.Append(ProgramConstants.CNCNET_PROTOCOL_REVISION);
        sb.Append(";");
        sb.Append(ProgramConstants.GAME_VERSION);
        sb.Append(";");
        sb.Append(PlayerLimit);
        sb.Append(";");
        sb.Append(channel.ChannelName);
        sb.Append(";");
        sb.Append(_gameRoomNameValue);
        sb.Append(";");
        if (Locked)
            sb.Append("1");
        else
            sb.Append("0");
        sb.Append(Convert.ToInt32(IsCustomPassword));
        sb.Append(Convert.ToInt32(closed));
        sb.Append("0");
        sb.Append("0");
        sb.Append(";");
        foreach (PlayerInfo pInfo in Players)
        {
            sb.Append(pInfo.Name);
            sb.Append(",");
        }

        sb.Remove(sb.Length - 1, 1);
        sb.Append(";");
        sb.Append(Map?.UntranslatedName ?? string.Empty);
        sb.Append(";");
        sb.Append(GameMode?.UntranslatedUIName ?? string.Empty);
        sb.Append(";");
        sb.Append(tunnelHandler.CurrentTunnel.Address + ":" + tunnelHandler.CurrentTunnel.Port);
        sb.Append(";");
        sb.Append(0);
        sb.Append(";");
        sb.Append(SkillLevel);
        sb.Append(";");
        sb.Append(Map?.SHA1);

        List<IGameSessionSetting> broadcastableSettings = GetBroadcastableSettings();

        List<int> gameOptionValues = new();

        int checkboxCount = CheckBoxes.Count(cb => cb.Setting.BroadcastToLobby);
        if (checkboxCount > 0)
        {
            bool[] checkboxValues = new bool[checkboxCount];
            for (int i = 0; i < checkboxCount; i++)
                checkboxValues[i] = CheckBoxes.Where(cb => cb.Setting.BroadcastToLobby).ElementAt(i).IsChecked;

            List<byte> byteList = Conversions.BoolArrayIntoBytes(checkboxValues).ToList();

            while (byteList.Count % 4 != 0)
                byteList.Add(0);

            byte[] byteArray = byteList.ToArray();

            for (int i = 0; i < byteArray.Length / 4; i++)
                gameOptionValues.Add(BinaryPrimitives.ReadInt32LittleEndian(byteArray.AsSpan(i * 4)));
        }

        int dropdownCount = DropDowns.Count(dd => dd.Setting.BroadcastToLobby);
        if (dropdownCount > 0)
            gameOptionValues.AddRange(DropDowns.Where(dd => dd.Setting.BroadcastToLobby).Select(dd => dd.SelectedIndex));

        sb.Append(";");
        if (gameOptionValues.Count > 0)
            sb.Append(string.Join(",", gameOptionValues));

        broadcastChannel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 20);
    }

    // --- Broadcastable settings ---

    public List<IGameSessionSetting> GetBroadcastableSettings()
    {
        var settings = new List<IGameSessionSetting>();
        foreach (var cb in CheckBoxes)
        {
            if (cb.Setting.BroadcastToLobby)
                settings.Add(cb.Setting);
        }
        foreach (var dd in DropDowns)
        {
            if (dd.Setting.BroadcastToLobby)
                settings.Add(dd.Setting);
        }
        return settings;
    }

    public int GetBroadcastableCheckboxCount() => CheckBoxes.Count(cb => cb.Setting.BroadcastToLobby);
    public int GetBroadcastableDropdownCount() => DropDowns.Count(dd => dd.Setting.BroadcastToLobby);

    // --- Abstract implementations ---

    protected override bool IsMultiplayer => true;

    protected override void UpdateDiscordPresence(bool resetTimer = false)
    {
        DiscordHandler?.UpdatePresence(
            Map?.UntranslatedName, GameMode?.UntranslatedUIName, "Multiplayer",
            ProgramConstants.IsInGame ? "In Game" : "In Lobby", Players.Count, PlayerLimit, "",
            channel?.UIName, IsHost, IsCustomPassword, Locked, resetTimer);
    }

    public override string GetSwitchName() => "Game Lobby".L10N("Client:Main:GameLobby");
}
