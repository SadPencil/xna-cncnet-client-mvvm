using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Domain;
using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.LAN;
using AvClientViewModel.Multiplayer.GameLobby.CommandHandlers;
using AvClientViewModel.Online;
using AvClientViewModel.Services;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.PlatformShim;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rampastring.Tools;

using Serilog;

using Timer = System.Timers.Timer;

namespace AvClientViewModel.Multiplayer.GameLobby;


public partial class LANGameLobbyViewModel : MultiplayerGameLobbyViewModel, ILANGameLobbyViewModel
{
    private const int GAME_OPTION_SPECIAL_FLAG_COUNT = 5;
    private const double DROPOUT_TIMEOUT = 20.0;
    private const double GAME_BROADCAST_INTERVAL = 2.0;

    private const string CHAT_COMMAND = "GLCHAT";
    private const string RETURN_COMMAND = "RETURN";
    private const string GET_READY_COMMAND = "GETREADY";
    private const string PLAYER_OPTIONS_REQUEST_COMMAND = "POREQ";
    private const string PLAYER_OPTIONS_BROADCAST_COMMAND = "POPTS";
    private const string PLAYER_JOIN_COMMAND = "JOIN";
    private const string PLAYER_QUIT_COMMAND = "QUIT";
    private const string GAME_OPTIONS_COMMAND = "OPTS";
    private const string PLAYER_READY_REQUEST = "READY";
    private const string LAUNCH_GAME_COMMAND = "LAUNCH";
    private const string FILE_HASH_COMMAND = "FHASH";
    private const string DICE_ROLL_COMMAND = "DR";
    public const string PING = "PING";

    // --- Dependencies ---
    private readonly LANColor[] chatColors;
    private readonly Encoding encoding;
    private readonly string localGame;

    // --- State ---
    private TcpListener listener;
    private TcpClient client;
    private volatile bool leaving;
    private int sessionId;
    private IPEndPoint hostEndPoint;

    private string localFileHash;
    private string overMessage = string.Empty;
    private TimeSpan timeSinceLastReceivedCommand = TimeSpan.Zero;

    // --- Command handlers ---
    private CommandHandlerBase[] hostCommandHandlers;
    private LANClientCommandHandler[] playerCommandHandlers;

    // --- Observable state ---
    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _localAddressText = string.Empty;

    [ObservableProperty]
    private int _chatColorIndex;

    protected override bool IsMultiplayer => true;

    // --- Events ---
    public event EventHandler<LobbyNotificationEventArgs> LobbyNotification;
    public event EventHandler<GameLeftEventArgs> GameLeft;
    public event EventHandler<GameBroadcastEventArgs> GameBroadcast;

    // --- Timers ---
    private Timer gameBroadcastTimer;
    private Timer updateTimer;

    // --- Services ---
    private readonly IApplicationLifecycleService applicationLifecycleService;

    // --- Constructor ---

    public LANGameLobbyViewModel(
        MapLoader mapLoader,
        DiscordHandler discordHandler,
        IGameProcessService gameProcessService,
        IUIThreadMarshaller uiThreadMarshaller,
        IApplicationLifecycleService applicationLifecycleService,
        Random random,
        LANColor[] chatColors)
        : base(mapLoader, discordHandler, gameProcessService, uiThreadMarshaller, random)
    {
        this.chatColors = chatColors;
        this.encoding = EncodingExt.UTF8NoBOM;
        this.localGame = ClientConfiguration.Instance.LocalGame;
        this.applicationLifecycleService = applicationLifecycleService;

        hostCommandHandlers = new CommandHandlerBase[]
        {
            new StringCommandHandler(CHAT_COMMAND, GameHost_HandleChatCommand),
            new NoParamCommandHandler(RETURN_COMMAND, GameHost_HandleReturnCommand),
            new StringCommandHandler(PLAYER_OPTIONS_REQUEST_COMMAND, HandlePlayerOptionsRequest),
            new NoParamCommandHandler(PLAYER_QUIT_COMMAND, HandlePlayerQuit),
            new StringCommandHandler(PLAYER_READY_REQUEST, GameHost_HandleReadyRequest),
            new StringCommandHandler(FILE_HASH_COMMAND, HandleFileHashCommand),
            new StringCommandHandler(DICE_ROLL_COMMAND, Host_HandleDiceRoll),
            new NoParamCommandHandler(PING, s => { }),
        };

        playerCommandHandlers = new LANClientCommandHandler[]
        {
            new ClientStringCommandHandler(CHAT_COMMAND, Player_HandleChatCommand),
            new ClientNoParamCommandHandler(GET_READY_COMMAND, HandleGetReadyCommand),
            new ClientNoParamCommandHandler(PLAYER_QUIT_COMMAND, HandleHostQuit),
            new ClientStringCommandHandler(RETURN_COMMAND, Player_HandleReturnCommand),
            new ClientStringCommandHandler(PLAYER_OPTIONS_BROADCAST_COMMAND, HandlePlayerOptionsBroadcast),
            new ClientStringCommandHandler(PlayerExtraOptions.LAN_MESSAGE_KEY, HandlePlayerExtraOptionsBroadcast),
            new ClientStringCommandHandler(LAUNCH_GAME_COMMAND, HandleGameLaunchCommand),
            new ClientStringCommandHandler(GAME_OPTIONS_COMMAND, HandleGameOptionsMessage),
            new ClientStringCommandHandler(DICE_ROLL_COMMAND, Client_HandleDiceRoll),
            new ClientNoParamCommandHandler(PING, HandlePing),
        };

        gameBroadcastTimer = new Timer(GAME_BROADCAST_INTERVAL * 1000);
        gameBroadcastTimer.AutoReset = true;
        gameBroadcastTimer.Elapsed += GameBroadcastTimer_Elapsed;

        updateTimer = new Timer(1000); // 1 second tick
        updateTimer.AutoReset = true;
        updateTimer.Elapsed += UpdateTimer_Elapsed;

        applicationLifecycleService.ApplicationClosing += OnApplicationClosing;
    }

