using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.Statistics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DXMainClientViewModel.Domain;
using DXMainClientViewModel.Domain.Multiplayer;
using Rampastring.Tools;

namespace DXMainClientViewModel.Multiplayer.GameLobby;


/// <summary>
/// ViewModel for multiplayer game lobbies (CnCNet and LAN).
/// </summary>
public abstract partial class MultiplayerGameLobbyViewModel : GameLobbyBaseViewModel, IMultiplayerGameLobbyViewModel
{
    private const int MAX_DICE = 10;
    private const int MAX_DIE_SIDES = 100;

    protected readonly Random random;

    // --- Chat box commands ---
    protected readonly List<ChatBoxCommand> chatBoxCommands;

    protected void AddChatBoxCommand(ChatBoxCommand command) => chatBoxCommands.Add(command);

    // --- Network settings ---
    [ObservableProperty]
    private int _frameSendRate;

    [ObservableProperty]
    private int _maxAhead;

    [ObservableProperty]
    private int _protocolVersion;

    // --- Chat ---
    [ObservableProperty]
    private IReadOnlyList<string> _chatMessages = Array.Empty<string>();

    private readonly List<string> chatMessagesList = new();

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    // --- Ready / Lock ---
    [ObservableProperty]
    private bool _isReady;

    [ObservableProperty]
    private bool _isGameLocked;

    [ObservableProperty]
    private bool _isAutoReadyChecked;

    [ObservableProperty]
    private bool _isAutoReadyEnabled;

    // --- Player status indicators ---
    [ObservableProperty]
    private IReadOnlyList<PlayerSlotState> _playerStatuses = Enumerable.Repeat(PlayerSlotState.Empty, MAX_PLAYER_COUNT).ToArray();

    [ObservableProperty]
    private IReadOnlyList<string> _playerStatusTooltips = Enumerable.Repeat(string.Empty, MAX_PLAYER_COUNT).ToArray();

    [ObservableProperty]
    private IReadOnlyList<int> _playerPings = Enumerable.Repeat(-1, MAX_PLAYER_COUNT).ToArray();

    // --- Map list visibility ---
    [ObservableProperty]
    private bool _isMapListVisible = true;

    // --- Lock button ---
    [ObservableProperty]
    private string _lockGameButtonText = "Lock Game".L10N("Client:Main:ButtonLockGame");

    [ObservableProperty]
    private bool _isLockGameButtonVisible = true;

    // --- Save game notification ---
    [ObservableProperty]
    private bool _hasSavedGameWarning;

    // --- Map preview start location selection ---
    [ObservableProperty]
    private bool _isStartLocationSelectionEnabled = true;

    // --- Internal state ---
    private bool suppressAutoReadyChanged;
    private bool locked;
    protected bool Locked
    {
        get => locked;
        set
        {
            if (locked != value)
            {
                locked = value;
                IsGameLocked = value;
                CopyPlayerDataToUI();
                UpdateDiscordPresence();
            }
        }
    }

    protected bool LastMapChangeWasInvalid { get; set; }

    private FileSystemWatcher fsw;
    private bool gameSaved;

    // --- Sound state ---
    private bool isMessageSoundEnabled = true;

    // --- Events ---
    public event EventHandler<NoticeEventArgs> NoticePosted;
    public event Action<string>? SoundPlayRequested;

    protected void RaiseMessageSoundRequested()
    {
        if (isMessageSoundEnabled)
            SoundPlayRequested?.Invoke("message.wav");
    }

    protected void RaiseSoundRequested(string soundName) => SoundPlayRequested?.Invoke(soundName);

    // --- Constructor ---

