using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using AvClientMvvmContract;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Mvvm;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Domain;
using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Online;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.Statistics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Multiplayer;


/// <summary>
/// Abstract base ViewModel for multiplayer game loading lobbies.
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
    private FileSystemWatcher? fsw;

    // --- Observable state ---
    [ObservableProperty]
    public partial string GameName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MapName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GameMode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HostName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedSavedGameIndex { get; set; }

    [ObservableProperty]
    public partial bool IsHost { get; set; }

    [ObservableProperty]
    public partial bool CanLoadGame { get; set; }

    [ObservableProperty]
    public partial string LoadGameButtonText { get; set; } = "Load Game".L10N("Client:Main:ButtonLoadGame");

    [ObservableProperty]
    public partial string DraftMessage { get; set; } = string.Empty;

    // --- Observable collections ---
    private readonly System.Collections.ObjectModel.ObservableCollection<string> _playerNames = new();
    public IReadOnlyList<string> PlayerNames => _playerNames;

    private readonly System.Collections.ObjectModel.ObservableCollection<string> _chatMessages = new();
    public IReadOnlyList<string> ChatMessages => _chatMessages;

    private readonly System.Collections.ObjectModel.ObservableCollection<string> _savedGameNames = new();
    public IReadOnlyList<string> SavedGameNames => _savedGameNames;

    private readonly CovariantReadOnlyObservableCollectionAdapter<PlayerDisplayInfo, IPlayerDisplayInfo> _playerDisplayInfoAdapter = new();
    public ObservableCollection<PlayerDisplayInfo> PlayerDisplayInfo => _playerDisplayInfoAdapter.Source;
    IReadOnlyList<IPlayerDisplayInfo> IGameLoadingLobbyViewModel.PlayerDisplayInfo => _playerDisplayInfoAdapter.Target;

    // --- Events ---
    public event EventHandler? GameLeft;
    public event EventHandler? GameStarting;
    public event Action<string>? SoundPlayRequested;

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
    private void SendChatMessage()
    {
        if (string.IsNullOrEmpty(DraftMessage))
            return;

        SendChatMessageToNetwork(DraftMessage);
        DraftMessage = string.Empty;
    }

    // --- Lifecycle ---

    public virtual void Initialize()
    {
        if (SavedGameManager.AreSavedGamesAvailable())
        {
            fsw = new FileSystemWatcher(SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "Saved Games"), "*.NET");
            fsw.EnableRaisingEvents = false;
            fsw.Created += OnSavedGameFileEvent;
            fsw.Changed += OnSavedGameFileEvent;
        }
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
        CanLoadGame = true;

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
                MultiplayerColor? mc = sgPlayer.ColorIndex > -1 ? MPColors[sgPlayer.ColorIndex] : null;
                displayInfo.Add(new PlayerDisplayInfo(sgPlayer.Name, false, false, mc?.Color ?? Rgb24Color.White));
            }
            else
            {
                MultiplayerColor? mc = sgPlayer.ColorIndex > -1 ? MPColors[sgPlayer.ColorIndex] : null;
                displayInfo.Add(new PlayerDisplayInfo(sgPlayer.Name, true, pInfo.Ready, mc?.Color ?? Rgb24Color.White));
            }
        }
        PlayerDisplayInfo.Clear(); foreach (var info in displayInfo) PlayerDisplayInfo.Add(info);
    }

    protected void CopyPlayerDataToUI() => UpdatePlayerDisplayInfo();

    // --- Game loading ---

    protected void PerformLoadGame()
    {
        FileInfo spawnFileInfo = SafePath.GetFile(ProgramConstants.GamePath, "spawn.ini");

        spawnFileInfo.Delete();

        File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Saved Games", "spawnSG.ini"), spawnFileInfo.FullName);

        IniFile spawnIni = new IniFile(spawnFileInfo.FullName);

        int sgIndex = (_savedGameNames.Count - 1) - SelectedSavedGameIndex;

        spawnIni.SetStringValue("Settings", "SaveGameName",
            string.Format("SVGM_{0}.NET", sgIndex.ToString("D3")));
        spawnIni.SetBooleanValue("Settings", "LoadSaveGame", true);

        PlayerInfo? localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

        if (localPlayer == null)
            return;

        spawnIni.SetIntValue("Settings", "Port", localPlayer.Port);

        for (int i = 1; i < Players.Count; i++)
        {
            string otherName = spawnIni.GetStringValue("Other" + i, "Name", string.Empty);

            if (string.IsNullOrEmpty(otherName))
                continue;

            PlayerInfo? otherPlayer = Players.Find(p => p.Name == otherName);

            if (otherPlayer == null)
                continue;

            spawnIni.SetStringValue("Other" + i, "Ip", otherPlayer.IPAddress);
            spawnIni.SetIntValue("Other" + i, "Port", otherPlayer.Port);
        }

        WriteSpawnIniAdditions(spawnIni);
        spawnIni.WriteIniFile();

        FileInfo spawnMapFileInfo = SafePath.GetFile(ProgramConstants.GamePath, "spawnmap.ini");
        spawnMapFileInfo.Delete();
        using (var spawnMapStreamWriter = new StreamWriter(spawnMapFileInfo.FullName))
        {
            spawnMapStreamWriter.WriteLine("[Map]");
            spawnMapStreamWriter.WriteLine("Size=0,0,50,50");
            spawnMapStreamWriter.WriteLine("LocalSize=0,0,50,50");
            spawnMapStreamWriter.WriteLine();
        }

        gameLoadTime = DateTime.Now;

        // One-shot subscription: only the lobby that launched the game receives the exit event
        GameProcessService.GameProcessExited += OnGameProcessExited;
        GameProcessService.StartGameProcess();

        if (fsw != null)
            fsw.EnableRaisingEvents = true;

        GameStarting?.Invoke(this, EventArgs.Empty);
        UpdateDiscordPresence(true);
    }

    // --- Game process ---

    private void OnGameProcessExited()
    {
        // One-shot: unsubscribe so only the lobby that started the game handles this
        GameProcessService.GameProcessExited -= OnGameProcessExited;

        // File I/O and Discord RPC on current (threadpool) thread - not on UI thread
        if (fsw != null)
            fsw.EnableRaisingEvents = false;

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

        HandleGameProcessExited();
    }

    protected virtual void HandleGameProcessExited()
    {
    }

    // --- Saved game file system watcher ---

    private void OnSavedGameFileEvent(object sender, FileSystemEventArgs e)
    {
        // File I/O on current (threadpool) thread - no UI marshaling needed
        Log.Information("FSW Event: " + e.FullPath);

        if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
        {
            SavedGameManager.RenameSavedGame();
        }
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
            SoundPlayRequested?.Invoke("getready.wav");
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

    // --- Virtual members ---

    protected virtual void WriteSpawnIniAdditions(IniFile spawnIni)
    {
        // Do nothing by default
    }

    // --- Helpers ---

    protected void ResetDiscordPresence() => DiscordHandler.UpdatePresence();

    protected void RaiseJoinSoundRequested() => SoundPlayRequested?.Invoke("joingame.wav");
    protected void RaiseLeaveSoundRequested() => SoundPlayRequested?.Invoke("leavegame.wav");
    protected void RaiseMessageSoundRequested() => SoundPlayRequested?.Invoke("message.wav");

    protected void AddChatMessage(string message)
    {
        _chatMessages.Add(message);
        SoundPlayRequested?.Invoke("message.wav");
    }

    protected void AddChatMessageWithoutSound(string message) => _chatMessages.Add(message);

    protected virtual string GetIPAddressForPlayer(PlayerInfo pInfo) => "0.0.0.0";

    // --- Cleanup ---

    public virtual void Clean()
    {
        fsw?.Dispose();
        fsw = null;
    }
}

/// <summary>
/// Display info for a player in the loading lobby.
/// </summary>
public class PlayerDisplayInfo : IPlayerDisplayInfo
{
    public string Name { get; }
    public bool IsPresent { get; }
    public bool IsReady { get; }
    public IRgb24Color Color { get; }

    public PlayerDisplayInfo(string name, bool isPresent, bool isReady, IRgb24Color color)
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