    protected override int MaxPlayerCount => MAX_PLAYER_COUNT;

    public override bool IsHost { get; set; }

    // --- Lifecycle ---

    public void SetUp(bool isHost, IPEndPoint hostEndPoint, TcpClient client)
    {
        leaving = false;
        sessionId++;
        IsHost = isHost;
        Refresh(isHost);

        this.hostEndPoint = hostEndPoint;

        if (isHost)
        {
            RandomSeed = random.Next();
            Thread thread = new Thread(ListenForClients);
            thread.Start();

            this.client = new TcpClient();
            this.client.Connect("127.0.0.1", ProgramConstants.LAN_GAME_LOBBY_PORT);

            byte[] buffer = encoding.GetBytes(PLAYER_JOIN_COMMAND +
                ProgramConstants.LAN_DATA_SEPARATOR + ProgramConstants.PLAYERNAME);

            this.client.GetStream().Write(buffer, 0, buffer.Length);
            this.client.GetStream().Flush();

            var fhc = new FileHashCalculator();
            fhc.CalculateHashes();
            localFileHash = fhc.GetCompleteHash();

            RefreshMapSelectionUI();

            gameBroadcastTimer.Start();
        }
        else
        {
            this.client = client;
        }

        new Thread(HandleServerCommunication).Start();

        if (IsHost)
            CopyPlayerDataToUI();

        updateTimer.Start();
    }

    public void PostJoin()
    {
        var fhc = new FileHashCalculator();
        fhc.CalculateHashes();
        SendMessageToHost(FILE_HASH_COMMAND + " " + fhc.GetCompleteHash());
        ResetAutoReadyCheckbox();
    }

    // --- Server code ---

    private void ListenForClients()
    {
        listener = new TcpListener(IPAddress.Any, ProgramConstants.LAN_GAME_LOBBY_PORT);
        listener.Start();

        while (true)
        {
            TcpClient tcpClient;

            try
            {
                tcpClient = listener.AcceptTcpClient();
            }
            catch (Exception ex)
            {
                Log.Warning("Listener error: " + ex.ToString());
                break;
            }

            Log.Information("New client connected from " + ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());

            if (Players.Count >= MAX_PLAYER_COUNT)
            {
                Log.Information("Dropping client because of player limit.");
                tcpClient.Close();
                continue;
            }

            if (Locked)
            {
                Log.Information("Dropping client because the game room is locked.");
                tcpClient.Close();
                continue;
            }

            LANPlayerInfo lpInfo = new LANPlayerInfo(encoding);
            lpInfo.SetClient(tcpClient);

            Thread thread = new Thread(new ParameterizedThreadStart(HandleClientConnection));
            thread.Start(lpInfo);
        }
    }

    private void HandleClientConnection(object clientInfo)
    {
        var lpInfo = (LANPlayerInfo)clientInfo;

        byte[] message = new byte[1024];

        while (true)
        {
            int bytesRead = 0;

            try
            {
                bytesRead = lpInfo.TcpClient.GetStream().Read(message, 0, message.Length);
            }
            catch (Exception ex)
            {
                Log.Warning("Socket error with client " + lpInfo.IPAddress + "; removing. Message: " + ex.ToString());
                break;
            }

            if (bytesRead == 0)
            {
                Log.Warning("Connect attempt from " + lpInfo.IPAddress + " failed! (0 bytes read)");
                break;
            }

            string msg = encoding.GetString(message, 0, bytesRead);

            string[] command = msg.Split(ProgramConstants.LAN_MESSAGE_SEPARATOR);
            string[] parts = command[0].Split(ProgramConstants.LAN_DATA_SEPARATOR);

            if (parts.Length != 2)
                break;

            string name = parts[1].Trim();

            if (parts[0] == "JOIN" && !string.IsNullOrEmpty(name))
            {
                lpInfo.Name = name;

                UIThreadMarshaller.AddCallback(new Action<LANPlayerInfo>(AddPlayer), lpInfo);
                return;
            }

            break;
        }

        if (lpInfo.TcpClient.Connected)
            lpInfo.TcpClient.Close();
    }