    protected MultiplayerGameLobbyViewModel(
        MapLoader mapLoader,
        DiscordHandler discordHandler,
        IGameProcessService gameProcessService,
        IUIThreadMarshaller uiThreadMarshaller,
        Random random)
        : base(mapLoader, discordHandler, gameProcessService, uiThreadMarshaller, random)
    {
        this.random = random;

        chatBoxCommands = new List<ChatBoxCommand>
        {
            new ChatBoxCommand("HIDEMAPS", "Hide map list (game host only)".L10N("Client:Main:ChatboxCommandHideMapsHelp"), true,
                _ => IsMapListVisible = false),
            new ChatBoxCommand("SHOWMAPS", "Show map list (game host only)".L10N("Client:Main:ChatboxCommandShowMapsHelp"), true,
                _ => IsMapListVisible = true),
            new ChatBoxCommand("FRAMESENDRATE", string.Format("Change order lag / FrameSendRate (default {0}) (game host only)".L10N("Client:Main:ChatboxCommandFrameSendRateHelpV2"), ClientConfiguration.Instance.DefaultFrameSendRate), true,
                SetFrameSendRate),
            new ChatBoxCommand("MAXAHEAD", string.Format("Change MaxAhead (default {0}) (game host only)".L10N("Client:Main:ChatboxCommandMaxAheadHelpV2"), ClientConfiguration.Instance.DefaultMaxAhead), true,
                SetMaxAhead),
            new ChatBoxCommand("PROTOCOLVERSION", string.Format("Change ProtocolVersion (default {0}) (game host only)".L10N("Client:Main:ChatboxCommandProtocolVersionHelpV2"), ClientConfiguration.Instance.DefaultProtocolVersion), true,
                SetProtocolVersion),
            new ChatBoxCommand("LOADMAP", "Load a custom map with given filename from /Maps/Custom/ folder.".L10N("Client:Main:ChatboxCommandLoadMapHelp"), true,
                LoadCustomMap),
            new ChatBoxCommand("RANDOMSTARTS", "Enables completely random starting locations (Tiberian Sun based games only).".L10N("Client:Main:ChatboxCommandRandomStartsHelp"), true,
                SetStartingLocationClearance),
            new ChatBoxCommand("ROLL", "Roll dice, for example /roll 3d6".L10N("Client:Main:ChatboxCommandRollHelp"), false,
                RollDiceCommand),
            new ChatBoxCommand("SAVEOPTIONS", "Save game option preset so it can be loaded later".L10N("Client:Main:ChatboxCommandSaveOptionsHelp"), false,
                HandleGameOptionPresetSaveCommand),
            new ChatBoxCommand("LOADOPTIONS", "Load game option preset".L10N("Client:Main:ChatboxCommandLoadOptionsHelp"), true,
                HandleGameOptionPresetLoadCommand)
        };
    }

    // --- Initialization ---

