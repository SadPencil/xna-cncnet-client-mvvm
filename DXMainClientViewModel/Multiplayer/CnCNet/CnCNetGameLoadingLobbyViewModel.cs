using System;
using System.Collections.Generic;
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

namespace DXMainClientViewModel.Multiplayer.CnCNet;

/// <summary>
/// ViewModel for loading saved CnCNet multiplayer games.
/// Contains all business logic from CnCNetGameLoadingLobby.cs except XNA UI rendering.
/// </summary>

public partial class CnCNetGameLoadingLobbyViewModel : GameLoadingLobbyBaseViewModel, ICnCNetGameLoadingLobbyViewModel
{
    private const double GAME_BROADCAST_INTERVAL = 20.0;
    private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;

    private const string NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND = "NPRSNT";
    private const string GET_READY_CTCP_COMMAND = "GTRDY";
    private const string FILE_HASH_CTCP_COMMAND = "FHSH";
    private const string INVALID_FILE_HASH_CTCP_COMMAND = "IHSH";
    private const string TUNNEL_PING_CTCP_COMMAND = "TNLPNG";
    private const string OPTIONS_CTCP_COMMAND = "OP";
    private const string INVALID_SAVED_GAME_INDEX_CTCP_COMMAND = "ISGI";
    private const string START_GAME_CTCP_COMMAND = "START";
    private const string PLAYER_READY_CTCP_COMMAND = "READY";
    private const string CHANGE_TUNNEL_SERVER_MESSAGE = "CHTNL";

    private readonly CnCNetManager connectionManager;
    private readonly CnCNetUserData cncnetUserData;
    private readonly TunnelHandler tunnelHandler;
    private readonly GameCollection gameCollection;
    private readonly string localGame;

    private readonly CommandHandlerBase[] ctcpCommandHandlers;

    private Channel? channel;
    private string hostName = string.Empty;
    private string gameFilesHash = string.Empty;
    private bool started;
    private IRCColor? chatColor;
    private string untranslatedMapName = string.Empty;
    private string untranslatedGameMode = string.Empty;

    private Timer? gameBroadcastTimer;

    // --- Observable state ---

    [ObservableProperty]
    private string _channelName = string.Empty;

    [ObservableProperty]
    private string _selectedTunnelName = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isChangeTunnelVisible;

    [ObservableProperty]
    private int _chatColorIndex;

    // --- Events ---

    public event EventHandler? TunnelSelectionRequested;
    public event EventHandler? PrimarySwitchRequested;

    // --- Constructor ---

    public CnCNetGameLoadingLobbyViewModel(
        DiscordHandler discordHandler,
        IGameProcessService gameProcessService,
        IUIThreadMarshaller uiThreadMarshaller,
        CnCNetManager connectionManager,
        CnCNetUserData cncnetUserData,
        TunnelHandler tunnelHandler,
        GameCollection gameCollection)
        : base(discordHandler, gameProcessService, uiThreadMarshaller)
    {
        this.connectionManager = connectionManager;
        this.cncnetUserData = cncnetUserData;
        this.tunnelHandler = tunnelHandler;
        this.gameCollection = gameCollection;
        this.localGame = ClientConfiguration.Instance.LocalGame;

        ctcpCommandHandlers = new CommandHandlerBase[]
        {
            new NoParamCommandHandler(NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND, HandleNotAllPresentNotification),
            new NoParamCommandHandler(GET_READY_CTCP_COMMAND, HandleGetReadyNotification),
            new StringCommandHandler(FILE_HASH_CTCP_COMMAND, HandleFileHashCommand),
            new StringCommandHandler(INVALID_FILE_HASH_CTCP_COMMAND, HandleCheaterNotification),
            new IntCommandHandler(TUNNEL_PING_CTCP_COMMAND, HandleTunnelPing),
            new StringCommandHandler(OPTIONS_CTCP_COMMAND, HandleOptionsMessage),
            new NoParamCommandHandler(INVALID_SAVED_GAME_INDEX_CTCP_COMMAND, HandleInvalidSaveIndexCommand),
            new StringCommandHandler(START_GAME_CTCP_COMMAND, HandleStartGameCommand),
            new IntCommandHandler(PLAYER_READY_CTCP_COMMAND, HandlePlayerReadyRequest),
            new StringCommandHandler(CHANGE_TUNNEL_SERVER_MESSAGE, HandleTunnelServerChangeMessage)
        };

        connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
        connectionManager.Disconnected += ConnectionManager_Disconnected;
    }

