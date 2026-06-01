using DXMainClientMvvmContract.Multiplayer.GameLobby;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using DXMainClientMvvmContract.ViewServices;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// ViewModel for the Skirmish game lobby.
/// Contains all business logic from SkirmishLobby.cs except XNA UI rendering.
/// </summary>
public partial class SkirmishLobbyViewModel : GameLobbyBaseViewModel, ISkirmishLobbyViewModel
{
    private const string SETTINGS_PATH = "Client/SkirmishSettings.ini";

    private readonly Random random;

    // --- Observable state ---

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private bool _showPlayerNamesInGame;

    [ObservableProperty]
    private bool _isErrorVisible;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isNoticeVisible;

    [ObservableProperty]
    private string _noticeMessage = string.Empty;

    // --- Domain events (on concrete class only, not on interface) ---

    public event EventHandler? Exited;

    // --- Constructor ---

    public SkirmishLobbyViewModel(
        MapLoader mapLoader,
        DiscordHandler discordHandler,
        IGameProcessService gameProcessService,
        IUIThreadMarshaller uiThreadMarshaller,
        Random random)
        : base(mapLoader, discordHandler, gameProcessService, uiThreadMarshaller, random)
    {
        this.random = random;
    }

    protected override int MaxPlayerCount => MAX_PLAYER_COUNT;

    protected override bool IsMultiplayer => false;

    public override bool IsHost { get; set; } = true;

    protected override void AddNotice(string message)
    {
        NoticeMessage = message;
        IsNoticeVisible = true;
    }

    // --- Lifecycle ---

    public void Initialize()
    {
        RandomSeed = random.Next();
        LoadSettings();
        CopyPlayerDataToUI();
        ProgramConstants.PlayerNameChanged += ProgramConstants_PlayerNameChanged;
    }

    public void Open()
    {
        UpdateDiscordPresence(true);
    }

    // --- Commands ---

    [RelayCommand]
    private void AddAiPlayer()
    {
        if (AIPlayers.Count >= MAX_PLAYER_COUNT - 1)
            return;

        int aiIndex = AIPlayers.Count;
        if (aiIndex >= ProgramConstants.AI_PLAYER_NAMES.Count)
            aiIndex = 0;

        var aiPlayer = new PlayerInfo(ProgramConstants.AI_PLAYER_NAMES[aiIndex], 0, 0, 0, 0);
        aiPlayer.IsAI = true;
        aiPlayer.AILevel = 0;
        AIPlayers.Add(aiPlayer);
        CopyPlayerDataToUI();
    }

    [RelayCommand]
    private void RemoveSelectedPlayer()
    {
        // Remove the last AI player
        if (AIPlayers.Count > 0)
        {
            AIPlayers.RemoveAt(AIPlayers.Count - 1);
            CopyPlayerDataToUI();
        }
    }

    [RelayCommand]
    private void DismissError()
    {
        IsErrorVisible = false;
    }

    [RelayCommand]
    private void DismissNotice()
    {
        IsNoticeVisible = false;
    }

    [RelayCommand]
    private void RandomizeSides()
    {
        int sideCount = SideCount;
        if (sideCount == 0)
            return;

        foreach (var p in Players)
            p.SideId = random.Next(sideCount);
        foreach (var p in AIPlayers)
            p.SideId = random.Next(sideCount);

        CopyPlayerDataToUI();
    }

    protected override void LaunchGame()
    {
        string? error = CheckGameValidity();

        if (error == null)
        {
            SaveSettings();
            StartGame();
            return;
        }

        ErrorMessage = error;
        IsErrorVisible = true;
    }

    protected override void LeaveGame()
    {
        IsVisible = false;
        ResetDiscordPresence();
    }

    // --- Game validation ---

    private string? CheckGameValidity()
    {
        int spectatorSideIndex = SideCount + RandomSelectorCount;
        int totalPlayerCount = Players.Count(p => p.SideId < spectatorSideIndex)
            + AIPlayers.Count;

        if (GameModeMap != null && GameModeMap.MultiplayerOnly)
        {
            return string.Format("{0} can only be played on CnCNet and LAN.".L10N("Client:Main:GameModeMultiplayerOnly"),
                GameModeMap.ToString());
        }

        if (GameModeMap != null && totalPlayerCount < GameModeMap.MinPlayers)
        {
            return string.Format("{0} cannot be played with less than {1} players.".L10N("Client:Main:GameModeInsufficientPlayers"),
                GameModeMap.ToString(), GameModeMap.MinPlayers);
        }

        if (GameModeMap != null && GameModeMap.EnforceMaxPlayers)
        {
            if (totalPlayerCount > GameModeMap.MaxPlayers)
            {
                return string.Format("{0} cannot be played with more than {1} players.".L10N("Client:Main:TooManyPlayers"),
                    GameModeMap.ToString(), GameModeMap.MaxPlayers);
            }

            IEnumerable<PlayerInfo> concatList = Players.Concat(AIPlayers);

            foreach (PlayerInfo pInfo in concatList)
            {
                if (pInfo.StartingLocation == 0)
                    continue;

                if (concatList.Count(p => p.StartingLocation == pInfo.StartingLocation) > 1)
                {
                    return "Multiple players cannot share the same starting location on the selected map.".L10N("Client:Main:StartLocationOccupied");
                }
            }
        }

        if (GameModeMap != null && GameModeMap.IsCoop && Players[0].SideId == spectatorSideIndex)
        {
            return "Co-op missions cannot be spectated. You'll have to show a bit more effort to cheat here.".L10N("Client:Main:CoOpMissionSpectatorPrompt");
        }

        var teamMappingsError = PlayerExtraOptions?.GetTeamMappingsError();
        if (!string.IsNullOrEmpty(teamMappingsError))
            return teamMappingsError;

        return null;
    }

