using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Domain;
using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.CnCNet;
using AvClientViewModel.Domain.Multiplayer.LAN;
using AvClientViewModel.LAN;
using AvClientViewModel.Multiplayer.GameLobby;
using AvClientViewModel.Online;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.PlatformShim;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rampastring.Tools;

using Serilog;

using Timer = System.Timers.Timer;

namespace AvClientViewModel.Multiplayer;

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
    private readonly IViewLifecycleService viewLifecycleService;
    private readonly IGameProcessService gameProcessService;
    private readonly GameCollection gameCollection;
    private readonly MapLoader mapLoader;
    private readonly DiscordHandler discordHandler;
    private readonly Random random;
    private readonly Encoding encoding = EncodingExt.UTF8NoBOM;

    private string localGame;
    private int localGameIndex;
    private LANColor[] chatColors;
    private TimeSpan timeSinceAliveMessage = TimeSpan.Zero;
    private Timer? updateTimer;

    // Child ViewModels (concrete types for event subscription)
    private LANGameLobbyViewModel lanGameLobby;
    private LANGameLoadingLobbyViewModel lanGameLoadingLobby;
    private LANGameCreationWindowViewModel gameCreationWindow;

    // Exposed to View via interface
    public ILANGameCreationWindowViewModel? GameCreationWindow => gameCreationWindow;
    public ILANGameLobbyViewModel GameLobby => lanGameLobby;
    public ILANGameLoadingLobbyViewModel GameLoadingLobby => lanGameLoadingLobby;

    // --- Domain events (for parent coordination) ---
    public event EventHandler? Exited;

    // --- Observable state ---

    [ObservableProperty]
    public partial int SelectedGameIndex { get; set; } = -1;

    [ObservableProperty]
    public partial int HoveredGameIndex { get; set; } = -1;

    [ObservableProperty]
    public partial int SelectedColorIndex { get; set; }

    [ObservableProperty]
    public partial string PlayerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DraftMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    // --- Observable collections ---

    private IReadOnlyList<ILANHostedGame> games = Array.Empty<ILANHostedGame>();
    public IReadOnlyList<ILANHostedGame> Games
    {
        get => games;
        set => SetProperty(ref games, value);
    }

    // Player list — replaced wholesale to avoid per-item notifications.
    private IReadOnlyList<string> playerNames = Array.Empty<string>();
    public IReadOnlyList<string> PlayerNames
    {
        get => playerNames;
        set => SetProperty(ref playerNames, value);
    }

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
        IViewLifecycleService viewLifecycleService,
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
        this.viewLifecycleService = viewLifecycleService;
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
            new LANColor("Gray".L10N("Client:Main:ColorGray"), new Rgb24Color(128, 128, 128)),
            new LANColor("Metallic".L10N("Client:Main:ColorLightGrayMetallic"), new Rgb24Color(211, 211, 211)),
            new LANColor("Green".L10N("Client:Main:ColorGreen"), new Rgb24Color(34, 139, 34)),
            new LANColor("Lime Green".L10N("Client:Main:ColorLimeGreen"), new Rgb24Color(50, 205, 50)),
            new LANColor("Green Yellow".L10N("Client:Main:ColorGreenYellow"), new Rgb24Color(173, 255, 47)),
            new LANColor("Goldenrod".L10N("Client:Main:ColorGoldenrod"), new Rgb24Color(255, 193, 37)),
            new LANColor("Yellow".L10N("Client:Main:ColorYellow"), new Rgb24Color(255, 255, 0)),
            new LANColor("Orange".L10N("Client:Main:ColorOrange"), new Rgb24Color(255, 165, 0)),
            new LANColor("Red".L10N("Client:Main:ColorRed"), new Rgb24Color(255, 0, 0)),
            new LANColor("Pink".L10N("Client:Main:ColorPink"), new Rgb24Color(255, 20, 147)),
            new LANColor("Purple".L10N("Client:Main:ColorPurple"), new Rgb24Color(147, 112, 219)),
            new LANColor("Sky Blue".L10N("Client:Main:ColorSkyBlue"), new Rgb24Color(135, 206, 235)),
            new LANColor("Blue".L10N("Client:Main:ColorBlue"), new Rgb24Color(65, 105, 225)),
            new LANColor("Brown".L10N("Client:Main:ColorBrown"), new Rgb24Color(139, 69, 19)),
            new LANColor("Teal".L10N("Client:Main:ColorTeal"), new Rgb24Color(0, 128, 128))
        };

        foreach (LANColor color in chatColors)
            _colorOptions.Add(color.Name);

        viewLifecycleService.Closing += (_, _) => Cleanup();

        broadcastManager.MessageReceived += (sender, e) =>
            uiThreadMarshaller.AddCallback(() => UI_HandleNetworkMessage(e.Data, e.EndPoint));
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
            lanGameLobby.IsVisible = true;
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
                lanGameLoadingLobby.IsVisible = true;

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
                lanGameLobby.IsVisible = true;

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

        // Create child ViewModels
        lanGameLobby = new LANGameLobbyViewModel(
            mapLoader,
            discordHandler,
            gameProcessService,
            uiThreadMarshaller,
            viewLifecycleService,
            random,
            chatColors);
        lanGameLobby.Initialize();

        lanGameLoadingLobby = new LANGameLoadingLobbyViewModel(
            discordHandler,
            gameProcessService,
            uiThreadMarshaller,
            viewLifecycleService,
            chatColors);
        lanGameLoadingLobby.Initialize();

        gameCreationWindow = new LANGameCreationWindowViewModel(
            onNewGameRequested: () =>
            {
                lanGameLobby.SetUp(true,
                    new IPEndPoint(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT), null);
                lanGameLobby.IsVisible = true;
                IsEnabled = false;
            },
            onLoadGameRequested: e =>
            {
                lanGameLoadingLobby.SetUp(true,
                    new IPEndPoint(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT),
                    null, e.LoadedGameID);
                lanGameLoadingLobby.IsVisible = true;
                IsEnabled = false;
            });

        // Subscribe to child ViewModel events
        lanGameLobby.GameLeft += LanGameLobby_GameLeft;
        lanGameLobby.GameBroadcast += LanGameLobby_GameBroadcast;

        lanGameLoadingLobby.GameLeft += LanGameLoadingLobby_GameLeft;
        lanGameLoadingLobby.GameBroadcast += LanGameLoadingLobby_GameBroadcast;

        // Set initial chat color (after child ViewModels are created)
        int savedColor = UserINISettings.Instance.LANChatColor;
        SelectedColorIndex = savedColor >= 0 && savedColor < chatColors.Length ? savedColor : 0;
    }

    public void Open()
    {
        playerManager.Clear();
        messageDeduplicator.Clear();
        hostedGames.Clear();
        Games = Array.Empty<ILANHostedGame>();

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
        StartUpdateTimer();
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

        lanGameLobby.IsVisible = false;
        IsEnabled = true;
    }

    private void LanGameLobby_GameBroadcast(object? sender, GameBroadcastEventArgs e)
    {
        SendMessage(e.Message);
    }

    private void LanGameLoadingLobby_GameLeft(object? sender, EventArgs e)
    {
        lanGameLoadingLobby.IsVisible = false;
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
        uiThreadMarshaller.AddCallback(new Action(UI_UpdateTick));
    }

    private void UI_UpdateTick()
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
                UpdateGameList();
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

    private void UI_HandleNetworkMessage(string data, IPEndPoint endPoint)
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
                    UpdateGameList();
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

                UpdateGameList();

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
        var list = playerManager.GetAllPlayers().Select(p => p.Name).ToList();
        var old = PlayerNames;
        bool changed = old.Count != list.Count;
        if (!changed)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (old[i] != list[i])
                { changed = true; break; }
            }
        }

        if (changed)
        {
            PlayerNames = list;
            Log.Debug("[LAN-PLAYER-REFRESH] old={Old} new={New} changed=True", old.Count, list.Count);
        }
    }

    private void UpdateGameList()
    {
        // Index hosted games by endpoint for fast lookup
        var byEndpoint = new Dictionary<string, ILANHostedGame>();
        foreach (var g in hostedGames)
            byEndpoint[g.EndPoint.ToString()] = g;
        var presentEndpoints = new HashSet<string>(byEndpoint.Keys);

        var oldList = Games;

        // Pass 1: build list preserving positions of still-existing games
        List<ILANHostedGame?> build = new(oldList.Count);
        var gaps = new List<int>();

        for (int i = 0; i < oldList.Count; i++)
        {
            var key = ((HostedLANGame)oldList[i]).EndPoint.ToString();
            if (presentEndpoints.Contains(key))
            {
                build.Add(byEndpoint[key]);
                byEndpoint.Remove(key);
            }
            else
            {
                gaps.Add(build.Count);
                build.Add(null);
            }
        }

        // Remaining new games
        var newGames = byEndpoint.Values.ToList();
        int ngIdx = 0;

        // Pass 2: fill each gap with a new game, or shift from the end
        foreach (int gapPos in gaps)
        {
            if (ngIdx < newGames.Count)
            {
                build[gapPos] = newGames[ngIdx++];
            }
            else
            {
                int last = build.Count - 1;
                while (last > gapPos && build[last] == null)
                    last--;
                if (last <= gapPos)
                    break;

                if ((last == SelectedGameIndex || last == HoveredGameIndex) && last > gapPos + 1)
                    last--;

                build[gapPos] = build[last];
                build.RemoveAt(last);
            }
        }

        // Trim nulls and append leftover new games
        var result = new List<ILANHostedGame>(build.Count);
        foreach (var g in build)
        {
            if (g != null)
                result.Add(g);
        }
        if (ngIdx < newGames.Count)
            result.AddRange(newGames.Skip(ngIdx));

        // Skip assignment if nothing changed (same endpoints, same order).
        var old = Games;
        bool changed = old.Count != result.Count;
        if (!changed)
        {
            for (int i = 0; i < result.Count; i++)
            {
                if (((HostedLANGame)old[i]).EndPoint.ToString()
                    != ((HostedLANGame)result[i]).EndPoint.ToString())
                {
                    changed = true;
                    break;
                }
            }
        }

        string? hoveredBefore = (HoveredGameIndex >= 0 && HoveredGameIndex < old.Count)
            ? ((HostedLANGame)old[HoveredGameIndex]).EndPoint.ToString() : null;
        string? hoveredAfter = null;

        if (changed)
        {
            Games = result;
            int hoverIdx = -1;
            for (int i = 0; i < result.Count; i++)
            {
                if (hoveredBefore != null
                    && ((HostedLANGame)result[i]).EndPoint.ToString() == hoveredBefore)
                { hoverIdx = i; break; }
            }
            hoveredAfter = hoverIdx >= 0
                ? ((HostedLANGame)result[hoverIdx]).EndPoint.ToString() : null;
        }
        else
        {
            hoveredAfter = hoveredBefore;
        }

        Log.Debug("[LAN-GAME-REFRESH] total={Total} old={Old} new={New} changed={Changed} "
            + "hoveredIdx={HoverIdx} hovered={HoverBefore}->{HoverAfter} gaps={Gaps} newGames={NewGms}",
            hostedGames.Count, old.Count, result.Count, changed,
            HoveredGameIndex, hoveredBefore, hoveredAfter, gaps.Count, newGames.Count);
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



