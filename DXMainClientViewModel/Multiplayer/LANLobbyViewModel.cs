using DXMainClientMvvmContract.Multiplayer;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

using ClientCore;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain;
using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Domain.Multiplayer.LAN;
using DXMainClientViewModel.LAN;
using DXMainClientViewModel.Multiplayer.GameLobby;
using DXMainClientViewModel.Services;

using Rampastring.Tools;

using Timer = System.Timers.Timer;
using DXMainClientMvvmContract.ViewServices;

namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the LAN lobby.
/// Contains all business logic from LANLobby.cs except XNA UI rendering.
/// </summary>
public partial class LANLobbyViewModel : ObservableObject, ILANLobbyViewModel
{
    private const double ALIVE_MESSAGE_INTERVAL = 5.0;
    private const double INACTIVITY_REMOVE_TIME = 10.0;
    private const double GAME_INACTIVITY_REMOVE_TIME = 20.0;
    private const double UPDATE_INTERVAL_MS = 1000.0;

    private readonly ILANBroadcastManagerService broadcastManager;
    private readonly ILANPlayerManagerService playerManager;
    private readonly ILANMessageDeduplicatorService messageDeduplicator;
    private readonly IUIThreadMarshaller uiThreadMarshaller;
    private readonly IApplicationLifecycleService applicationLifecycleService;
    private readonly IGameProcessService gameProcessService;
    private readonly GameCollection gameCollection;
    private readonly MapLoader mapLoader;
    private readonly DiscordHandler discordHandler;
    private readonly Random random;
    private readonly Encoding encoding = Encoding.UTF8;

    private string localGame;
    private int localGameIndex;
    private LANColor[] chatColors;
    private TimeSpan timeSinceAliveMessage = TimeSpan.Zero;
    private Timer? updateTimer;

    // Child ViewModels (concrete types for event subscription)
    private LANGameLobbyViewModel lanGameLobby;
    private LANGameLoadingLobbyViewModel lanGameLoadingLobby;
    private LANGameCreationWindowViewModel gameCreationWindow;

    // --- Domain events (for parent coordination) ---
    public event EventHandler? Exited;

    // --- Observable state ---

    [ObservableProperty]
    private int _selectedGameIndex = -1;

    [ObservableProperty]
    private int _selectedColorIndex;

    [ObservableProperty]
    private string _playerName = string.Empty;

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isVisible;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _gameNames = new();
    public IReadOnlyList<string> GameNames => _gameNames;

    private readonly ObservableCollection<string> _playerNames = new();
    public IReadOnlyList<string> PlayerNames => _playerNames;

    private readonly ObservableCollection<string> _chatMessages = new();
    public IReadOnlyList<string> ChatMessages => _chatMessages;

    private readonly ObservableCollection<string> _colorOptions = new();
    public IReadOnlyList<string> ColorOptions => _colorOptions;

    // Internal game list for tracking
    private readonly List<HostedLANGame> hostedGames = new();

    // --- Constructor ---

