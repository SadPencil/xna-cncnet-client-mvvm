using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.Statistics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain;
using DXMainClientViewModel.Domain.Multiplayer;

using Rampastring.Tools;

namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// Abstract base ViewModel for multiplayer game loading lobbies.
/// Contains all business logic from GameLoadingLobbyBase.cs except XNA UI rendering.
/// </summary>
public abstract partial class GameLoadingLobbyBaseViewModel : ObservableObject, IGameLoadingLobbyViewModel
{
    protected const int MAX_PLAYER_COUNT = 8;

    protected readonly DiscordHandler DiscordHandler;
    protected readonly IGameProcessService GameProcessService;
    protected readonly IUIThreadMarshaller UIThreadMarshaller;

    // --- State ---
    protected List<SavedGamePlayer> SGPlayers = new();
    protected List<PlayerInfo> Players = new();
    protected List<MultiplayerColor> MPColors = new();
    protected bool IsHostState;
    private string loadedGameID = string.Empty;
    private int uniqueGameId;
    private DateTime gameLoadTime;
    private bool isSettingUp;

    // --- Observable state ---
    [ObservableProperty]
    private string _gameName = string.Empty;

    [ObservableProperty]
    private string _mapName = string.Empty;

    [ObservableProperty]
    private string _gameMode = string.Empty;

    [ObservableProperty]
    private string _hostName = string.Empty;

    [ObservableProperty]
    private int _selectedSavedGameIndex;

    [ObservableProperty]
    private bool _isHost;

    [ObservableProperty]
    private bool _canLoadGame;

    [ObservableProperty]
    private string _loadGameButtonText = "Load Game".L10N("Client:Main:ButtonLoadGame");

    // --- Observable collections ---
    private readonly System.Collections.ObjectModel.ObservableCollection<string> _playerNames = new();
    public IReadOnlyList<string> PlayerNames => _playerNames;

    private readonly System.Collections.ObjectModel.ObservableCollection<string> _chatMessages = new();
    public IReadOnlyList<string> ChatMessages => _chatMessages;

    private readonly System.Collections.ObjectModel.ObservableCollection<string> _savedGameNames = new();
    public IReadOnlyList<string> SavedGameNames => _savedGameNames;

    private readonly System.Collections.ObjectModel.ObservableCollection<PlayerDisplayInfo> _playerDisplayInfo = new();
    public IReadOnlyList<PlayerDisplayInfo> PlayerDisplayInfo => _playerDisplayInfo;

    // --- Events ---
    public event EventHandler? GameLeft;
    public event EventHandler? GetReadySoundRequested;
    public event EventHandler? JoinSoundRequested;
    public event EventHandler? LeaveSoundRequested;
    public event EventHandler? MessageSoundRequested;
    public event EventHandler? GameStarting;

    // --- Constructor ---

    protected GameLoadingLobbyBaseViewModel(
        DiscordHandler discordHandler,
        IGameProcessService gameProcessService,
        IUIThreadMarshaller uiThreadMarshaller)
    {
        DiscordHandler = discordHandler;
        GameProcessService = gameProcessService;
        UIThreadMarshaller = uiThreadMarshaller;

        MPColors = MultiplayerColor.LoadColors();

        GameProcessService.GameProcessExited += OnGameProcessExited;
    }

    // --- Commands ---

    [RelayCommand]
    private void LoadGame()
    {
        if (!IsHostState)
        {
            RequestReadyStatus();
            return;
        }

        if (Players.Find(p => !p.Ready) != null)
        {
            GetReadyNotification();
            return;
        }

        if (Players.Count != SGPlayers.Count)
        {
            NotAllPresentNotification();
            return;
        }

        HostStartGame();
    }

    [RelayCommand]
    protected virtual void LeaveGame()
    {
        GameLeft?.Invoke(this, EventArgs.Empty);
        ResetDiscordPresence();
    }