    // --- Commands ---

    [RelayCommand]
    private void ChangeTunnel()
    {
        TunnelSelectionRequested?.Invoke(this, EventArgs.Empty);
    }

    // --- Lifecycle ---

    public override void Initialize()
    {
        // No-op: subscriptions happen in SetUp
    }

    /// <summary>
    /// Sets up events and information before joining the channel.
    /// </summary>
    public void SetUp(bool isHost, ICnCNetTunnel tunnel, IChannel channel, string hostName)
    {
        this.channel = (Channel)channel;
        this.hostName = hostName;

        this.channel.MessageAdded += Channel_MessageAdded;
        this.channel.UserAdded += Channel_UserAdded;
        this.channel.UserLeft += Channel_UserLeft;
        this.channel.UserQuitIRC += Channel_UserQuitIRC;
        this.channel.CTCPReceived += Channel_CTCPReceived;

        tunnelHandler.CurrentTunnel = (CnCNetTunnel)tunnel;
        tunnelHandler.CurrentTunnelPinged += TunnelHandler_CurrentTunnelPinged;

        started = false;

        Refresh(isHost);
    }

    public override void Refresh(bool isHost)
    {
        base.Refresh(isHost);

        IsChangeTunnelVisible = isHost;
        SelectedTunnelName = tunnelHandler.CurrentTunnel?.Name ?? string.Empty;

        // Store untranslated names for broadcasting and Discord
        untranslatedMapName = MapName;
        untranslatedGameMode = GameMode;

        if (isHost)
        {
            StartGameBroadcastTimer();
        }
    }