    public LANLobbyViewModel(
        ILANBroadcastManagerService broadcastManager,
        ILANPlayerManagerService playerManager,
        ILANMessageDeduplicatorService messageDeduplicator,
        IUIThreadMarshaller uiThreadMarshaller,
        IApplicationLifecycleService applicationLifecycleService,
        IGameProcessService gameProcessService,
        GameCollection gameCollection,
        MapLoader mapLoader,
        DiscordHandler discordHandler,
        Random random)
    {
        this.broadcastManager = broadcastManager;
        this.playerManager = playerManager;
        this.messageDeduplicator = messageDeduplicator;
        this.uiThreadMarshaller = uiThreadMarshaller;
        this.applicationLifecycleService = applicationLifecycleService;
        this.gameProcessService = gameProcessService;
        this.gameCollection = gameCollection;
        this.mapLoader = mapLoader;
        this.discordHandler = discordHandler;
        this.random = random;

        this.localGame = ClientConfiguration.Instance.LocalGame;
        this.localGameIndex = gameCollection.GameList.FindIndex(
            g => g.InternalName.ToUpper() == localGame.ToUpper());

        chatColors = new LANColor[]
        {
            new LANColor("Gray".L10N("Client:Main:ColorGray"), 128, 128, 128),
            new LANColor("Metallic".L10N("Client:Main:ColorLightGrayMetallic"), 211, 211, 211),
            new LANColor("Green".L10N("Client:Main:ColorGreen"), 34, 139, 34),
            new LANColor("Lime Green".L10N("Client:Main:ColorLimeGreen"), 50, 205, 50),
            new LANColor("Green Yellow".L10N("Client:Main:ColorGreenYellow"), 173, 255, 47),
            new LANColor("Goldenrod".L10N("Client:Main:ColorGoldenrod"), 255, 193, 37),
            new LANColor("Yellow".L10N("Client:Main:ColorYellow"), 255, 255, 0),
            new LANColor("Orange".L10N("Client:Main:ColorOrange"), 255, 165, 0),
            new LANColor("Red".L10N("Client:Main:ColorRed"), 255, 0, 0),
            new LANColor("Pink".L10N("Client:Main:ColorPink"), 255, 20, 147),
            new LANColor("Purple".L10N("Client:Main:ColorPurple"), 147, 112, 219),
            new LANColor("Sky Blue".L10N("Client:Main:ColorSkyBlue"), 135, 206, 235),
            new LANColor("Blue".L10N("Client:Main:ColorBlue"), 65, 105, 225),
            new LANColor("Brown".L10N("Client:Main:ColorBrown"), 139, 69, 19),
            new LANColor("Teal".L10N("Client:Main:ColorTeal"), 0, 128, 128)
        };

        foreach (LANColor color in chatColors)
            _colorOptions.Add(color.Name);

        applicationLifecycleService.ApplicationClosing += (_, _) => Cleanup();

        broadcastManager.MessageReceived += (sender, e) =>
            uiThreadMarshaller.AddCallback(() => HandleNetworkMessage(e.Data, e.EndPoint));
    }

    // --- Commands ---

    [RelayCommand]
    private void CreateGame()
    {
        if (!ClientConfiguration.Instance.DisableMultiplayerGameLoading)
        {
            gameCreationWindow.Open();
        }
        else
        {
            // Directly create a new game without the creation window
            lanGameLobby.SetUp(true,
                new IPEndPoint(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT), null);
            IsEnabled = false;
        }
    }