    [RelayCommand]
    private void SendChatMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        SendChatMessageToNetwork(message);
    }

    // --- Lifecycle ---

    public virtual void Initialize()
    {
        // Subclasses should override to set up network subscriptions
    }

    public virtual void Refresh(bool isHost)
    {
        isSettingUp = true;
        IsHostState = isHost;
        IsHost = isHost;

        SGPlayers.Clear();
        Players.Clear();
        _savedGameNames.Clear();
        _chatMessages.Clear();

        LoadGameButtonText = isHost ? "Load Game".L10N("Client:Main:ButtonLoadGame") : "I'm Ready".L10N("Client:Main:ButtonGetReady");

        IniFile spawnSGIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "Saved Games", "spawnSG.ini"));

        loadedGameID = spawnSGIni.GetStringValue("Settings", "GameID", "0");
        MapName = spawnSGIni.GetStringValue("Settings", "UIMapName", string.Empty).L10N($"INI:Maps:{spawnSGIni.GetStringValue("Settings", "MapID", string.Empty)}:Description");
        GameMode = spawnSGIni.GetStringValue("Settings", "UIGameMode", string.Empty).L10N($"INI:GameModes:{spawnSGIni.GetStringValue("Settings", "UIGameMode", string.Empty)}:UIName");

        uniqueGameId = spawnSGIni.GetIntValue("Settings", "GameID", -1);

        int playerCount = spawnSGIni.GetIntValue("Settings", "PlayerCount", 0);

        SavedGamePlayer localPlayer = new SavedGamePlayer();
        localPlayer.Name = ProgramConstants.PLAYERNAME;
        localPlayer.ColorIndex = MPColors.FindIndex(
            c => c.GameColorIndex == spawnSGIni.GetIntValue("Settings", "Color", 0));

        SGPlayers.Add(localPlayer);

        for (int i = 1; i < playerCount; i++)
        {
            string sectionName = "Other" + i;

            SavedGamePlayer sgPlayer = new SavedGamePlayer();
            sgPlayer.Name = spawnSGIni.GetStringValue(sectionName, "Name", "Unknown player".L10N("Client:Main:UnknownPlayer"));
            sgPlayer.ColorIndex = MPColors.FindIndex(
                c => c.GameColorIndex == spawnSGIni.GetIntValue(sectionName, "Color", 0));

            SGPlayers.Add(sgPlayer);
        }

        List<string> timestamps = SavedGameManager.GetSaveGameTimestamps();
        timestamps.Reverse(); // Most recent saved game first
        _savedGameNames.Clear(); foreach (var ts in timestamps) _savedGameNames.Add(ts);

        if (_savedGameNames.Count > 0)
            SelectedSavedGameIndex = 0;

        UpdatePlayerDisplayInfo();
        isSettingUp = false;
    }

    // --- Player display ---

    protected void UpdatePlayerDisplayInfo()
    {
        var displayInfo = new List<PlayerDisplayInfo>();
        for (int i = 0; i < SGPlayers.Count; i++)
        {
            SavedGamePlayer sgPlayer = SGPlayers[i];
            PlayerInfo? pInfo = Players.Find(p => p.Name == sgPlayer.Name);

            if (pInfo == null)
            {
                displayInfo.Add(new PlayerDisplayInfo(sgPlayer.Name, false, false,
                    sgPlayer.ColorIndex > -1 ? System.Drawing.Color.FromArgb(MPColors[sgPlayer.ColorIndex].R, MPColors[sgPlayer.ColorIndex].G, MPColors[sgPlayer.ColorIndex].B) : System.Drawing.Color.White));
            }
            else
            {
                displayInfo.Add(new PlayerDisplayInfo(sgPlayer.Name, true, pInfo.Ready,
                    sgPlayer.ColorIndex > -1 ? System.Drawing.Color.FromArgb(MPColors[sgPlayer.ColorIndex].R, MPColors[sgPlayer.ColorIndex].G, MPColors[sgPlayer.ColorIndex].B) : System.Drawing.Color.White));
            }
        }
        _playerDisplayInfo.Clear(); foreach (var info in displayInfo) _playerDisplayInfo.Add(info);
    }

    protected void CopyPlayerDataToUI() => UpdatePlayerDisplayInfo();

    // --- Game process ---

    protected void StartGameProcess()
    {
        gameLoadTime = DateTime.Now;
        GameStarting?.Invoke(this, EventArgs.Empty);
    }

    private void OnGameProcessExited()
    {
        UIThreadMarshaller.AddCallback(new Action(HandleGameProcessExited));
    }

    protected virtual void HandleGameProcessExited()
    {
        var matchStatistics = StatisticsManager.Instance.GetMatchWithGameID(uniqueGameId);

        if (matchStatistics != null)
        {
            int newLength = matchStatistics.LengthInSeconds +
                (int)(DateTime.Now - gameLoadTime).TotalSeconds;

            matchStatistics.ParseStatistics(ProgramConstants.GamePath,
                ClientConfiguration.Instance.LocalGame, true);

            matchStatistics.LengthInSeconds = newLength;

            StatisticsManager.Instance.SaveDatabase();
        }
        UpdateDiscordPresence(true);
    }

    // --- Saved game selection ---

    partial void OnSelectedSavedGameIndexChanged(int value)
    {
        if (!IsHostState)
            return;

        for (int i = 1; i < Players.Count; i++)
            Players[i].Ready = false;

        UpdatePlayerDisplayInfo();

        if (!isSettingUp)
            BroadcastOptions();
        UpdateDiscordPresence();
    }

    // --- Notifications ---

    protected virtual void GetReadyNotification()
    {
        AddNotice("The game host wants to load the game but cannot because not all players are ready!".L10N("Client:Main:GetReadyPlease"));

        if (!IsHostState && !Players.Find(p => p.Name == ProgramConstants.PLAYERNAME).Ready)
            GetReadySoundRequested?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void NotAllPresentNotification() =>
        AddNotice("You cannot load the game before all players are present.".L10N("Client:Main:NotAllPresent"));

    // --- Abstract members ---

    protected abstract void RequestReadyStatus();
    protected abstract void HostStartGame();
    protected abstract void BroadcastOptions();
    protected abstract void SendChatMessageToNetwork(string message);
    protected abstract void AddNotice(string message);
    protected abstract void UpdateDiscordPresence(bool resetTimer = false);
    protected abstract void WriteSpawnIniAdditions(IniFile spawnIni);

    // --- Helpers ---

    protected void ResetDiscordPresence() => DiscordHandler.UpdatePresence();

    protected void RaiseJoinSoundRequested() => JoinSoundRequested?.Invoke(this, EventArgs.Empty);
    protected void RaiseLeaveSoundRequested() => LeaveSoundRequested?.Invoke(this, EventArgs.Empty);
    protected void RaiseMessageSoundRequested() => MessageSoundRequested?.Invoke(this, EventArgs.Empty);

    protected void AddChatMessage(string message)
    {
        _chatMessages.Add(message);
        MessageSoundRequested?.Invoke(this, EventArgs.Empty);
    }

    protected virtual string GetIPAddressForPlayer(PlayerInfo pInfo) => "0.0.0.0";
}

/// <summary>
/// Display info for a player in the loading lobby.
/// </summary>
public class PlayerDisplayInfo
{
    public string Name { get; }
    public bool IsPresent { get; }
    public bool IsReady { get; }
    public System.Drawing.Color Color { get; }

    public PlayerDisplayInfo(string name, bool isPresent, bool isReady, System.Drawing.Color color)
    {
        Name = name;
        IsPresent = isPresent;
        IsReady = isReady;
        Color = color;
    }

    public string DisplayName => IsPresent
        ? (IsReady ? Name : Name + " " + "(Not Ready)".L10N("Client:Main:NotReadySuffix"))
        : Name + " " + "(Not present)".L10N("Client:Main:NotPresentSuffix");
}