    /// <summary>
    /// Called when the local user has joined the game channel.
    /// </summary>
    public void OnJoined()
    {
        FileHashCalculator fhc = new FileHashCalculator();
        fhc.CalculateHashes();

        if (IsHost)
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} +klnNs {1} {2}", channel!.ChannelName,
                channel.Password, SGPlayers.Count),
                QueuedMessageType.SYSTEM_MESSAGE, 50));

            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("TOPIC {0} :{1}", channel.ChannelName,
                ProgramConstants.CNCNET_PROTOCOL_REVISION + ";" + localGame.ToLower()),
                QueuedMessageType.SYSTEM_MESSAGE, 50));

            gameFilesHash = fhc.GetCompleteHash();

            StartGameBroadcastTimer();
        }
        else
        {
            channel!.SendCTCPMessage(FILE_HASH_CTCP_COMMAND + " " + fhc.GetCompleteHash(), QueuedMessageType.SYSTEM_MESSAGE, 10);

            channel.SendCTCPMessage(TUNNEL_PING_CTCP_COMMAND + " " + tunnelHandler.CurrentTunnel!.PingInMs, QueuedMessageType.SYSTEM_MESSAGE, 10);

            if (tunnelHandler.CurrentTunnel.PingInMs < 0)
                AddNotice(string.Format("{0} - unknown ping to tunnel server.".L10N("Client:Main:PlayerUnknownPing"), ProgramConstants.PLAYERNAME));
            else
                AddNotice(string.Format("{0} - ping to tunnel server: {1} ms".L10N("Client:Main:PlayerPing"), ProgramConstants.PLAYERNAME, tunnelHandler.CurrentTunnel.PingInMs));
        }

        PrimarySwitchRequested?.Invoke(this, EventArgs.Empty);
        UpdateDiscordPresence(true);
    }

    /// <summary>
    /// Clears event subscriptions and leaves the channel.
    /// </summary>
    public void Clear()
    {
        StopGameBroadcastTimer();

        if (channel != null)
        {
            channel.Leave();

            channel.MessageAdded -= Channel_MessageAdded;
            channel.UserAdded -= Channel_UserAdded;
            channel.UserLeft -= Channel_UserLeft;
            channel.UserQuitIRC -= Channel_UserQuitIRC;
            channel.CTCPReceived -= Channel_CTCPReceived;

            connectionManager.RemoveChannel(channel);
        }

        if (IsEnabled)
        {
            IsEnabled = false;
            base.LeaveGame();
        }

        tunnelHandler.CurrentTunnel = null;
        tunnelHandler.CurrentTunnelPinged -= TunnelHandler_CurrentTunnelPinged;
    }

    public void ChangeChatColor(IIRCColor color)
    {
        chatColor = (IRCColor)color;
        ChatColorIndex = color.IrcColorId;
    }

    protected override void LeaveGame() => Clear();

    /// <summary>
    /// Called by View when user selects a tunnel from the tunnel selection window.
    /// </summary>
    public void OnTunnelSelected(ICnCNetTunnel tunnel)
    {
        channel?.SendCTCPMessage($"{CHANGE_TUNNEL_SERVER_MESSAGE} {tunnel.Address}:{tunnel.Port}",
            QueuedMessageType.SYSTEM_MESSAGE, 10);
        HandleTunnelServerChange((CnCNetTunnel)tunnel);
    }

    // --- Channel event handlers ---

    private void Channel_UserAdded(object? sender, ChannelUserEventArgs e)
    {
        PlayerInfo pInfo = new PlayerInfo();
        pInfo.Name = e.User.IRCUser.Name;

        Players.Add(pInfo);

        RaiseJoinSoundRequested();

        BroadcastOptions();
        CopyPlayerDataToUI();
        UpdateDiscordPresence();
    }

    private void Channel_UserLeft(object? sender, UserNameEventArgs e)
    {
        RemovePlayer(e.UserName);
        UpdateDiscordPresence();
    }

    private void Channel_UserQuitIRC(object? sender, UserNameEventArgs e)
    {
        RemovePlayer(e.UserName);
        UpdateDiscordPresence();
    }

    private void RemovePlayer(string playerName)
    {
        int index = Players.FindIndex(p => p.Name == playerName);

        if (index == -1)
            return;

        RaiseLeaveSoundRequested();

        Players.RemoveAt(index);

        CopyPlayerDataToUI();

        if (!IsHost && playerName == hostName && !ProgramConstants.IsInGame)
        {
            AddChatMessage("The game host left the game!".L10N("Client:Main:HostLeft"));
            Clear();
        }
    }

    private void Channel_MessageAdded(object? sender, IRCMessageEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Message.SenderIdent) &&
            cncnetUserData.IsIgnored(e.Message.SenderIdent) &&
            !e.Message.SenderIsAdmin)
        {
            AddChatMessageWithoutSound(string.Format("Message blocked from - {0}".L10N("Client:Main:PMBlockedFrom"), e.Message.SenderName));
        }
        else
        {
            AddChatMessage(e.Message.ToString());
        }
    }

    private void Channel_CTCPReceived(object? sender, ChannelCTCPEventArgs e)
    {
        foreach (CommandHandlerBase cmdHandler in ctcpCommandHandlers)
        {
            if (cmdHandler.Handle(e.UserName, e.Message))
                return;
        }

        Logger.Log("Unhandled CTCP command: " + e.Message + " from " + e.UserName);
    }

    private void ConnectionManager_Disconnected(object? sender, EventArgs e) => Clear();

    private void ConnectionManager_ConnectionLost(object? sender, ConnectionLostEventArgs e) => Clear();

    private void TunnelHandler_CurrentTunnelPinged(object? sender, EventArgs e)
    {
        // TODO Rampastring pls, review and merge that XNAIndicator PR already
    }

    // --- Abstract member implementations ---

    protected override void GetReadyNotification()
    {
        base.GetReadyNotification();
        PrimarySwitchRequested?.Invoke(this, EventArgs.Empty);
        if (IsHost)
            channel?.SendCTCPMessage(GET_READY_CTCP_COMMAND, QueuedMessageType.GAME_GET_READY_MESSAGE, 0);
    }

    protected override void NotAllPresentNotification()
    {
        base.NotAllPresentNotification();
        if (IsHost)
            channel?.SendCTCPMessage(NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override void AddNotice(string message)
    {
        channel?.AddMessage(new ChatMessage(255, 255, 255, message));
    }

    protected override void BroadcastOptions()
    {
        if (!IsHost)
            return;

        Players[0].Ready = true;

        StringBuilder message = new StringBuilder(OPTIONS_CTCP_COMMAND + " ");
        message.Append(SelectedSavedGameIndex);
        message.Append(";");
        foreach (PlayerInfo pInfo in Players)
        {
            message.Append(pInfo.Name);
            message.Append(":");
            message.Append(Convert.ToInt32(pInfo.Ready));
            message.Append(";");
        }
        message.Remove(message.Length - 1, 1);

        channel?.SendCTCPMessage(message.ToString(), QueuedMessageType.GAME_SETTINGS_MESSAGE, 10);
    }

    protected override void SendChatMessageToNetwork(string message)
    {
        RaiseMessageSoundRequested();
        channel?.SendChatMessage(message, chatColor!);
    }

    protected override void RequestReadyStatus() =>
        channel?.SendCTCPMessage(PLAYER_READY_CTCP_COMMAND + " 1", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 10);

    protected override void HostStartGame()
    {
        AddNotice("Contacting tunnel server...".L10N("Client:Main:ConnectingTunnel"));
        List<int> playerPorts = tunnelHandler.CurrentTunnel!.GetPlayerPortInfo(SGPlayers.Count);

        if (playerPorts.Count < Players.Count)
        {
            TunnelSelectionRequested?.Invoke(this, EventArgs.Empty);
            AddNotice(("An error occured while contacting the specified CnCNet " +
                "tunnel server. Please try using a different tunnel server").L10N("Client:Main:ConnectTunnelError2"));
            return;
        }

        StringBuilder sb = new StringBuilder(START_GAME_CTCP_COMMAND + " ");
        for (int pId = 0; pId < Players.Count; pId++)
        {
            Players[pId].Port = playerPorts[pId];
            sb.Append(Players[pId].Name);
            sb.Append(";");
            sb.Append("0.0.0.0:");
            sb.Append(playerPorts[pId]);
            sb.Append(";");
        }
        sb.Remove(sb.Length - 1, 1);
        channel?.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 9);

        AddNotice("Starting game...".L10N("Client:Main:StartingGame"));

        started = true;

        PerformLoadGame();
    }

    protected override void WriteSpawnIniAdditions(IniFile spawnIni)
    {
        spawnIni.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel!.Address);
        spawnIni.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);
    }

    protected override void HandleGameProcessExited()
    {
        base.HandleGameProcessExited();
        Clear();
    }

    protected override void UpdateDiscordPresence(bool resetTimer = false)
    {
        if (DiscordHandler == null)
            return;

        PlayerInfo? player = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
        if (player == null)
            return;
        string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby";

        DiscordHandler.UpdatePresence(
            untranslatedMapName, untranslatedGameMode, "Multiplayer",
            currentState, Players.Count, SGPlayers.Count,
            channel?.UIName ?? string.Empty, IsHost, resetTimer);
    }

    // --- CTCP Command Handlers ---

    private void HandleGetReadyNotification(string sender)
    {
        if (sender != hostName)
            return;

        GetReadyNotification();
    }

    private void HandleNotAllPresentNotification(string sender)
    {
        if (sender != hostName)
            return;

        NotAllPresentNotification();
    }

    private void HandleFileHashCommand(string sender, string fileHash)
    {
        if (!IsHost)
            return;

        PlayerInfo? pInfo = Players.Find(p => p.Name == sender);
        if (pInfo == null)
            return;

        pInfo.HashReceived = true;

        if (fileHash != gameFilesHash)
            HandleCheaterNotification(hostName, sender);
    }

    private void HandleCheaterNotification(string sender, string cheaterName)
    {
        if (sender != hostName)
            return;

        AddNotice(string.Format("{0} - modified files detected! They could be cheating!".L10N("Client:Main:PlayerCheating"), cheaterName));

        if (IsHost)
            channel?.SendCTCPMessage(INVALID_FILE_HASH_CTCP_COMMAND + " " + cheaterName, QueuedMessageType.SYSTEM_MESSAGE, 0);
    }

    private void HandleTunnelPing(string sender, int pingInMs)
    {
        if (pingInMs < 0)
            AddNotice(string.Format("{0} - unknown ping to tunnel server.".L10N("Client:Main:PlayerUnknownPing"), sender));
        else
            AddNotice(string.Format("{0} - ping to tunnel server: {1} ms".L10N("Client:Main:PlayerPing"), sender, pingInMs));
    }

    private void HandleOptionsMessage(string sender, string data)
    {
        if (sender != hostName)
            return;

        string[] parts = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 1)
            return;

        int sgIndex = Conversions.IntFromString(parts[0], -1);

        if (sgIndex < 0)
            return;

        if (sgIndex >= SavedGameNames.Count)
        {
            AddNotice("The game host has selected an invalid saved game index!".L10N("Client:Main:HostInvalidIndex") + " " + sgIndex);
            channel?.SendCTCPMessage(INVALID_SAVED_GAME_INDEX_CTCP_COMMAND, QueuedMessageType.SYSTEM_MESSAGE, 10);
            return;
        }

        SelectedSavedGameIndex = sgIndex;

        Players.Clear();

        for (int i = 1; i < parts.Length; i++)
        {
            string[] playerAndReadyStatus = parts[i].Split(':');
            if (playerAndReadyStatus.Length < 2)
                return;

            string playerName = playerAndReadyStatus[0];
            int readyStatus = Conversions.IntFromString(playerAndReadyStatus[1], -1);

            if (string.IsNullOrEmpty(playerName) || readyStatus == -1)
                return;

            PlayerInfo pInfo = new PlayerInfo();
            pInfo.Name = playerName;
            pInfo.Ready = Convert.ToBoolean(readyStatus);

            Players.Add(pInfo);
        }

        CopyPlayerDataToUI();
    }

    private void HandleInvalidSaveIndexCommand(string sender)
    {
        PlayerInfo? pInfo = Players.Find(p => p.Name == sender);

        if (pInfo == null)
            return;

        pInfo.Ready = false;

        AddNotice(string.Format("{0} does not have the selected saved game on their system! Try selecting an earlier saved game.".L10N("Client:Main:PlayerDontHaveSavedGame"), pInfo.Name));

        CopyPlayerDataToUI();
    }

    private void HandleStartGameCommand(string sender, string data)
    {
        if (sender != hostName)
            return;

        string[] parts = data.Split(';');

        int playerCount = parts.Length / 2;

        for (int i = 0; i < playerCount; i++)
        {
            if (parts.Length < i * 2 + 1)
                return;

            string pName = parts[i * 2];
            string ipAndPort = parts[i * 2 + 1];
            string[] ipAndPortSplit = ipAndPort.Split(':');

            if (ipAndPortSplit.Length < 2)
                return;

            int port = 0;
            bool success = int.TryParse(ipAndPortSplit[1], out port);
            if (!success)
                return;

            PlayerInfo? pInfo = Players.Find(p => p.Name == pName);

            if (pInfo == null)
                continue;

            pInfo.Port = port;
        }

        PerformLoadGame();
    }

    private void HandlePlayerReadyRequest(string sender, int readyStatus)
    {
        PlayerInfo? pInfo = Players.Find(p => p.Name == sender);

        if (pInfo == null)
            return;

        pInfo.Ready = Convert.ToBoolean(readyStatus);

        CopyPlayerDataToUI();

        if (IsHost)
            BroadcastOptions();
    }

    private void HandleTunnelServerChangeMessage(string sender, string tunnelAddressAndPort)
    {
        if (sender != hostName)
            return;

        string[] split = tunnelAddressAndPort.Split(':');
        string tunnelAddress = split[0];
        int tunnelPort = int.Parse(split[1]);

        CnCNetTunnel? tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort);
        if (tunnel == null)
        {
            AddNotice(("The game host has selected an invalid tunnel server! " +
                "The game host needs to change the server or you will be unable " +
                "to participate in the match.").L10N("Client:Main:HostInvalidTunnel"));
            CanLoadGame = false;
            return;
        }

        HandleTunnelServerChange(tunnel);
        CanLoadGame = true;
    }

    private void HandleTunnelServerChange(CnCNetTunnel tunnel)
    {
        tunnelHandler.CurrentTunnel = tunnel;
        SelectedTunnelName = tunnel.Name;
        AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("Client:Main:HostChangeTunnel"), tunnel.Name));
    }

    // --- Game broadcast ---

    private void BroadcastGame()
    {
        Channel? broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

        if (broadcastChannel == null)
            return;

        StringBuilder sb = new StringBuilder("GAME ");
        sb.Append(ProgramConstants.CNCNET_PROTOCOL_REVISION);
        sb.Append(";");
        sb.Append(ProgramConstants.GAME_VERSION);
        sb.Append(";");
        sb.Append(SGPlayers.Count);
        sb.Append(";");
        sb.Append(channel!.ChannelName);
        sb.Append(";");
        sb.Append(channel.UIName);
        sb.Append(";");
        if (started || Players.Count == SGPlayers.Count)
            sb.Append("1");
        else
            sb.Append("0");
        sb.Append("0"); // IsCustomPassword
        sb.Append("0"); // Closed
        sb.Append("1"); // IsLoadedGame
        sb.Append("0"); // IsLadder
        sb.Append(";");
        foreach (SavedGamePlayer sgPlayer in SGPlayers)
        {
            sb.Append(sgPlayer.Name);
            sb.Append(",");
        }

        sb.Remove(sb.Length - 1, 1);
        sb.Append(";");
        sb.Append(untranslatedMapName);
        sb.Append(";");
        sb.Append(untranslatedGameMode);
        sb.Append(";");
        sb.Append(tunnelHandler.CurrentTunnel!.Address + ":" + tunnelHandler.CurrentTunnel.Port);
        sb.Append(";");
        sb.Append(0); // LoadedGameId
        sb.Append(";");
        sb.Append(ClientConfiguration.Instance.DefaultSkillLevelIndex); // we don't know the original skill level
        sb.Append(";"); // Map SHA1
        sb.Append(";"); // Game option values

        broadcastChannel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 20);
    }

    // --- Timer management ---

    private void StartGameBroadcastTimer()
    {
        StopGameBroadcastTimer();

        gameBroadcastTimer = new Timer(GAME_BROADCAST_INTERVAL * 1000);
        gameBroadcastTimer.AutoReset = true;
        gameBroadcastTimer.Elapsed += (s, e) => BroadcastGame();
        gameBroadcastTimer.Start();

        // Initial broadcast after delay
        Timer initialTimer = new Timer(INITIAL_GAME_BROADCAST_DELAY * 1000);
        initialTimer.AutoReset = false;
        initialTimer.Elapsed += (s, e) =>
        {
            BroadcastGame();
            initialTimer.Dispose();
        };
        initialTimer.Start();
    }

    private void StopGameBroadcastTimer()
    {
        if (gameBroadcastTimer != null)
        {
            gameBroadcastTimer.Stop();
            gameBroadcastTimer.Dispose();
            gameBroadcastTimer = null;
        }
    }

    protected override string GetIPAddressForPlayer(PlayerInfo pInfo)
    {
        return "0.0.0.0";
    }
}