    [RelayCommand]
    private void JoinSelectedGame()
    {
        if (SelectedGameIndex < 0 || SelectedGameIndex >= hostedGames.Count)
            return;

        HostedLANGame hg = hostedGames[SelectedGameIndex];

        if (hg.Game.InternalName.ToUpper() != localGame.ToUpper())
        {
            AddChatMessage(string.Format("The selected game is for {0}!".L10N("Client:Main:GameIsOfPurpose"),
                gameCollection.GetGameNameFromInternalName(hg.Game.InternalName)));
            return;
        }

        if (hg.Locked)
        {
            AddChatMessage(string.Format("The game {0} is locked!".L10N("Client:Main:GameLockedWithName"), hg.RoomName));
            return;
        }

        if (hg.IsLoadedGame)
        {
            if (!hg.Players.Contains(ProgramConstants.PLAYERNAME))
            {
                AddChatMessage("You do not exist in the saved game!".L10N("Client:Main:NotInSavedGame"));
                return;
            }
        }
        else
        {
            if (hg.Players.Contains(ProgramConstants.PLAYERNAME))
            {
                AddChatMessage("Your name is already taken in the game.".L10N("Client:Main:NameOccupied"));
                return;
            }
        }

        if (hg.GameVersion != ProgramConstants.GAME_VERSION)
        {
            AddChatMessage("The game host is on a different game version than you. Version incompatibilities may cause issues.".L10N("Client:Main:JoinGameVersionMismatch"));
        }

        AddChatMessage(string.Format("Attempting to join game {0} ...".L10N("Client:Main:AttemptJoin"), hg.RoomName));

        try
        {
            var client = new TcpClient(hg.EndPoint.Address.ToString(), ProgramConstants.LAN_GAME_LOBBY_PORT);

            byte[] buffer;

            if (hg.IsLoadedGame)
            {
                var spawnSGIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));

                int loadedGameId = spawnSGIni.GetIntValue("Settings", "GameID", -1);

                lanGameLoadingLobby.SetUp(false, hg.EndPoint, client, loadedGameId);

                buffer = encoding.GetBytes("JOIN" + ProgramConstants.LAN_DATA_SEPARATOR +
                    ProgramConstants.PLAYERNAME + ProgramConstants.LAN_DATA_SEPARATOR +
                    loadedGameId + ProgramConstants.LAN_MESSAGE_SEPARATOR);

                client.GetStream().Write(buffer, 0, buffer.Length);
                client.GetStream().Flush();

                lanGameLoadingLobby.PostJoin();
            }
            else
            {
                lanGameLobby.SetUp(false, hg.EndPoint, client);

                buffer = encoding.GetBytes("JOIN" + ProgramConstants.LAN_DATA_SEPARATOR +
                    ProgramConstants.PLAYERNAME + ProgramConstants.LAN_MESSAGE_SEPARATOR);

                client.GetStream().Write(buffer, 0, buffer.Length);
                client.GetStream().Flush();

                lanGameLobby.PostJoin();
            }