    private void AddPlayer(LANPlayerInfo lpInfo)
    {
        if (Players.Find(p => p.Name == lpInfo.Name) != null ||
            Players.Count >= MAX_PLAYER_COUNT || Locked)
            return;

        Players.Add(lpInfo);

        if (IsHost && Players.Count == 1)
            Players[0].Ready = true;

        lpInfo.MessageReceived += LpInfo_MessageReceived;
        lpInfo.ConnectionLost += LpInfo_ConnectionLost;

        AddNotice(string.Format("{0} connected from {1}".L10N("Client:Main:PlayerFromIP"), lpInfo.Name, lpInfo.IPAddress));
        lpInfo.StartReceiveLoop();

        OnGameOptionChanged();
        CopyPlayerDataToUI();
        BroadcastPlayerOptions();
        BroadcastPlayerExtraOptions();
        UpdateDiscordPresence();
        RaiseSoundRequested("joingame.wav");
    }

    private void LpInfo_ConnectionLost(object sender, EventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action<LANPlayerInfo>(HandleConnectionLost), (LANPlayerInfo)sender);
    }

    private void HandleConnectionLost(LANPlayerInfo lpInfo)
    {
        CleanUpPlayer(lpInfo);
        Players.Remove(lpInfo);

        AddNotice(string.Format("{0} has left the game.".L10N("Client:Main:PlayerLeftGame"), lpInfo.Name));
        RaiseSoundRequested("leavegame.wav");

        CopyPlayerDataToUI();
        BroadcastPlayerOptions();

        if (lpInfo.Name == ProgramConstants.PLAYERNAME)
            ResetDiscordPresence();
        else
            UpdateDiscordPresence();
    }

    private void LpInfo_MessageReceived(object sender, NetworkMessageEventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action<string, LANPlayerInfo>(HandleClientMessage),
            e.Message, (LANPlayerInfo)sender);
    }

    private void HandleClientMessage(string data, LANPlayerInfo lpInfo)
    {
        lpInfo.TimeSinceLastReceivedMessage = TimeSpan.Zero;

        foreach (CommandHandlerBase cmdHandler in hostCommandHandlers)
        {
            if (cmdHandler.Handle(lpInfo.Name, data))
                return;
        }

        Log.Information("Unknown LAN command from " + lpInfo.ToString() + " : " + data);
    }

    private void CleanUpPlayer(LANPlayerInfo lpInfo)
    {
        lpInfo.MessageReceived -= LpInfo_MessageReceived;
        lpInfo.ConnectionLost -= LpInfo_ConnectionLost;
        lpInfo.TcpClient.Close();
    }

    // --- Client communication ---

    private void HandleServerCommunication()
    {
        byte[] message = new byte[1024];

        var msg = string.Empty;

        int bytesRead = 0;

        int mySessionId = sessionId;

        if (!client.Connected)
            return;

        var stream = client.GetStream();

        while (true)
        {
            bytesRead = 0;

            try
            {
                bytesRead = stream.Read(message, 0, message.Length);
            }
            catch (Exception ex)
            {
                if (leaving)
                    break;

                Log.Warning(string.Format(
                    "Reading data from the server failed! Server address: {0}. Exception: {1}",
                    hostEndPoint.Address.ToString(), ex.ToString()));

                string localizedMessage = string.Format(
                    "Reading data from the server failed! Server address: {0}. Exception: {1}".L10N("Client:Main:LanServerReadError"),
                     hostEndPoint.Address.ToString(), ex.Message);

                UIThreadMarshaller.AddCallback(() =>
                {
                    if (sessionId == mySessionId)
                        LeaveGame(localizedMessage);
                });
                break;
            }

            if (bytesRead > 0)
            {
                msg = encoding.GetString(message, 0, bytesRead);

                msg = overMessage + msg;
                List<string> commands = new List<string>();

                while (true)
                {
                    int index = msg.IndexOf(ProgramConstants.LAN_MESSAGE_SEPARATOR);

                    if (index == -1)
                    {
                        overMessage = msg;
                        break;
                    }
                    else
                    {
                        commands.Add(msg.Substring(0, index));
                        msg = msg.Substring(index + 1);
                    }
                }

                foreach (string cmd in commands)
                {
                    string capturedCmd = cmd;
                    UIThreadMarshaller.AddCallback(() =>
                    {
                        if (sessionId == mySessionId)
                            HandleMessageFromServer(capturedCmd);
                    });
                }

                continue;
            }

            if (leaving)
                break;

            {
                Log.Warning(string.Format(
                    "Reading data from the server failed (0 bytes received)! Server address: {0}", hostEndPoint.Address.ToString()));

                string localizedMessage = string.Format(
                    "Reading data from the server failed (0 bytes received)! Server address: {0}".L10N("Client:Main:LanServerReadZero"),
                     hostEndPoint.Address.ToString());

                UIThreadMarshaller.AddCallback(() =>
                {
                    if (sessionId == mySessionId)
                        LeaveGame(localizedMessage);
                });
            }

            break;
        }
    }

    private void HandleMessageFromServer(string message)
    {
        timeSinceLastReceivedCommand = TimeSpan.Zero;

        foreach (var cmdHandler in playerCommandHandlers)
        {
            if (cmdHandler.Handle(message))
                return;
        }

        Log.Information("Unknown LAN command from the server: " + message);
    }

    // --- Leave game ---

    [RelayCommand]
    public void LeaveGameLobby() => LeaveGame();

    protected override void LeaveGame() => LeaveGame(null);

    protected void LeaveGame(string message = null)
    {
        if (leaving)
            return;

        Clear();
        GameLeft?.Invoke(this, new GameLeftEventArgs() { Message = message });
    }

    // --- Clear ---

    public override void Clear()
    {
        updateTimer.Stop();
        applicationLifecycleService.ApplicationClosing -= OnApplicationClosing;

        if (IsHost)
        {
            GameBroadcast?.Invoke(this, new GameBroadcastEventArgs("GAMECLOSED"));
            BroadcastMessage(PLAYER_QUIT_COMMAND);
            Players.ForEach(p => CleanUpPlayer((LANPlayerInfo)p));
            listener.Stop();
            gameBroadcastTimer.Stop();
        }
        else
        {
            SendMessageToHost(PLAYER_QUIT_COMMAND);
        }

        base.Clear();

        leaving = true;

        if (this.client.Connected)
            this.client.Close();

        ResetDiscordPresence();
    }

    // --- Discord ---

    protected override void UpdateDiscordPresence(bool resetTimer = false)
    {
        if (DiscordHandler == null)
            return;

        PlayerInfo player = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
        if (player == null || Map == null || GameMode == null)
            return;

        string side = "";
        string[] sides = ClientConfiguration.Instance.Sides.Split(',');
        if (player.SideId > 0 && player.SideId <= sides.Length)
            side = sides[player.SideId - 1];

        string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby";

        DiscordHandler.UpdatePresence(
            Map.UntranslatedName, GameMode.UntranslatedUIName, "LAN",
            currentState, Players.Count, MAX_PLAYER_COUNT, side,
            "LAN Game", IsHost, false, Locked, resetTimer);
    }

    public override string GetSwitchName() => "LAN Game Lobby".L10N("Client:Main:LANGameLobby");

    // --- Player options ---

    protected override void BroadcastPlayerOptions()
    {
        if (!IsHost)
            return;

        var sb = new ExtendedStringBuilder(PLAYER_OPTIONS_BROADCAST_COMMAND + " ", true);
        sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
        foreach (PlayerInfo pInfo in Players.Concat(AIPlayers))
        {
            sb.Append(pInfo.Name);
            sb.Append(pInfo.SideId);
            sb.Append(pInfo.ColorId);
            sb.Append(pInfo.StartingLocation);
            sb.Append(pInfo.TeamId);
            if (pInfo.AutoReady && !pInfo.IsInGame && !LastMapChangeWasInvalid)
                sb.Append(2);
            else
                sb.Append(Convert.ToInt32(pInfo.IsAI || pInfo.Ready));
            sb.Append(pInfo.IPAddress);
            if (pInfo.IsAI)
                sb.Append(pInfo.AILevel);
            else
                sb.Append("-1");
        }

        BroadcastMessage(sb.ToString());
    }

    protected override void BroadcastPlayerExtraOptions()
    {
        if (!IsHost)
            return;

        BroadcastMessage(PlayerExtraOptions.ToLanMessage(), true);
    }

    protected override void HostLaunchGame() => BroadcastMessage(LAUNCH_GAME_COMMAND + " " + UniqueGameID);

    protected override string GetIPAddressForPlayer(PlayerInfo player)
    {
        var lpInfo = (LANPlayerInfo)player;
        return lpInfo.IPAddress;
    }

    protected override void RequestPlayerOptions(int side, int color, int start, int team)
    {
        var sb = new ExtendedStringBuilder(PLAYER_OPTIONS_REQUEST_COMMAND + " ", true);
        sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
        sb.Append(side);
        sb.Append(color);
        sb.Append(start);
        sb.Append(team);
        SendMessageToHost(sb.ToString());
    }

    protected override void RequestReadyStatus() =>
        SendMessageToHost(PLAYER_READY_REQUEST + " " + Convert.ToInt32(IsAutoReadyChecked));

    protected override void SendChatMessage(string message)
    {
        var sb = new ExtendedStringBuilder(CHAT_COMMAND + " ", true);
        sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
        sb.Append(ChatColorIndex);
        sb.Append(message);
        SendMessageToHost(sb.ToString());
    }

    // --- Game options ---

    protected override void OnGameOptionChanged()
    {
        base.OnGameOptionChanged();

        if (!IsHost)
            return;

        var sb = new ExtendedStringBuilder(GAME_OPTIONS_COMMAND + " ", true);
        sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
        foreach (var chkBox in CheckBoxes)
        {
            sb.Append(Convert.ToInt32(chkBox.IsChecked));
        }

        foreach (var dd in DropDowns)
        {
            sb.Append(dd.SelectedIndex);
        }

        sb.Append(RandomSeed);
        sb.Append(Map?.SHA1 ?? string.Empty);
        sb.Append(GameMode?.Name ?? string.Empty);
        sb.Append(FrameSendRate);
        sb.Append(Convert.ToInt32(RemoveStartingLocations));

        BroadcastMessage(sb.ToString());
    }

    // --- Ready notification ---

    protected override void GetReadyNotification()
    {
        base.GetReadyNotification();

        if (IsHost)
            BroadcastMessage(GET_READY_COMMAND);
    }

    // --- Lock/Unlock ---

    protected override void PerformLockGame()
    {
        Locked = true;
        LockGameButtonText = "Unlock Game".L10N("Client:Main:UnlockGame");
        AddNotice("You've locked the game room.".L10N("Client:Main:RoomLockedByYou"));
    }

    protected override void PerformUnlockGame(bool announce)
    {
        Locked = false;
        LockGameButtonText = "Lock Game".L10N("Client:Main:LockGame");
        if (announce)
            AddNotice("You've unlocked the game room.".L10N("Client:Main:RoomUnlockedByYou"));
    }

    // --- Game process exited ---

    protected override void OnGameProcessExited()
    {
        base.OnGameProcessExited();

        SendMessageToHost(RETURN_COMMAND);

        if (IsHost)
        {
            RandomSeed = random.Next();
            OnGameOptionChanged();
            ClearReadyStatuses();
            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
            BroadcastPlayerExtraOptions();

            if (Players.Count < MAX_PLAYER_COUNT)
            {
                PerformUnlockGame(true);
            }
        }
    }

    // --- Broadcast timer ---

    private void GameBroadcastTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (IsHost && !leaving)
        {
            BroadcastGameState();
        }
    }

    // --- Update timer (player timeout detection) ---

    private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action(Update));
    }

    private void Update()
    {
        if (leaving)
            return;

        if (IsHost)
        {
            for (int i = 1; i < Players.Count; i++)
            {
                LANPlayerInfo lpInfo = (LANPlayerInfo)Players[i];
                if (!lpInfo.Update(TimeSpan.FromSeconds(1)))
                {
                    CleanUpPlayer(lpInfo);
                    Players.RemoveAt(i);
                    AddNotice(string.Format("{0} - connection timed out".L10N("Client:Main:PlayerTimeout"), lpInfo.Name));
                    CopyPlayerDataToUI();
                    BroadcastPlayerOptions();
                    BroadcastPlayerExtraOptions();
                    UpdateDiscordPresence();
                    i--;
                }
            }
        }
        else
        {
            timeSinceLastReceivedCommand += TimeSpan.FromSeconds(1);
            if (timeSinceLastReceivedCommand > TimeSpan.FromSeconds(DROPOUT_TIMEOUT))
            {
                string localizedMessage = string.Format(
                    "Connection to the game host timed out. Server address: {0}".L10N("Client:Main:HostConnectTimeOutWithAddress"),
                    hostEndPoint.Address.ToString());
                LobbyNotification?.Invoke(this, new LobbyNotificationEventArgs(localizedMessage));
                LeaveGame(localizedMessage);
            }
        }
    }

    // --- Application lifecycle ---

    private void OnApplicationClosing(object sender, EventArgs e)
    {
        if (client != null && client.Connected)
            Clear();
    }

    // --- Player extra options ---

    protected override void OnPlayerExtraOptionsChanged()
    {
        base.OnPlayerExtraOptionsChanged();
        BroadcastPlayerExtraOptions();
    }

    [RelayCommand]
    private void BroadcastGameState()
    {
        var sb = new ExtendedStringBuilder("GAME ", true);
        sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
        sb.Append(ProgramConstants.LAN_PROTOCOL_REVISION);
        sb.Append(ProgramConstants.GAME_VERSION);
        sb.Append(localGame);
        sb.Append(Map?.UntranslatedName ?? string.Empty);
        sb.Append(GameMode?.UntranslatedUIName ?? string.Empty);
        sb.Append(0); // LoadedGameID
        var sbPlayers = new StringBuilder();
        Players.ForEach(p => sbPlayers.Append(p.Name + ","));
        sbPlayers.Remove(sbPlayers.Length - 1, 1);
        sb.Append(sbPlayers.ToString());
        sb.Append(Convert.ToInt32(Locked));
        sb.Append(0); // IsLoadedGame
        sb.Append(Map?.SHA1);

        GameBroadcast?.Invoke(this, new GameBroadcastEventArgs(sb.ToString()));
    }

    // --- Broadcast helper ---

    private void BroadcastMessage(string message, bool otherPlayersOnly = false)
    {
        if (!IsHost)
            return;

        foreach (PlayerInfo pInfo in Players.Where(p => !otherPlayersOnly || p.Name != ProgramConstants.PLAYERNAME))
        {
            var lpInfo = (LANPlayerInfo)pInfo;
            lpInfo.SendMessage(message);
        }
    }

    private void SendMessageToHost(string message)
    {
        if (!client.Connected)
            return;

        byte[] buffer = encoding.GetBytes(message + ProgramConstants.LAN_MESSAGE_SEPARATOR);

        NetworkStream ns = client.GetStream();

        try
        {
            ns.Write(buffer, 0, buffer.Length);
            ns.Flush();
        }
        catch
        {
            Log.Warning("Sending message to game host failed!");
        }
    }

    // --- Command Handlers ---

    private void HandleFileHashCommand(string sender, string fileHash)
    {
        if (fileHash != localFileHash)
            AddNotice(string.Format("{0} has modified game files! They could be cheating!".L10N("Client:Main:PlayerModifiedFiles"), sender));

        PlayerInfo pInfo = Players.Find(p => p.Name == sender);
        if (pInfo == null)
            return;

        pInfo.HashReceived = true;
        CopyPlayerDataToUI();
    }

    private void GameHost_HandleChatCommand(string sender, string data)
    {
        string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

        if (parts.Length < 2)
            return;

        int colorIndex = Conversions.IntFromString(parts[0], -1);

        if (colorIndex < 0 || colorIndex >= chatColors.Length)
            return;

        BroadcastMessage(CHAT_COMMAND + " " + sender + ProgramConstants.LAN_DATA_SEPARATOR + data);
    }

    private void Player_HandleChatCommand(string data)
    {
        string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

        if (parts.Length < 3)
            return;

        string playerName = parts[0];

        int colorIndex = Conversions.IntFromString(parts[1], -1);

        if (colorIndex < 0 || colorIndex >= chatColors.Length)
            return;

        AddChatMessage($"{playerName}: {parts[2]}");
        RaiseMessageSoundRequested();
    }

    private void GameHost_HandleReturnCommand(string sender)
    {
        BroadcastMessage(RETURN_COMMAND + ProgramConstants.LAN_DATA_SEPARATOR + sender);
    }

    private void Player_HandleReturnCommand(string sender)
    {
        ReturnNotification(sender);
    }

    private void HandleGetReadyCommand()
    {
        if (!IsHost)
            GetReadyNotification();
    }

    private void HandleHostQuit()
    {
        if (!IsHost && !leaving)
            LeaveGame();
    }

    private void HandlePlayerOptionsRequest(string sender, string data)
    {
        if (!IsHost)
            return;

        PlayerInfo pInfo = Players.Find(p => p.Name == sender);

        if (pInfo == null)
            return;

        string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

        if (parts.Length != 4)
            return;

        int side = Conversions.IntFromString(parts[0], -1);
        int color = Conversions.IntFromString(parts[1], -1);
        int start = Conversions.IntFromString(parts[2], -1);
        int team = Conversions.IntFromString(parts[3], -1);

        if (side < 0 || side > SideCount + RandomSelectorCount)
            return;

        if (color < 0 || color > MPColors.Count)
            return;

        var disallowedSides = GetDisallowedSides();

        if (side > 0 && side <= SideCount && disallowedSides[side - 1])
            return;

        if (GameModeMap.CoopInfo != null)
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

    private void HandlePlayerExtraOptionsBroadcast(string data)
    {
        var oldOptions = PlayerExtraOptions;
        PlayerExtraOptions = PlayerExtraOptions.FromMessage(data);
        var newOptions = PlayerExtraOptions;

        if (oldOptions.IsForceRandomSides != newOptions.IsForceRandomSides)
            AddNotice(newOptions.IsForceRandomSides
                ? "The game host has disabled side selection".L10N("Client:Main:HostDisableSide")
                : "The game host has enabled side selection".L10N("Client:Main:HostEnableSide"));
        if (oldOptions.IsForceRandomColors != newOptions.IsForceRandomColors)
            AddNotice(newOptions.IsForceRandomColors
                ? "The game host has disabled color selection".L10N("Client:Main:HostDisableColor")
                : "The game host has enabled color selection".L10N("Client:Main:HostEnableColor"));
        if (oldOptions.IsForceRandomStarts != newOptions.IsForceRandomStarts)
            AddNotice(newOptions.IsForceRandomStarts
                ? "The game host has disabled start selection".L10N("Client:Main:HostDisableStart")
                : "The game host has enabled start selection".L10N("Client:Main:HostEnableStart"));
        if (oldOptions.IsForceNoTeams != newOptions.IsForceNoTeams)
            AddNotice(newOptions.IsForceNoTeams
                ? "The game host has disabled team selection".L10N("Client:Main:HostDisableTeam")
                : "The game host has enabled team selection".L10N("Client:Main:HostEnableTeam"));
    }

    private void HandlePlayerOptionsBroadcast(string data)
    {
        if (IsHost)
            return;

        string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

        int playerCount = parts.Length / 8;

        if (parts.Length != playerCount * 8)
            return;

        PlayerInfo localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
        int oldSideId = localPlayer == null ? -1 : localPlayer.SideId;

        Players.Clear();
        AIPlayers.Clear();

        for (int i = 0; i < playerCount; i++)
        {
            int baseIndex = i * 8;

            string name = parts[baseIndex];
            int side = Conversions.IntFromString(parts[baseIndex + 1], -1);
            int color = Conversions.IntFromString(parts[baseIndex + 2], -1);
            int start = Conversions.IntFromString(parts[baseIndex + 3], -1);
            int team = Conversions.IntFromString(parts[baseIndex + 4], -1);
            int readyStatus = Conversions.IntFromString(parts[baseIndex + 5], -1);
            string ipAddress = parts[baseIndex + 6];
            int aiLevel = Conversions.IntFromString(parts[baseIndex + 7], -1);

            if (side < 0 || side > SideCount + RandomSelectorCount)
                return;

            if (color < 0 || color > MPColors.Count)
                return;

            if (start < 0 || start > MAX_PLAYER_COUNT)
                return;

            if (team < 0 || team > 4)
                return;

            if (ipAddress == "127.0.0.1")
                ipAddress = hostEndPoint.Address.ToString();

            bool isAi = aiLevel > -1;
            if (aiLevel > 2)
                return;

            PlayerInfo pInfo;

            if (!isAi)
            {
                pInfo = new LANPlayerInfo(encoding);
                pInfo.Name = name;
                Players.Add(pInfo);
            }
            else
            {
                pInfo = new PlayerInfo();
                pInfo.Name = AILevelToName(aiLevel);
                pInfo.IsAI = true;
                pInfo.AILevel = aiLevel;
                AIPlayers.Add(pInfo);
            }

            pInfo.SideId = side;
            pInfo.ColorId = color;
            pInfo.StartingLocation = start;
            pInfo.TeamId = team;
            pInfo.Ready = readyStatus > 0;
            pInfo.AutoReady = readyStatus > 1;
            pInfo.IPAddress = ipAddress;
        }

        CopyPlayerDataToUI();

        localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
        if (localPlayer != null && oldSideId != localPlayer.SideId)
            UpdateDiscordPresence();
    }

    private void HandlePlayerQuit(string sender)
    {
        PlayerInfo pInfo = Players.Find(p => p.Name == sender);

        if (pInfo == null)
            return;

        AddNotice(string.Format("{0} has left the game.".L10N("Client:Main:PlayerLeftGame"), pInfo.Name));
        Players.Remove(pInfo);
        ClearReadyStatuses();
        CopyPlayerDataToUI();
        BroadcastPlayerOptions();
        UpdateDiscordPresence();
        RaiseSoundRequested("leavegame.wav");
    }

    private void HandleGameOptionsMessage(string data)
    {
        if (IsHost)
            return;

        string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

        if (parts.Length != CheckBoxes.Count + DropDowns.Count + GAME_OPTION_SPECIAL_FLAG_COUNT)
        {
            AddNotice(("The game host has sent an invalid game options message! " +
                "The game host's game version might be different from yours.").L10N("Client:Main:HostGameOptionInvalid"));
            Log.Warning("Invalid game options message from host: " + data);
            return;
        }

        int randomSeed = Conversions.IntFromString(parts[parts.Length - GAME_OPTION_SPECIAL_FLAG_COUNT], -1);
        if (randomSeed == -1)
            return;

        RandomSeed = randomSeed;

        string mapSHA1 = parts[parts.Length - (GAME_OPTION_SPECIAL_FLAG_COUNT - 1)];
        string gameMode = parts[parts.Length - (GAME_OPTION_SPECIAL_FLAG_COUNT - 2)];

        GameModeMap gameModeMap = GameModeMaps.FirstOrDefault(gmm => gmm.GameMode.Name == gameMode && gmm.Map.SHA1 == mapSHA1);

        if (gameModeMap == null)
        {
            ChangeMap(null);
            if (!string.IsNullOrEmpty(mapSHA1))
                AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("Client:Main:MapNotExist") + " " +
                    "The host needs to change the map or you won't be able to play.".L10N("Client:Main:HostNeedChangeMapForYou"));

            return;
        }

        if (GameModeMap != gameModeMap)
            ChangeMap(gameModeMap);

        int frameSendRate = Conversions.IntFromString(parts[parts.Length - (GAME_OPTION_SPECIAL_FLAG_COUNT - 3)], FrameSendRate);
        if (frameSendRate != FrameSendRate)
        {
            FrameSendRate = frameSendRate;
            AddNotice(string.Format("The game host has changed FrameSendRate (order lag) to {0}".L10N("Client:Main:HostChangeFrameSendRate"), frameSendRate));
        }

        bool removeStartingLocations = Convert.ToBoolean(Conversions.IntFromString(
            parts[parts.Length - (GAME_OPTION_SPECIAL_FLAG_COUNT - 4)], Convert.ToInt32(RemoveStartingLocations)));
        SetRandomStartingLocations(removeStartingLocations);

        for (int i = 0; i < CheckBoxes.Count; i++)
        {
            var chkBox = (GameOptionCheckBox)CheckBoxes[i];

            bool oldValue = chkBox.IsChecked;
            chkBox.IsChecked = Conversions.IntFromString(parts[i], -1) > 0;

            if (chkBox.IsChecked != oldValue)
            {
                if (chkBox.IsChecked)
                    AddNotice(string.Format("The game host has enabled {0}".L10N("Client:Main:HostEnableOption"), chkBox.Name));
                else
                    AddNotice(string.Format("The game host has disabled {0}".L10N("Client:Main:HostDisableOption"), chkBox.Name));
            }
        }

        for (int i = 0; i < DropDowns.Count; i++)
        {
            int index = Conversions.IntFromString(parts[CheckBoxes.Count + i], -1);

            var dd = (GameOptionDropDown)DropDowns[i];

            if (index < 0 || index >= dd.Items.Count)
                return;

            int oldValue = dd.SelectedIndex;
            dd.SelectedIndex = index;

            if (index != oldValue)
            {
                AddNotice(string.Format("The game host has set {0} to {1}".L10N("Client:Main:HostSetOption"), dd.Name, dd.Items[dd.SelectedIndex]));
            }
        }
    }

    private void GameHost_HandleReadyRequest(string sender, string autoReady)
    {
        PlayerInfo pInfo = Players.Find(p => p.Name == sender);

        if (pInfo == null)
            return;

        pInfo.Ready = true;
        pInfo.AutoReady = Convert.ToBoolean(Conversions.IntFromString(autoReady, 0));
        CopyPlayerDataToUI();
        BroadcastPlayerOptions();
    }

    private void HandleGameLaunchCommand(string gameId)
    {
        Players.ForEach(pInfo => pInfo.IsInGame = true);
        UniqueGameID = Conversions.IntFromString(gameId, -1);
        if (UniqueGameID < 0)
            return;

        CopyPlayerDataToUI();
        StartGame();
    }

    private void HandlePing()
    {
        SendMessageToHost(PING);
    }

    private void ReturnNotification(string sender)
    {
        AddNotice(string.Format("{0} has returned from the game.".L10N("Client:Main:PlayerReturned"), sender));

        PlayerInfo pInfo = Players.Find(p => p.Name == sender);

        if (pInfo != null)
            pInfo.IsInGame = false;

        CopyPlayerDataToUI();
        RaiseSoundRequested("return.wav");
    }

    // --- Dice roll ---

    protected override void BroadcastDiceRoll(int dieSides, int[] results)
    {
        string resultString = string.Join(",", results);
        SendMessageToHost($"DR {dieSides},{resultString}");
    }

    private void Host_HandleDiceRoll(string sender, string result)
    {
        BroadcastMessage($"{DICE_ROLL_COMMAND} {sender}{ProgramConstants.LAN_DATA_SEPARATOR}{result}");
    }

    private void Client_HandleDiceRoll(string data)
    {
        string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);
        if (parts.Length != 2)
            return;

        HandleDiceRollResult(parts[0], parts[1]);
    }

    // --- Spawn INI ---

    protected override void WriteSpawnIniAdditions(IniFile iniFile)
    {
        base.WriteSpawnIniAdditions(iniFile);

        iniFile.SetIntValue("Settings", "Port", ProgramConstants.LAN_INGAME_PORT);
        iniFile.SetIntValue("Settings", "GameID", UniqueGameID);
        iniFile.SetBooleanValue("Settings", "Host", IsHost);
    }
}