    // --- Settings persistence ---

    private void SaveSettings()
    {
        try
        {
            FileInfo settingsFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH);

            // Delete the file so we don't keep potential extra AI players that already exist in the file
            settingsFileInfo.Delete();

            var skirmishSettingsIni = new IniFile(settingsFileInfo.FullName);

            skirmishSettingsIni.SetStringValue("Player", "Info", Players[0].ToString());

            for (int i = 0; i < AIPlayers.Count; i++)
            {
                skirmishSettingsIni.SetStringValue("AIPlayers", i.ToString(), AIPlayers[i].ToString());
            }

            skirmishSettingsIni.SetStringValue("Settings", "Map", Map?.SHA1 ?? string.Empty);
            skirmishSettingsIni.SetStringValue("Settings", "GameModeMapFilter",
                SelectedGameModeFilterIndex >= 0 && SelectedGameModeFilterIndex < GameModeFilterOptions.Count
                    ? GameModeFilterOptions[SelectedGameModeFilterIndex] : string.Empty);

            if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
            {
                foreach (var dd in DropDowns.Cast<GameOptionDropDown>())
                {
                    skirmishSettingsIni.SetStringValue("GameOptions", dd.Name, dd.UserSelectedIndex + "");
                }

                foreach (var cb in CheckBoxes)
                {
                    skirmishSettingsIni.SetStringValue("GameOptions", cb.Name, cb.IsChecked.ToString());
                }
            }

            skirmishSettingsIni.WriteIniFile();
        }
        catch (Exception ex)
        {
            Logger.Log("Saving skirmish settings failed! Reason: " + ex.ToString());
        }
    }

    private void LoadSettings()
    {
        if (!SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH).Exists)
        {
            InitDefaultSettings();
            return;
        }

        var skirmishSettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, SETTINGS_PATH));

        string gameModeMapFilterName = skirmishSettingsIni.GetStringValue("Settings", "GameModeMapFilter", string.Empty);
        if (string.IsNullOrEmpty(gameModeMapFilterName))
            gameModeMapFilterName = skirmishSettingsIni.GetStringValue("Settings", "GameMode", string.Empty); // legacy

        // Find the filter index in GameModeFilterOptions
        int filterIndex = GameModeFilterOptions.ToList().FindIndex(o => o == gameModeMapFilterName);
        if (filterIndex < 0)
            filterIndex = GetDefaultGameModeMapFilterIndex();

        SelectedGameModeFilterIndex = filterIndex;

        // Try to select the saved map by SHA1
        string mapSHA1 = skirmishSettingsIni.GetStringValue("Settings", "Map", string.Empty);
        if (!string.IsNullOrEmpty(mapSHA1))
        {
            var sortedMaps = GetSortedGameModeMaps();
            int mapIndex = sortedMaps.FindIndex(gmm => gmm.Map.SHA1 == mapSHA1);
            if (mapIndex > -1)
                SelectedMapIndex = mapIndex;
        }

        var player = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("Player", "Info", string.Empty));

        if (player == null)
        {
            Logger.Log("Failed to load human player information from skirmish settings!");
            InitDefaultSettings();
            return;
        }

        CheckLoadedPlayerVariableBounds(player);

        player.Name = ProgramConstants.PLAYERNAME;
        Players.Add(player);

        List<string>? keys = skirmishSettingsIni.GetSectionKeys("AIPlayers");

        if (keys == null)
        {
            keys = new List<string>();
        }

        bool AIAllowed = GameModeMap != null && !GameModeMap.HumanPlayersOnly;
        foreach (string key in keys)
        {
            if (!AIAllowed) break;
            var aiPlayer = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("AIPlayers", key, string.Empty));

            if (aiPlayer == null)
            {
                Logger.Log("Failed to load AI player information from skirmish settings!");
                InitDefaultSettings();
                return;
            }

            CheckLoadedPlayerVariableBounds(aiPlayer, true);

            if (AIPlayers.Count < MAX_PLAYER_COUNT - 1)
                AIPlayers.Add(aiPlayer);
        }

        if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
        {
            foreach (var dd in DropDowns.Cast<GameOptionDropDown>())
            {
                if (GameMode != null)
                {
                    int gameModeMatchIndex = GameMode.ForcedDropDownValues.FindIndex(p => p.Key.Equals(dd.Name));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Dropdown '" + dd.Name + "' has forced value in gamemode - saved settings ignored.");
                        continue;
                    }
                }

                if (Map != null)
                {
                    int gameModeMatchIndex = Map.ForcedDropDownValues.FindIndex(p => p.Key.Equals(dd.Name));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Dropdown '" + dd.Name + "' has forced value in map - saved settings ignored.");
                        continue;
                    }
                }

                int savedIndex = skirmishSettingsIni.GetIntValue("GameOptions", dd.Name, dd.UserSelectedIndex);
                dd.UserSelectedIndex = savedIndex;

                if (savedIndex > -1 && savedIndex < dd.Items.Count)
                    dd.SelectedIndex = savedIndex;
            }

            foreach (var cb in CheckBoxes)
            {
                if (GameMode != null)
                {
                    int gameModeMatchIndex = GameMode.ForcedCheckBoxValues.FindIndex(p => p.Key.Equals(cb.Name));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Checkbox '" + cb.Name + "' has forced value in gamemode - saved settings ignored.");
                        continue;
                    }
                }

                if (Map != null)
                {
                    int gameModeMatchIndex = Map.ForcedCheckBoxValues.FindIndex(p => p.Key.Equals(cb.Name));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Checkbox '" + cb.Name + "' has forced value in map - saved settings ignored.");
                        continue;
                    }
                }

                ((GameOptionCheckBox)cb).IsChecked = skirmishSettingsIni.GetBooleanValue("GameOptions", cb.Name, cb.IsChecked);
            }
        }
    }

    private void CheckLoadedPlayerVariableBounds(PlayerInfo pInfo, bool isAIPlayer = false)
    {
        int sideCount = SideCount + RandomSelectorCount;
        if (isAIPlayer) sideCount--;

        if (pInfo.SideId < 0 || pInfo.SideId > sideCount)
        {
            pInfo.SideId = 0;
        }

        if (pInfo.ColorId < 0 || pInfo.ColorId > MPColors.Count)
        {
            pInfo.ColorId = 0;
        }

        if (pInfo.TeamId < 0 || pInfo.TeamId >= ProgramConstants.TEAMS.Count + 1 ||
            (!(GameModeMap?.IsCoop ?? false)) && (GameModeMap?.ForceNoTeams ?? false))
        {
            pInfo.TeamId = 0;
        }

        if (pInfo.StartingLocation < 0 || pInfo.StartingLocation > MAX_PLAYER_COUNT ||
            (GameModeMap?.ForceRandomStartLocations ?? false))
        {
            pInfo.StartingLocation = 0;
        }
    }

    private void InitDefaultSettings()
    {
        Players.Clear();
        AIPlayers.Clear();

        Players.Add(new PlayerInfo(ProgramConstants.PLAYERNAME, 0, 0, 0, 0));
        PlayerInfo aiPlayer = new PlayerInfo(ProgramConstants.AI_PLAYER_NAMES[0], 0, 0, 0, 0);
        aiPlayer.IsAI = true;
        aiPlayer.AILevel = 0;
        AIPlayers.Add(aiPlayer);

        LoadDefaultGameModeMap();
    }

    // --- Event handlers ---

    private void ProgramConstants_PlayerNameChanged(object? sender, EventArgs e)
    {
        Players[0].Name = ProgramConstants.PLAYERNAME;
        CopyPlayerDataToUI();
    }

    // --- Overrides ---

    protected override void ToggleFavorite()
    {
        base.ToggleFavorite();
        RefreshMapSelectionUI();
    }

    protected override void OnGameProcessExited()
    {
        base.OnGameProcessExited();
        RandomSeed = random.Next();
    }

    protected override void CopyPlayerDataFromUI()
    {
        base.CopyPlayerDataFromUI();
        UpdateDiscordPresence();
    }

    protected override void UpdateDiscordPresence(bool resetTimer = false)
    {
        if (DiscordHandler == null || Map == null || GameMode == null)
            return;

        int playerIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);
        if (playerIndex >= MAX_PLAYER_COUNT || playerIndex < 0)
            return;

        PlayerInfo player = Players[playerIndex];
        string side = "";
        string[] sides = ClientConfiguration.Instance.Sides.Split(',');
        if (player.SideId > 0 && player.SideId <= sides.Length)
            side = sides[player.SideId - 1];

        string currentState = ProgramConstants.IsInGame ? "In Game" : "Setting Up";

        DiscordHandler.UpdatePresence(
            Map.UntranslatedName, GameMode.UntranslatedUIName, currentState, side, resetTimer);
    }

    protected override void OnPlayerExtraOptionsChanged()
    {
        base.OnPlayerExtraOptionsChanged();
        CopyPlayerDataToUI();
    }

    protected override bool AllowPlayerOptionsChange() => true;

    protected override int GetDefaultMapRankIndex(GameModeMap gameModeMap)
    {
        return StatisticsManager.Instance.GetSkirmishRankForDefaultMap(gameModeMap.Map.UntranslatedName, gameModeMap.MaxPlayers);
    }
}