            IsEnabled = false;
        }
        catch (Exception ex)
        {
            AddChatMessage("Connecting to the game failed! Message:".L10N("Client:Main:ConnectGameFailed") + " " + ex.Message);
        }
    }

    [RelayCommand]
    private void ExitLobby()
    {
        SendMessage("QUIT");
        broadcastManager.Shutdown();
        StopUpdateTimer();
        IsEnabled = false;
        IsVisible = false;
    }

    [RelayCommand]
    private void SendChatMessage()
    {
        if (string.IsNullOrEmpty(DraftMessage))
            return;

        string chatMessage = DraftMessage.Replace((char)01, '?');

        StringBuilder sb = new StringBuilder("CHAT ");
        sb.Append(SelectedColorIndex);
        sb.Append(ProgramConstants.LAN_DATA_SEPARATOR);
        sb.Append(chatMessage);

        SendMessage(sb.ToString());

        DraftMessage = string.Empty;
    }

    [RelayCommand]
    private void RefreshGames()
    {
        // Refresh is handled by the periodic update timer
        // This command can be used for manual refresh
    }

    // --- Lifecycle ---

    public void Initialize()
    {
        PlayerName = ProgramConstants.PLAYERNAME;

        int savedColor = UserINISettings.Instance.LANChatColor;
        SelectedColorIndex = savedColor >= 0 && savedColor < chatColors.Length ? savedColor : 0;

        // Create child ViewModels
        lanGameLobby = new LANGameLobbyViewModel(
            mapLoader,
            discordHandler,
            gameProcessService,
            uiThreadMarshaller,
            applicationLifecycleService,
            random,
            chatColors);

        lanGameLoadingLobby = new LANGameLoadingLobbyViewModel(
            discordHandler,
            gameProcessService,
            uiThreadMarshaller,
            applicationLifecycleService,
            chatColors);

        gameCreationWindow = new LANGameCreationWindowViewModel(
            onNewGameRequested: () =>
            {
                lanGameLobby.SetUp(true,
                    new IPEndPoint(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT), null);
                IsEnabled = false;
            },
            onLoadGameRequested: e =>
            {
                lanGameLoadingLobby.SetUp(true,
                    new IPEndPoint(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT),
                    null, e.LoadedGameID);
                IsEnabled = false;
            });

        // Subscribe to child ViewModel events
        lanGameLobby.GameLeft += LanGameLobby_GameLeft;
        lanGameLobby.GameBroadcast += LanGameLobby_GameBroadcast;

        lanGameLoadingLobby.GameLeft += LanGameLoadingLobby_GameLeft;
        lanGameLoadingLobby.GameBroadcast += LanGameLoadingLobby_GameBroadcast;


        // Set initial chat color
        lanGameLobby.ChatColorIndex = SelectedColorIndex;
        lanGameLoadingLobby.SetChatColorIndex(SelectedColorIndex);

        StartUpdateTimer();
    }

    public void Open()
    {
        playerManager.Clear();
        messageDeduplicator.Clear();
        hostedGames.Clear();
        _gameNames.Clear();

        IsEnabled = true;

        try
        {
            broadcastManager.Initialize();
        }
        catch (Exception ex)
        {
            AddChatMessage("Creating LAN socket failed! Message:".L10N("Client:Main:SocketFailure1") + " " + ex.Message);
            AddChatMessage("Please check your firewall settings.".L10N("Client:Main:SocketFailure2"));
            AddChatMessage("Also make sure that no other application is listening to traffic on UDP ports 1232 - 1234.".L10N("Client:Main:SocketFailure3"));
            return;
        }

        SendAlive();
    }

    public void Close()
    {
        SendMessage("QUIT");
        broadcastManager.Shutdown();
        StopUpdateTimer();
        IsEnabled = false;
    }

    // --- Child ViewModel event handlers ---

    private void LanGameLobby_GameLeft(object? sender, GameLeftEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Message))
            AddChatMessage(e.Message);

        IsEnabled = true;
    }

    private void LanGameLobby_GameBroadcast(object? sender, GameBroadcastEventArgs e)
    {
        SendMessage(e.Message);
    }

    private void LanGameLoadingLobby_GameLeft(object? sender, EventArgs e)
    {
        IsEnabled = true;
    }

    private void LanGameLoadingLobby_GameBroadcast(object? sender, GameBroadcastEventArgs e)
    {
        SendMessage(e.Message);
    }



    // --- Color management ---

    partial void OnSelectedColorIndexChanged(int value)
    {
        if (value >= 0 && value < chatColors.Length)
        {
            UserINISettings.Instance.LANChatColor.Value = value;
            UserINISettings.Instance.SaveSettings();

            // Propagate color to child lobbies
            lanGameLobby.ChatColorIndex = value;
            lanGameLoadingLobby.SetChatColorIndex(value);
        }
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
        uiThreadMarshaller.AddCallback(new Action(UpdateTick));
    }

    private void UpdateTick()
    {
        // Remove inactive players
        var playersCopy = playerManager.GetAllPlayers();
        foreach (var player in playersCopy)
        {
            player.AddToTimeWithoutRefresh(TimeSpan.FromMilliseconds(UPDATE_INTERVAL_MS));

            if (player.TimeWithoutRefresh > TimeSpan.FromSeconds(INACTIVITY_REMOVE_TIME))
                playerManager.RemovePlayer(player.EndPoint);
        }

        // Remove inactive games
        for (int i = hostedGames.Count - 1; i >= 0; i--)
        {
            hostedGames[i].TimeWithoutRefresh += TimeSpan.FromMilliseconds(UPDATE_INTERVAL_MS);
            if (hostedGames[i].TimeWithoutRefresh > TimeSpan.FromSeconds(GAME_INACTIVITY_REMOVE_TIME))
            {
                hostedGames.RemoveAt(i);
                RefreshGameNames();
            }
        }

        // Send ALIVE message periodically
        timeSinceAliveMessage += TimeSpan.FromMilliseconds(UPDATE_INTERVAL_MS);
        if (timeSinceAliveMessage > TimeSpan.FromSeconds(ALIVE_MESSAGE_INTERVAL))
            SendAlive();
    }

    // --- Network message handling ---

    private void SendMessage(string message)
    {
        string wrappedMessage = messageDeduplicator.WrapMessage(message);

        bool sendSucceeded = broadcastManager.SendMessage(wrappedMessage);

        if (!sendSucceeded)
        {
            AddChatMessage("Failed to send LAN broadcast message. The network socket may not be initialized.");
        }
    }

    private void HandleNetworkMessage(string data, IPEndPoint endPoint)
    {
        messageDeduplicator.UnwrapMessage(data, out string payload, out bool isDuplicate);

        if (isDuplicate || string.IsNullOrWhiteSpace(payload))
            return;

        string[] commandAndParams = payload.Split(' ');

        string command = commandAndParams[0];

        string[] parameters;
        {
            int firstSpace = payload.IndexOf(' ');
            parameters = firstSpace >= 0
                ? payload.Substring(firstSpace + 1).Split([ProgramConstants.LAN_DATA_SEPARATOR])
                : [];
        }

        LANLobbyUser? user = playerManager.GetPlayerIfExist(endPoint);

        switch (command)
        {
            case "ALIVE":
                if (parameters.Length < 2)
                    return;

                int gameIndex = Conversions.IntFromString(parameters[0], -1);
                string name = parameters[1];

                if (user == null)
                {
                    user = playerManager.GetOrCreatePlayer(endPoint, name);
                }

                user.ClearTimeWithoutRefresh();

                // Update player names collection
                RefreshPlayerNames();

                break;

            case "CHAT":
                if (user == null)
                    return;

                if (parameters.Length < 2)
                    return;

                int colorIndex = Conversions.IntFromString(parameters[0], -1);

                if (colorIndex < 0 || colorIndex >= chatColors.Length)
                    return;

                AddChatMessage($"[{user.Name}] {parameters[1]}");

                break;

            case "QUIT":
                if (user == null)
                    return;

                playerManager.RemovePlayer(endPoint);
                RefreshPlayerNames();

                break;

            case "GAMECLOSED":
                int closedGameIndex = hostedGames.FindIndex(g => g.EndPoint.Equals(endPoint));
                if (closedGameIndex > -1)
                {
                    hostedGames.RemoveAt(closedGameIndex);
                    RefreshGameNames();
                }

                break;

            case "GAME":
                if (user == null)
                    return;

                HostedLANGame game = new HostedLANGame();
                if (!game.SetDataFromStringArray(gameCollection, parameters))
                    return;

                game.EndPoint = endPoint;

                int existingGameIndex =
                    hostedGames.FindIndex(g => g.EndPoint.Equals(endPoint));

                if (existingGameIndex > -1)
                    hostedGames[existingGameIndex] = game;
                else
                    hostedGames.Add(game);

                RefreshGameNames();

                break;
        }
    }

    private void SendAlive()
    {
        StringBuilder sb = new StringBuilder("ALIVE ");
        sb.Append(localGameIndex);
        sb.Append(ProgramConstants.LAN_DATA_SEPARATOR);
        sb.Append(ProgramConstants.PLAYERNAME);
        SendMessage(sb.ToString());
        timeSinceAliveMessage = TimeSpan.Zero;
    }

    // --- Helpers ---

    private void RefreshPlayerNames()
    {
        _playerNames.Clear();
        foreach (var player in playerManager.GetAllPlayers())
            _playerNames.Add(player.Name);
    }

    private void RefreshGameNames()
    {
        _gameNames.Clear();
        foreach (var game in hostedGames)
            _gameNames.Add(game.RoomName);
    }

    private void AddChatMessage(string message)
    {
        _chatMessages.Add(message);
    }

    private void Cleanup()
    {
        SendMessage("QUIT");
        broadcastManager?.Dispose();
        messageDeduplicator?.Dispose();
        StopUpdateTimer();
    }

    public IReadOnlyList<LANColor> ChatColors => chatColors;
}



