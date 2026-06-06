using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Domain;
using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.LAN;
using AvClientViewModel.Multiplayer.GameLobby;
using AvClientViewModel.Online;
using AvClientViewModel.Services;

using ClientCore;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rampastring.Tools;

using Serilog;

using Timer = System.Timers.Timer;

namespace AvClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for LAN game loading lobby.
/// </summary>

public partial class LANGameLoadingLobbyViewModel : GameLoadingLobbyBaseViewModel, ILANGameLoadingLobbyViewModel
{
    private const double DROPOUT_TIMEOUT = 20.0;
    private const double GAME_BROADCAST_INTERVAL = 2.0;
    private const double UPDATE_INTERVAL_MS = 500.0;

    private const string OPTIONS_COMMAND = "OPTS";
    private const string GAME_LAUNCH_COMMAND = "START";
    private const string READY_STATUS_COMMAND = "READY";
    private const string CHAT_COMMAND = "CHAT";
    private const string PLAYER_QUIT_COMMAND = "QUIT";
    private const string PLAYER_JOIN_COMMAND = "JOIN";
    private const string FILE_HASH_COMMAND = "FHASH";

    private readonly IApplicationLifecycleService applicationLifecycleService;
    private readonly LANColor[] chatColors;
    private readonly Encoding encoding;

    private TcpListener? listener;
    private TcpClient? client;
    private IPEndPoint? hostEndPoint;
    private int chatColorIndex;
    private string localGame;
    private string localFileHash = string.Empty;
    private string untranslatedMapName = string.Empty;
    private string untranslatedGameMode = string.Empty;

    private LANServerCommandHandler[] hostCommandHandlers;
    private LANClientCommandHandler[] playerCommandHandlers;

    private TimeSpan timeSinceGameBroadcast = TimeSpan.Zero;
    private TimeSpan timeSinceLastReceivedCommand = TimeSpan.Zero;
    private string overMessage = string.Empty;
    private int loadedGameId;
    private bool started;
    private volatile bool leaving;
    private int sessionId;

    private Timer? updateTimer;

    // --- Observable state ---

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    [ObservableProperty]
    public partial string LocalAddressText { get; set; }  = string.Empty;