    public override void Initialize()
    {
        base.Initialize();

        // Add Spectator side to all player slots (matches original MultiplayerGameLobby.Initialize)
        const string spectatorName = "Spectator";
        string spectatorL10N = spectatorName.L10N("Client:Sides:SpectatorSide");
        foreach (var slot in PlayerSlots)
        {
            var sideOptions = new List<string>(slot.SideOptions) { spectatorL10N };
            slot.SideOptions = sideOptions;
            var sideSelectable = new List<bool>(slot.SideSelectable) { true };
            slot.SideSelectable = sideSelectable;
        }

        FrameSendRate = ClientConfiguration.Instance.DefaultFrameSendRate;
        ProtocolVersion = ClientConfiguration.Instance.DefaultProtocolVersion;
        MaxAhead = ClientConfiguration.Instance.DefaultMaxAhead;

        if (SavedGameManager.AreSavedGamesAvailable())
        {
            fsw = new FileSystemWatcher(SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "Saved Games"), "*.NET");
            fsw.Created += OnSavedGameFileEvent;
            fsw.Changed += OnSavedGameFileEvent;
            fsw.EnableRaisingEvents = false;
        }
        else
        {
            Logger.Log("MultiplayerGameLobby: Saved games are not available!");
        }
    }

    protected override void Clean()
    {
        base.Clean();
        fsw?.Dispose();
        fsw = null;
    }

    // --- Chat ---

    [RelayCommand]
    private void SendChatMessage()
    {
        var text = DraftMessage;
        if (string.IsNullOrEmpty(text))
            return;

        if (text.StartsWith("/"))
        {
            string command;
            string parameters;
            int spaceIndex = text.IndexOf(' ');

            if (spaceIndex == -1)
            {
                command = text.Substring(1).ToUpper();
                parameters = string.Empty;
            }
            else
            {
                command = text.Substring(1, spaceIndex - 1).ToUpper();
                parameters = text.Substring(spaceIndex + 1);
            }

            DraftMessage = string.Empty;

            foreach (var chatBoxCommand in chatBoxCommands)
            {
                if (command == chatBoxCommand.Command)
                {
                    if (!IsHost && chatBoxCommand.HostOnly)
                    {
                        AddNotice(string.Format("/{0} is for game hosts only.".L10N("Client:Main:ChatboxCommandHostOnly"), chatBoxCommand.Command));
                        return;
                    }

                    chatBoxCommand.Action(parameters);
                    return;
                }
            }

            StringBuilder sb = new StringBuilder("To use a command, start your message with /<command>. Possible chat box commands:".L10N("Client:Main:ChatboxCommandTipText") + " ");
            foreach (var chatBoxCommand in chatBoxCommands)
            {
                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
                sb.Append($"{chatBoxCommand.Command}: {chatBoxCommand.Description}");
            }
            AddNotice(sb.ToString());
            return;
        }

        SendChatMessage(text);
        DraftMessage = string.Empty;
    }

    protected abstract void SendChatMessage(string message);

    // --- Chat box command implementations ---

    private void SetFrameSendRate(string value)
    {
        bool success = int.TryParse(value, out int intValue);

        if (!success)
        {
            AddNotice("Command syntax: /FrameSendRate <number>".L10N("Client:Main:ChatboxCommandFrameSendRateSyntax"));
            return;
        }

        FrameSendRate = intValue;
        AddNotice(string.Format("FrameSendRate has been changed to {0}".L10N("Client:Main:FrameSendRateChanged"), intValue));

        OnGameOptionChanged();
        ClearReadyStatuses();
    }

    private void SetMaxAhead(string value)
    {
        bool success = int.TryParse(value, out int intValue);

        if (!success)
        {
            AddNotice("Command syntax: /MaxAhead <number>".L10N("Client:Main:ChatboxCommandMaxAheadSyntax"));
            return;
        }

        MaxAhead = intValue;
        AddNotice(string.Format("MaxAhead has been changed to {0}".L10N("Client:Main:MaxAheadChanged"), intValue));

        OnGameOptionChanged();
        ClearReadyStatuses();
    }

    private void SetProtocolVersion(string value)
    {
        bool success = int.TryParse(value, out int intValue);

        if (!success)
        {
            AddNotice("Command syntax: /ProtocolVersion <number>.".L10N("Client:Main:ChatboxCommandProtocolVersionSyntax"));
            return;
        }

        if (!(intValue == 0 || intValue == 2))
        {
            AddNotice("ProtocolVersion only allows values 0 and 2.".L10N("Client:Main:ChatboxCommandProtocolVersionInvalid"));
            return;
        }

        ProtocolVersion = intValue;
        AddNotice(string.Format("ProtocolVersion has been changed to {0}".L10N("Client:Main:ProtocolVersionChanged"), intValue));

        OnGameOptionChanged();
        ClearReadyStatuses();
    }

    private void SetStartingLocationClearance(string value)
    {
        bool removeStartingLocations = Conversions.BooleanFromString(value, RemoveStartingLocations);

        SetRandomStartingLocations(removeStartingLocations);

        OnGameOptionChanged();
        ClearReadyStatuses();
    }

    protected void SetRandomStartingLocations(bool newValue)
    {
        if (newValue != RemoveStartingLocations)
        {
            RemoveStartingLocations = newValue;
            if (RemoveStartingLocations)
                AddNotice("The game host has enabled completely random starting locations (only works for regular maps).".L10N("Client:Main:HostEnabledRandomStartLocation"));
            else
                AddNotice("The game host has disabled completely random starting locations.".L10N("Client:Main:HostDisabledRandomStartLocation"));
        }
    }

    private void RollDiceCommand(string dieType)
    {
        int dieSides = 6;
        int dieCount = 1;

        if (!string.IsNullOrEmpty(dieType))
        {
            string[] parts = dieType.Split('d');
            if (parts.Length == 2)
            {
                if (!int.TryParse(parts[0], out dieCount) || !int.TryParse(parts[1], out dieSides))
                {
                    AddNotice("Invalid dice specified. Expected format: /roll <die count>d<die sides>".L10N("Client:Main:ChatboxCommandRollInvalidAndSyntax"));
                    return;
                }
            }
        }

        if (dieCount > MAX_DICE || dieCount < 1)
        {
            AddNotice("You can only have between 1 to 10 dice at once.".L10N("Client:Main:ChatboxCommandRollInvalid2"));
            return;
        }

        if (dieSides > MAX_DIE_SIDES || dieSides < 2)
        {
            AddNotice("You can only have between 2 and 100 sides in a die.".L10N("Client:Main:ChatboxCommandRollInvalid3"));
            return;
        }

        int[] results = new int[dieCount];
        for (int i = 0; i < dieCount; i++)
        {
            results[i] = random.Next(1, dieSides + 1);
        }

        BroadcastDiceRoll(dieSides, results);
    }

    protected abstract void BroadcastDiceRoll(int dieSides, int[] results);

    protected void HandleDiceRollResult(string senderName, string result)
    {
        if (string.IsNullOrEmpty(result))
            return;

        string[] parts = result.Split(',');
        if (parts.Length < 2 || parts.Length > MAX_DICE + 1)
            return;

        int[] intArray = Array.ConvertAll(parts, (s) => { return Conversions.IntFromString(s, -1); });
        int dieSides = intArray[0];
        if (dieSides < 1 || dieSides > MAX_DIE_SIDES)
            return;
        int[] results = new int[intArray.Length - 1];
        Array.ConstrainedCopy(intArray, 1, results, 0, results.Length);

        for (int i = 1; i < intArray.Length; i++)
        {
            if (intArray[i] < 1 || intArray[i] > dieSides)
                return;
        }

        PrintDiceRollResult(senderName, dieSides, results);
    }

    protected void PrintDiceRollResult(string senderName, int dieSides, int[] results)
    {
        AddNotice(String.Format("{0} rolled {1}d{2} and got {3}".L10N("Client:Main:PrintDiceRollResult"),
            senderName, results.Length, dieSides, string.Join(", ", results)
        ));
    }

    private void LoadCustomMap(string mapName)
    {
        Map map = MapLoader.LoadCustomMap($"Maps/Custom/{mapName}", out string resultMessage);
        if (map != null)
        {
            AddNotice(resultMessage);
            ListMaps();
        }
        else
        {
            AddNotice(resultMessage, NoticeSeverity.Error);
        }
    }

    private void HandleGameOptionPresetSaveCommand(string presetName)
    {
        string error = AddGameOptionPreset(presetName);
        if (!string.IsNullOrEmpty(error))
            AddNotice(error);
    }

    private void HandleGameOptionPresetLoadCommand(string presetName)
    {
        if (LoadGameOptionPreset(presetName))
            AddNotice("Game option preset loaded succesfully.".L10N("Client:Main:PresetLoaded"));
        else
            AddNotice(string.Format("Preset {0} not found!".L10N("Client:Main:PresetNotFound"), presetName));
    }

    // --- Game lock ---

    [RelayCommand]
    private void LockGame()
    {
        if (locked)
            PerformUnlockGame(true);
        else
            PerformLockGame();
    }

    protected abstract void PerformLockGame();
    protected abstract void PerformUnlockGame(bool manual);

    // --- Auto-ready ---

    partial void OnIsAutoReadyCheckedChanged(bool value)
    {
        if (suppressAutoReadyChanged)
            return;
        UpdateLaunchGameButtonStatus();
        RequestReadyStatus();
    }

    protected void ResetAutoReadyCheckbox()
    {
        suppressAutoReadyChanged = true;
        IsAutoReadyChecked = false;
        suppressAutoReadyChanged = false;
        UpdateLaunchGameButtonStatus();
    }

    protected abstract void RequestReadyStatus();

    // --- Ready toggle ---

    [RelayCommand]
    private void ToggleReady()
    {
        RequestReadyStatus();
    }

    // --- Refresh (host/player UI switching) ---

    protected void Refresh(bool isHost)
    {
        IsHost = isHost;
        Locked = false;
        CopyPlayerDataToUI();
        UpdateMapPreviewBoxEnabledStatus();

        IsMapListVisible = isHost;
        IsAutoReadyEnabled = !isHost;

        LaunchButtonText = IsHost ? BTN_LAUNCH_GAME : BTN_LAUNCH_READY;

        if (IsHost)
        {
            IsLockGameButtonVisible = true;
            LockGameButtonText = "Lock Game".L10N("Client:Main:ButtonLockGame");
            GenerateGameID();
        }
        else
        {
            IsLockGameButtonVisible = false;
        }

        chatMessagesList.Clear();
        chatMessagesList.Add("Type / to view a list of available chat commands.".L10N("Client:Main:ChatCommandTip"));
        ChatMessages = chatMessagesList.ToList();

        if (SavedGameManager.GetSaveGameCount() > 0)
        {
            HasSavedGameWarning = true;
            chatMessagesList.Add("Multiplayer saved games from a previous match have been detected. " +
                "The saved games of the previous match will be deleted if you create new saves during this match.".L10N("Client:Main:SavedGameDetected"));
            ChatMessages = chatMessagesList.ToList();
        }
        else
        {
            HasSavedGameWarning = false;
        }

        LoadDefaultGameModeMap();
    }

    public abstract string GetSwitchName();

    // --- Starting location applied (View calls this when map preview box applies locations) ---

    [RelayCommand]
    private void StartingLocationApplied()
    {
        ClearReadyStatuses();
        CopyPlayerDataToUI();
        BroadcastPlayerOptions();
    }

    // --- Chat message management ---

    protected void AddChatMessage(string message)
    {
        chatMessagesList.Add(message);
        ChatMessages = chatMessagesList.ToList();
        RaiseMessageSoundRequested();
    }

    // --- Player data overrides ---

    protected override void CopyPlayerDataToUI()
    {
        if (Players.Count + AIPlayers.Count > MAX_PLAYER_COUNT)
            return;

        base.CopyPlayerDataToUI();

        // Build status indicators
        var statuses = new PlayerSlotState[MAX_PLAYER_COUNT];
        var tooltips = new string[MAX_PLAYER_COUNT];
        var pings = new int[MAX_PLAYER_COUNT];

        for (int i = 0; i < MAX_PLAYER_COUNT; i++)
        {
            statuses[i] = PlayerSlotState.Empty;
            tooltips[i] = string.Empty;
            pings[i] = -1;
        }

        // Player statuses
        for (int pId = 0; pId < Players.Count; pId++)
        {
            if (Players[pId].IsInGame)
            {
                statuses[pId] = PlayerSlotState.InGame;
                tooltips[pId] = "The player is in game.".L10N("Client:ClientGUI:PlayerIsInGame");
            }
            else if (pId == 0)
            {
                statuses[pId] = Locked ? PlayerSlotState.Ready : PlayerSlotState.NotReady;
                tooltips[pId] = Locked
                    ? "The game room is locked.".L10N("Client:ClientGUI:GameRoomLocked")
                    : "The game room is not locked.".L10N("Client:ClientGUI:GameRoomNotLocked");
            }
            else
            {
                statuses[pId] = Players[pId].Ready ? PlayerSlotState.Ready : PlayerSlotState.NotReady;
                tooltips[pId] = Players[pId].Ready
                    ? "The player is ready.".L10N("Client:ClientGUI:PlayerIsReady")
                    : "The player isn't ready.".L10N("Client:ClientGUI:PlayerIsNotReady");
            }

            pings[pId] = Players[pId].Ping;
        }

        // AI statuses
        for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
        {
            int idx = aiId + Players.Count;
            if (IsPlayerSpectator(AIPlayers[aiId]))
            {
                statuses[idx] = PlayerSlotState.Error;
                tooltips[idx] = "AI players can't be spectators.".L10N("Client:ClientGUI:AICantSpec");
            }
            else
            {
                statuses[idx] = PlayerSlotState.AI;
                tooltips[idx] = "The player is an AI.".L10N("Client:ClientGUI:PlayerIsAI");
            }
        }

        // Empty slot statuses
        for (int i = AIPlayers.Count + Players.Count; i < MAX_PLAYER_COUNT; i++)
        {
            statuses[i] = PlayerSlotState.Empty;
            tooltips[i] = "The slot is empty.".L10N("Client:ClientGUI:SlotEmpty");
        }

        PlayerStatuses = statuses;
        PlayerStatusTooltips = tooltips;
        PlayerPings = pings;

        UpdateMapPreviewBoxEnabledStatus();
    }

    protected override void CopyPlayerDataFromUI()
    {
        if (PlayerUpdatingInProgress)
            return;

        if (IsHost)
        {
            base.CopyPlayerDataFromUI();
            BroadcastPlayerOptions();
            return;
        }

        int mTopIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);

        if (mTopIndex == -1)
            return;

        // Read from PlayerSlots (which the View updates via binding)
        var slot = PlayerSlots[mTopIndex];
        int requestedSide = slot.SelectedSideIndex;
        int requestedColor = slot.SelectedColorIndex;
        int requestedStart = slot.SelectedStartIndex;
        int requestedTeam = slot.SelectedTeamIndex;

        RequestPlayerOptions(requestedSide, requestedColor, requestedStart, requestedTeam);
    }

    protected abstract void BroadcastPlayerOptions();
    protected abstract void BroadcastPlayerExtraOptions();
    protected abstract void RequestPlayerOptions(int side, int color, int start, int team);

    // --- Game option changes ---

    protected override void OnGameOptionChanged()
    {
        base.OnGameOptionChanged();

        ClearReadyStatuses();
        CopyPlayerDataToUI();
    }

    // --- Map change ---

    protected override void ChangeMap(GameModeMap gameModeMap)
    {
        base.ChangeMap(gameModeMap);

        bool resetAutoReady = gameModeMap?.GameMode == null || gameModeMap?.Map == null;

        ClearReadyStatuses(resetAutoReady);

        if ((LastMapChangeWasInvalid || resetAutoReady) && IsAutoReadyChecked)
            RequestReadyStatus();

        LastMapChangeWasInvalid = resetAutoReady;
    }

    // --- Favorite map toggle ---

    protected override void ToggleFavorite()
    {
        base.ToggleFavorite();

        if ((GameModeMap != null && GameModeMap.IsFavorite) || !IsHost)
            return;

        RefreshForFavoriteMapRemoved();
    }

    private void RefreshForFavoriteMapRemoved()
    {
        if (gameModeMapFilter == null || !gameModeMapFilter.GetGameModeMaps().Any())
        {
            LoadDefaultGameModeMap();
            return;
        }

        ListMaps();
        if (IsFavoriteMapsSelected())
            SelectedMapIndex = 0;
    }

    // --- Game launch ---

    protected override void LaunchGame()
    {
        if (!IsHost)
        {
            RequestReadyStatus();
            return;
        }

        if (!Locked)
        {
            LockGameNotification();
            return;
        }

        var teamMappingsError = PlayerExtraOptions?.GetTeamMappingsError();
        if (!string.IsNullOrEmpty(teamMappingsError))
        {
            AddNotice(teamMappingsError);
            return;
        }

        List<int> occupiedColorIds = new List<int>();
        foreach (PlayerInfo player in Players)
        {
            if (occupiedColorIds.Contains(player.ColorId) && player.ColorId > 0)
            {
                SharedColorsNotification();
                return;
            }

            occupiedColorIds.Add(player.ColorId);
        }

        if (AIPlayers.Count(pInfo => IsPlayerSpectator(pInfo)) > 0)
        {
            AISpectatorsNotification();
            return;
        }

        if (GameModeMap.EnforceMaxPlayers)
        {
            foreach (PlayerInfo pInfo in Players)
            {
                if (pInfo.StartingLocation == 0)
                    continue;

                if (Players.Concat(AIPlayers).ToList().Find(
                    p => p.StartingLocation == pInfo.StartingLocation &&
                    p.Name != pInfo.Name) != null)
                {
                    SharedStartingLocationNotification();
                    return;
                }
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                int startingLocation = AIPlayers[aiId].StartingLocation;

                if (startingLocation == 0)
                    continue;

                int index = AIPlayers.FindIndex(aip => aip.StartingLocation == startingLocation);

                if (index > -1 && index != aiId)
                {
                    SharedStartingLocationNotification();
                    return;
                }
            }

            int totalPlayerCount = Players.Count(p => !IsPlayerSpectator(p))
                + AIPlayers.Count;

            int minPlayers = GameModeMap.MinPlayers;
            if (totalPlayerCount < minPlayers)
            {
                InsufficientPlayersNotification();
                return;
            }

            if (GameModeMap.EnforceMaxPlayers && totalPlayerCount > GameModeMap.MaxPlayers)
            {
                TooManyPlayersNotification();
                return;
            }
        }

        int iId = 0;
        foreach (PlayerInfo player in Players)
        {
            iId++;

            if (player.Name == ProgramConstants.PLAYERNAME)
                continue;

            if (!player.HashReceived)
            {
                NotVerifiedNotification(iId - 1);
                return;
            }

            if (player.IsInGame)
            {
                StillInGameNotification(iId - 1);
                return;
            }

            if (!player.Ready)
            {
                GetReadyNotification();
                return;
            }
        }

        HostLaunchGame();
    }

    protected abstract void HostLaunchGame();

    // --- Launch notifications ---

    protected virtual void LockGameNotification() =>
        AddNotice("The host needs to lock the game room before launching the game.".L10N("Client:Main:LockGameNotificationV2"));

    protected virtual void SharedColorsNotification() =>
        AddNotice("Multiple human players cannot share the same color.".L10N("Client:Main:SharedColorsNotification"));

    protected virtual void AISpectatorsNotification() =>
        AddNotice("AI players don't enjoy spectating matches. They want some action!".L10N("Client:Main:AISpectatorsNotification"));

    protected virtual void SharedStartingLocationNotification() =>
        AddNotice("Multiple players cannot share the same starting location on this map.".L10N("Client:Main:SharedStartingLocationNotification"));

    protected virtual void NotVerifiedNotification(int playerIndex)
    {
        if (playerIndex > -1 && playerIndex < Players.Count)
            AddNotice(string.Format("Unable to launch game. Player {0} hasn't been verified.".L10N("Client:Main:NotVerifiedNotification"), Players[playerIndex].Name));
    }

    protected virtual void StillInGameNotification(int playerIndex)
    {
        if (playerIndex > -1 && playerIndex < Players.Count)
            AddNotice(String.Format("Unable to launch game. Player {0} is still playing the game you started previously.".L10N("Client:Main:StillInGameNotification"),
                Players[playerIndex].Name));
    }

    protected virtual void GetReadyNotification()
    {
        AddNotice("The host wants to start the game but cannot because not all players are ready!".L10N("Client:Main:GetReadyNotification"));
        if (!IsHost && !Players.Find(p => p.Name == ProgramConstants.PLAYERNAME).Ready)
            SoundPlayRequested?.Invoke("getready.wav");
    }

    protected virtual void InsufficientPlayersNotification()
    {
        AddNotice(string.Format("Unable to launch game: {0} cannot be played with fewer than {1} players".L10N("Client:Main:InsufficientPlayersNotificationV2"),
            GameModeMap.ToString(), GameModeMap.MinPlayers));
    }

    protected virtual void TooManyPlayersNotification()
    {
        AddNotice(string.Format("Unable to launch game: {0} cannot be played with more than {1} players.".L10N("Client:Main:TooManyPlayersNotificationV2"),
            GameModeMap.ToString(), GameModeMap.MaxPlayers));
    }

    // --- Clear ---

    public virtual void Clear()
    {
        if (!IsHost)
            AIPlayers.Clear();

        Players.Clear();
    }

    // --- Default map rank ---

    protected override int GetDefaultMapRankIndex(GameModeMap gameModeMap)
    {
        if (gameModeMap.MaxPlayers > 3)
            return StatisticsManager.Instance.GetCoopRankForDefaultMap(gameModeMap.Map.UntranslatedName, gameModeMap.MaxPlayers);

        if (StatisticsManager.Instance.HasWonMapInPvP(gameModeMap.Map.UntranslatedName, gameModeMap.GameMode.UntranslatedUIName, gameModeMap.MaxPlayers))
            return 2;

        return -1;
    }

    // --- Network settings in spawn.ini ---

    protected override void WriteSpawnIniAdditions(IniFile iniFile)
    {
        base.WriteSpawnIniAdditions(iniFile);
        iniFile.SetIntValue("Settings", "FrameSendRate", FrameSendRate);
        if (MaxAhead > 0)
            iniFile.SetIntValue("Settings", "MaxAhead", MaxAhead);
        iniFile.SetIntValue("Settings", "Protocol", ProtocolVersion);
    }

    // --- Game process lifecycle ---

    protected override void StartGame()
    {
        if (fsw != null)
            fsw.EnableRaisingEvents = true;

        if (UserINISettings.Instance.StopGameLobbyMessageAudio)
            isMessageSoundEnabled = false;

        base.StartGame();
    }

    protected override void OnGameProcessExited()
    {
        gameSaved = false;

        if (fsw != null)
            fsw.EnableRaisingEvents = false;

        PlayerInfo pInfo = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
        if (pInfo != null)
            pInfo.IsInGame = false;

        if (UserINISettings.Instance.StopGameLobbyMessageAudio)
            isMessageSoundEnabled = true;

        base.OnGameProcessExited();

        if (IsHost)
        {
            GenerateGameID();
            RefreshGameModeFilter();
        }
        else if (IsAutoReadyChecked)
        {
            RequestReadyStatus();
        }
    }

    private void GenerateGameID()
    {
        int i = 0;

        while (i < 20)
        {
            string s = DateTime.Now.Day.ToString() +
                DateTime.Now.Month.ToString() +
                DateTime.Now.Hour.ToString() +
                DateTime.Now.Minute.ToString();

            UniqueGameID = int.Parse(i.ToString() + s);

            if (StatisticsManager.Instance.GetMatchWithGameID(UniqueGameID) == null)
                break;

            i++;
        }
    }

    // --- Saved game file system watcher ---

    private void OnSavedGameFileEvent(object sender, FileSystemEventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action(() => FSWEvent(e)));
    }

    private void FSWEvent(FileSystemEventArgs e)
    {
        Logger.Log("FSW Event: " + e.FullPath);

        if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
        {
            if (!gameSaved)
            {
                bool success = SavedGameManager.InitSavedGames();

                if (!success)
                    return;
            }

            gameSaved = true;

            SavedGameManager.RenameSavedGame();
        }
    }

    // --- Warning ---

    public void AddWarning(string message)
    {
        AddNotice(message, NoticeSeverity.Warning);
    }

    // --- Abstract implementations ---

    protected override bool AllowPlayerOptionsChange() => IsHost;

    protected override bool UpdateLaunchGameButtonStatus()
    {
        if (IsHost)
            CanLaunchGame = base.UpdateLaunchGameButtonStatus() && GameMode != null && Map != null;
        else
            CanLaunchGame = base.UpdateLaunchGameButtonStatus() && !IsAutoReadyChecked;

        return CanLaunchGame;
    }

    protected override void AddNotice(string message)
    {
        NoticePosted?.Invoke(this, new NoticeEventArgs(message, NoticeSeverity.Info));
    }

    protected void AddNotice(string message, NoticeSeverity severity)
    {
        NoticePosted?.Invoke(this, new NoticeEventArgs(message, severity));
    }

    // --- Map preview box enabled status ---

    protected void UpdateMapPreviewBoxEnabledStatus()
    {
        if (Map != null && GameMode != null)
        {
            bool disablestartlocs = GameModeMap.ForceRandomStartLocations || PlayerExtraOptions.IsForceRandomStarts;
            IsStartLocationSelectionEnabled = !disablestartlocs;
        }
        else
        {
            IsStartLocationSelectionEnabled = true;
        }
    }
} 

// checked