    [ObservableProperty]
    public partial bool AreAllPlayersReady { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    // --- Events ---

    public event EventHandler<LobbyNotificationEventArgs>? LobbyNotification;
    public event EventHandler<GameBroadcastEventArgs>? GameBroadcast;

    // --- Constructor ---

    public LANGameLoadingLobbyViewModel(
        DiscordHandler discordHandler,
        IGameProcessService gameProcessService,
        IUIThreadMarshaller uiThreadMarshaller,
        IApplicationLifecycleService applicationLifecycleService,
        LANColor[] chatColors)
        : base(discordHandler, gameProcessService, uiThreadMarshaller)
    {
        this.applicationLifecycleService = applicationLifecycleService;
        this.chatColors = chatColors;
        this.encoding = ProgramConstants.LAN_ENCODING;
        this.localGame = ClientConfiguration.Instance.LocalGame;

        hostCommandHandlers = new LANServerCommandHandler[]
        {
            new ServerStringCommandHandler(CHAT_COMMAND, Server_HandleChatMessage),
            new ServerStringCommandHandler(FILE_HASH_COMMAND, Server_HandleFileHashMessage),
            new ServerNoParamCommandHandler(READY_STATUS_COMMAND, Server_HandleReadyRequest),
        };

        playerCommandHandlers = new LANClientCommandHandler[]
        {
            new ClientStringCommandHandler(CHAT_COMMAND, Client_HandleChatMessage),
            new ClientStringCommandHandler(OPTIONS_COMMAND, Client_HandleOptionsMessage),
            new ClientNoParamCommandHandler(GAME_LAUNCH_COMMAND, Client_HandleStartCommand),
            new ClientNoParamCommandHandler(PLAYER_QUIT_COMMAND, HandleHostQuit),
        };

        applicationLifecycleService.ApplicationClosing += (_, _) =>
        {
            if (client != null && client.Connected)
                Clear();
        };
    }

    // --- Lifecycle ---

    public override void Initialize()
    {
        // No-op: setup happens in SetUp
    }

    public void SetUp(bool isHost, IPEndPoint hostEndPoint, TcpClient client, int loadedGameId)
    {
        leaving = false;
        sessionId++;
        Refresh(isHost);

        this.hostEndPoint = hostEndPoint;
        this.loadedGameId = loadedGameId;
        started = false;

        untranslatedMapName = MapName;
        untranslatedGameMode = GameMode;

        if (isHost)
        {
            Thread thread = new Thread(ListenForClients);
            thread.Start();

            this.client = new TcpClient();
            this.client.Connect("127.0.0.1", ProgramConstants.LAN_GAME_LOBBY_PORT);

            byte[] buffer = encoding.GetBytes(PLAYER_JOIN_COMMAND +
                ProgramConstants.LAN_DATA_SEPARATOR + ProgramConstants.PLAYERNAME +
                ProgramConstants.LAN_DATA_SEPARATOR + loadedGameId);

            this.client.GetStream().Write(buffer, 0, buffer.Length);
            this.client.GetStream().Flush();

            var fhc = new FileHashCalculator();
            fhc.CalculateHashes();
            localFileHash = fhc.GetCompleteHash();
        }
        else
        {
            this.client = client;
        }

        new Thread(HandleServerCommunication).Start();

        if (IsHost)
            CopyPlayerDataToUI();

        StartUpdateTimer();
    }

    public void PostJoin()
    {
        var fhc = new FileHashCalculator();
        fhc.CalculateHashes();
        SendMessageToHost(FILE_HASH_COMMAND + " " + fhc.GetCompleteHash());
        UpdateDiscordPresence(true);
    }

    public void SetChatColorIndex(int colorIndex)
    {
        chatColorIndex = colorIndex;
    }

    // --- Timer management ---

    private void StartUpdateTimer()
    {
        StopUpdateTimer();
        updateTimer = new Timer(UPDATE_INTERVAL_MS);
        updateTimer.AutoReset = true;
        updateTimer.Elapsed += UpdateTimer_Elapsed;
        updateTimer.Start();
    }

    private void StopUpdateTimer()
    {
        if (updateTimer != null)
        {
            updateTimer.Stop();
            updateTimer.Dispose();
            updateTimer = null;
        }
    }

    private void UpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action(UI_UpdateTick));
    }

    private void UI_UpdateTick()
    {
        if (IsHost)
        {
            for (int i = 1; i < Players.Count; i++)
            {
                LANPlayerInfo lpInfo = (LANPlayerInfo)Players[i];
                if (!lpInfo.Update(TimeSpan.FromMilliseconds(UPDATE_INTERVAL_MS)))
                {
                    CleanUpPlayer(lpInfo);
                    Players.RemoveAt(i);
                    AddNotice(string.Format("{0} - connection timed out".L10N("Client:Main:PlayerTimeout"), lpInfo.Name));
                    CopyPlayerDataToUI();
                    BroadcastOptions();
                    UpdateDiscordPresence();
                    i--;
                }
            }

            timeSinceGameBroadcast += TimeSpan.FromMilliseconds(UPDATE_INTERVAL_MS);

            if (timeSinceGameBroadcast > TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL))
            {
                BroadcastGame();
                timeSinceGameBroadcast = TimeSpan.Zero;
            }
        }
        else
        {
            timeSinceLastReceivedCommand += TimeSpan.FromMilliseconds(UPDATE_INTERVAL_MS);

            if (timeSinceLastReceivedCommand > TimeSpan.FromSeconds(DROPOUT_TIMEOUT))
            {
                LobbyNotification?.Invoke(this,
                    new LobbyNotificationEventArgs("Connection to the game host timed out.".L10N("Client:Main:HostConnectTimeOut")));
                UI_LeaveGame();
            }
        }
    }

    // --- Server code ---

    private void ListenForClients()
    {
        listener = new TcpListener(IPAddress.Any, ProgramConstants.LAN_GAME_LOBBY_PORT);
        listener.Start();

        while (true)
        {
            TcpClient client;

            try
            {
                client = listener.AcceptTcpClient();
            }
            catch (Exception ex)
            {
                Log.Warning("Listener error: " + ex.ToString());
                break;
            }

            Log.Information("New client connected from " + ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString());

            LANPlayerInfo lpInfo = new LANPlayerInfo(encoding);
            lpInfo.SetClient(client);

            Thread thread = new Thread(new ParameterizedThreadStart(HandleClientConnection!));
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

            if (parts.Length != 3)
                break;

            string name = parts[1].Trim();
            int joinGameId = Conversions.IntFromString(parts[2], -1);

            if (parts[0] == "JOIN" && !string.IsNullOrEmpty(name)
                && joinGameId == this.loadedGameId)
            {
                lpInfo.Name = name;

                UIThreadMarshaller.AddCallback(new Action<LANPlayerInfo>(UI_AddPlayer), lpInfo);
                return;
            }

            break;
        }

        if (lpInfo.TcpClient.Connected)
            lpInfo.TcpClient.Close();
    }

    private void UI_AddPlayer(LANPlayerInfo lpInfo)
    {
        if (Players.Find(p => p.Name == lpInfo.Name) != null ||
            Players.Count >= SGPlayers.Count ||
            SGPlayers.Find(p => p.Name == lpInfo.Name) == null)
        {
            lpInfo.TcpClient.Close();
            return;
        }

        if (Players.Count == 0)
            lpInfo.Ready = true;

        Players.Add(lpInfo);

        lpInfo.MessageReceived += LpInfo_MessageReceived;
        lpInfo.ConnectionLost += LpInfo_ConnectionLost;

        RaiseJoinSoundRequested();

        AddNotice(string.Format("{0} connected from {1}".L10N("Client:Main:PlayerFromIP"), lpInfo.Name, lpInfo.IPAddress));
        lpInfo.StartReceiveLoop();

        CopyPlayerDataToUI();
        BroadcastOptions();
        UpdateDiscordPresence();
    }

    private void LpInfo_ConnectionLost(object? sender, EventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action<LANPlayerInfo>(UI_HandleConnectionLost), (LANPlayerInfo)sender!);
    }

    private void UI_HandleConnectionLost(LANPlayerInfo lpInfo)
    {
        CleanUpPlayer(lpInfo);
        Players.Remove(lpInfo);

        AddNotice(string.Format("{0} has left the game.".L10N("Client:Main:PlayerLeftGame"), lpInfo.Name));

        RaiseLeaveSoundRequested();

        CopyPlayerDataToUI();
        BroadcastOptions();
        UpdateDiscordPresence();
    }

    private void LpInfo_MessageReceived(object? sender, NetworkMessageEventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action<string, LANPlayerInfo>(UI_HandleClientMessage),
            e.Message, (LANPlayerInfo)sender!);
    }

    private void UI_HandleClientMessage(string data, LANPlayerInfo lpInfo)
    {
        lpInfo.TimeSinceLastReceivedMessage = TimeSpan.Zero;

        foreach (var cmdHandler in hostCommandHandlers)
        {
            if (cmdHandler.Handle(lpInfo, data))
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

    // --- Client code ---

    private void HandleServerCommunication()
    {
        byte[] message = new byte[1024];

        var msg = string.Empty;

        int bytesRead = 0;

        int mySessionId = sessionId;

        if (client == null || !client.Connected)
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

                Log.Warning("Reading data from the server failed! Message: " + ex.ToString());
                UIThreadMarshaller.AddCallback(() =>
                {
                    if (sessionId == mySessionId)
                        UI_LeaveGame();
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
                    int index = msg.IndexOf((char)ProgramConstants.LAN_MESSAGE_SEPARATOR);

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

                if (commands.Count > 0)
                {
                    var commandsSnapshot = commands.ToArray();
                    UIThreadMarshaller.AddCallback(() =>
                    {
                        if (sessionId == mySessionId)
                        {
                            foreach (string cmd in commandsSnapshot)
                                UI_HandleMessageFromServer(cmd);
                        }
                    });
                }

                continue;
            }

            if (leaving)
                break;

            Log.Warning("Reading data from the server failed (0 bytes received)!");
            UIThreadMarshaller.AddCallback(() => { if (sessionId == mySessionId) UI_LeaveGame(); });
            break;
        }
    }

    private void UI_HandleMessageFromServer(string message)
    {
        timeSinceLastReceivedCommand = TimeSpan.Zero;

        foreach (var cmdHandler in playerCommandHandlers)
        {
            if (cmdHandler.Handle(message))
                return;
        }

        Log.Information("Unknown LAN command from the server: " + message);
    }

    private void HandleHostQuit()
    {
        if (!IsHost && !leaving)
            UI_LeaveGame();
    }

    // --- Abstract member implementations ---

    protected override void LeaveGame()
    {
        UI_LeaveGame();
    }

    private void UI_LeaveGame()
    {
        if (leaving)
            return;

        Clear();
        IsEnabled = false;

        base.LeaveGame();
    }

    private void Clear()
    {
        if (IsHost)
        {
            GameBroadcast?.Invoke(this, new GameBroadcastEventArgs("GAMECLOSED"));
            BroadcastMessage(PLAYER_QUIT_COMMAND);
            Players.ForEach(p => CleanUpPlayer((LANPlayerInfo)p));
            Players.Clear();
            listener?.Stop();
        }
        else
        {
            SendMessageToHost(PLAYER_QUIT_COMMAND);
        }

        leaving = true;

        if (client != null && client.Connected)
            client.Close();

        StopUpdateTimer();
    }

    protected override void AddNotice(string message)
    {
        AddChatMessage(message);
    }

    protected override void BroadcastOptions()
    {
        if (Players.Count > 0)
            Players[0].Ready = true;

        var sb = new ExtendedStringBuilder(OPTIONS_COMMAND + " ", true);
        sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;

        sb.Append(SelectedSavedGameIndex);

        foreach (PlayerInfo pInfo in Players)
        {
            sb.Append(pInfo.Name);
            sb.Append(Convert.ToInt32(pInfo.Ready));
            sb.Append(pInfo.IPAddress);
        }

        BroadcastMessage(sb.ToString());
    }

    protected override void HostStartGame()
    {
        BroadcastMessage(GAME_LAUNCH_COMMAND);
    }

    protected override void RequestReadyStatus()
    {
        SendMessageToHost(READY_STATUS_COMMAND);
    }

    protected override void SendChatMessageToNetwork(string message)
    {
        SendMessageToHost(CHAT_COMMAND + " " + chatColorIndex +
            ProgramConstants.LAN_DATA_SEPARATOR + message);

        RaiseMessageSoundRequested();
    }

    protected override void WriteSpawnIniAdditions(IniFile spawnIni)
    {
        // No additional spawn INI settings for LAN loading lobby
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
            untranslatedMapName, untranslatedGameMode, currentState, "LAN",
            Players.Count, SGPlayers.Count,
            "LAN Game", IsHost, resetTimer);
    }

    protected override void HandleGameProcessExited()
    {
        base.HandleGameProcessExited();
        UI_LeaveGame();
    }

    // --- Server command handlers ---

    private void Server_HandleChatMessage(LANPlayerInfo sender, string data)
    {
        string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

        if (parts.Length < 2)
            return;

        int colorIndex = Conversions.IntFromString(parts[0], -1);

        if (colorIndex < 0 || colorIndex >= chatColors.Length)
            return;

        BroadcastMessage(CHAT_COMMAND + " " + sender +
            ProgramConstants.LAN_DATA_SEPARATOR + colorIndex +
            ProgramConstants.LAN_DATA_SEPARATOR + data);
    }

    private void Server_HandleFileHashMessage(LANPlayerInfo sender, string hash)
    {
        if (hash != localFileHash)
            AddNotice(string.Format("{0} - modified files detected! They could be cheating!".L10N("Client:Main:PlayerCheating"), sender.Name));
        sender.HashReceived = true;
    }

    private void Server_HandleReadyRequest(LANPlayerInfo sender)
    {
        if (!sender.Ready)
        {
            sender.Ready = true;
            CopyPlayerDataToUI();
            BroadcastOptions();
        }
    }

    // --- Client command handlers ---

    private void Client_HandleChatMessage(string data)
    {
        string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

        if (parts.Length < 3)
            return;

        string playerName = parts[0];

        int colorIndex = Conversions.IntFromString(parts[1], -1);

        if (colorIndex < 0 || colorIndex >= chatColors.Length)
            return;

        AddChatMessage($"[{playerName}] {parts[2]}");
    }

    private void Client_HandleOptionsMessage(string data)
    {
        if (IsHost)
            return;

        string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);
        const int PLAYER_INFO_PARTS = 3;
        int pCount = (parts.Length - 1) / PLAYER_INFO_PARTS;

        if (pCount * PLAYER_INFO_PARTS + 1 != parts.Length)
            return;

        int savedGameIndex = Conversions.IntFromString(parts[0], -1);
        if (savedGameIndex < 0 || savedGameIndex >= SavedGameNames.Count)
        {
            return;
        }

        SelectedSavedGameIndex = savedGameIndex;

        Players.Clear();

        for (int i = 0; i < pCount; i++)
        {
            int baseIndex = 1 + i * PLAYER_INFO_PARTS;
            string pName = parts[baseIndex];
            bool ready = Conversions.IntFromString(parts[baseIndex + 1], -1) > 0;
            string ipAddress = parts[baseIndex + 2];

            LANPlayerInfo pInfo = new LANPlayerInfo(encoding);
            pInfo.Name = pName;
            pInfo.Ready = ready;
            pInfo.IPAddress = ipAddress;
            Players.Add(pInfo);
        }

        if (Players.Count > 0 && client != null) // Set IP of host
            Players[0].IPAddress = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();

        CopyPlayerDataToUI();
    }

    private void Client_HandleStartCommand()
    {
        started = true;
        PerformLoadGame();
    }

    // --- Helpers ---

    private void BroadcastMessage(string message)
    {
        if (!IsHost)
            return;

        foreach (PlayerInfo pInfo in Players)
        {
            var lpInfo = (LANPlayerInfo)pInfo;
            lpInfo.SendMessage(message);
        }
    }

    private void SendMessageToHost(string message)
    {
        if (client == null || !client.Connected)
            return;

        byte[] buffer = encoding.GetBytes(
            message + ProgramConstants.LAN_MESSAGE_SEPARATOR);

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

    private void BroadcastGame()
    {
        var sb = new ExtendedStringBuilder("GAME ", true);
        sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
        sb.Append(ProgramConstants.LAN_PROTOCOL_REVISION);
        sb.Append(ProgramConstants.GAME_VERSION);
        sb.Append(localGame);
        sb.Append(untranslatedMapName);
        sb.Append(untranslatedGameMode);
        sb.Append(0); // LoadedGameID
        var sbPlayers = new StringBuilder();
        SGPlayers.ForEach(p => sbPlayers.Append(p.Name + ","));
        sbPlayers.Remove(sbPlayers.Length - 1, 1);
        sb.Append(sbPlayers.ToString());
        sb.Append(Convert.ToInt32(started || Players.Count == SGPlayers.Count));
        sb.Append(1); // IsLoadedGame
        sb.Append(string.Empty); // MapHash

        GameBroadcast?.Invoke(this, new GameBroadcastEventArgs(sb.ToString()));
    }
}


