using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.Statistics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DXMainClientViewModel.Domain;
using DXMainClientViewModel.Domain.Multiplayer;
using Rampastring.Tools;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Abstract base ViewModel for all game lobbies (Skirmish, LAN, CnCNet).
/// Contains the common business logic for parsing game options and handling player info.
/// </summary>
public abstract partial class GameLobbyBaseViewModel : ObservableObject, IGameLobbyViewModel
{
    protected const int MAX_PLAYER_COUNT = 8;

    protected readonly string BTN_LAUNCH_GAME = "Launch Game".L10N("Client:Main:ButtonLaunchGame");
    protected readonly string BTN_LAUNCH_READY = "I'm Ready".L10N("Client:Main:ButtonIAmReady");
    protected readonly string BTN_LAUNCH_NOT_READY = "Not Ready".L10N("Client:Main:ButtonNotReady");
    private readonly string FavoriteMapsLabel = "Favorites".L10N("Client:Main:Favorites");

    private readonly Random random;

    protected MapLoader MapLoader { get; }
    protected DiscordHandler DiscordHandler { get; }
    protected IGameProcessService GameProcessService { get; }
    protected IUIThreadMarshaller UIThreadMarshaller { get; }
    protected IReadOnlyGameModeMapCollection GameModeMaps => MapLoader.GameModeMaps;

    protected List<MultiplayerColor> MPColors;
    protected List<PlayerInfo> Players = new();
    protected List<PlayerInfo> AIPlayers = new();
    protected List<int[]> RandomSelectors = new();
    protected int SideCount { get; private set; }
    protected int RandomSelectorCount { get; private set; } = 1;
    protected int RandomSeed { get; set; }
    protected int UniqueGameID { get; set; }
    protected bool RemoveStartingLocations { get; set; }
    protected IniFile GameOptionsIni { get; private set; }
    protected virtual int MaxPlayerCount => MAX_PLAYER_COUNT;

    private MatchStatistics matchStatistics;
    private bool disableGameOptionUpdateBroadcast;
    private GameModeMapFilter gameModeMapFilter;
    private bool searchAllGameModes;

    // Internal game option lists (IGameSessionSetting)
    public List<IGameSessionSetting> CheckBoxSettings { get; } = new();
    public List<IGameSessionSetting> DropDownSettings { get; } = new();

    // --- Observable state for View binding ---

    [ObservableProperty]
    private string _mapName = "Map: Unknown".L10N("Client:Main:MapUnknown");

    [ObservableProperty]
    private string _mapAuthor = "By Unknown Author".L10N("Client:Main:AuthorByUnknown");

    [ObservableProperty]
    private string _gameModeName = "Game mode: Unknown".L10N("Client:Main:GameModeUnknown");

    [ObservableProperty]
    private string _mapSize = "Size: Not available".L10N("Client:Main:MapSizeUnknown");

    [ObservableProperty]
    private string _gameName = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<MapListItem> _mapListItems = Array.Empty<MapListItem>();

    [ObservableProperty]
    private int _selectedMapIndex = -1;

    [ObservableProperty]
    private IReadOnlyList<string> _gameModeFilterOptions = Array.Empty<string>();

    [ObservableProperty]
    private int _selectedGameModeFilterIndex;

    [ObservableProperty]
    private string _mapSearchText = string.Empty;

    [ObservableProperty]
    private string _mapListTooltipText = string.Empty;

    [ObservableProperty]
    private int _sortDirectionState;

    [ObservableProperty]
    private bool _isMapSortButtonVisible = true;

    [ObservableProperty]
    private bool _isMapSortButtonEnabled = true;

    [ObservableProperty]
    private IReadOnlyList<PlayerSlotObservable> _playerSlots = Array.Empty<PlayerSlotObservable>();

    [ObservableProperty]
    private IReadOnlyList<GameOptionCheckBox> _checkBoxes = Array.Empty<GameOptionCheckBox>();

    [ObservableProperty]
    private IReadOnlyList<GameOptionDropDown> _dropDowns = Array.Empty<GameOptionDropDown>();

    [ObservableProperty]
    private int _launchButtonRank;

    [ObservableProperty]
    private string _launchButtonText = "Launch Game".L10N("Client:Main:ButtonLaunchGame");

    [ObservableProperty]
    private bool _canLaunchGame = true;

    [ObservableProperty]
    private IReadOnlyList<string> _playerNames = Array.Empty<string>();

    [ObservableProperty]
    private int _selectedPlayerIndex;

    // --- Player updating guard ---
    protected bool PlayerUpdatingInProgress { get; set; }

    // --- Player extra options (force random sides/colors/starts, force no teams) ---
    private PlayerExtraOptions playerExtraOptions = new();

    public PlayerExtraOptions PlayerExtraOptions
    {
        get => playerExtraOptions;
        set
        {
            playerExtraOptions = value;
            CopyPlayerDataToUI();
        }
    }

    // --- Currently selected GameModeMap (internal, not directly exposed) ---
    private GameModeMap _gameModeMap;
    protected GameModeMap GameModeMap
    {
        get => _gameModeMap;
        set
        {
            var old = _gameModeMap;
            _gameModeMap = value;
            if (value != null && old != value)
                UpdateDiscordPresence();
        }
    }

    protected Map Map => GameModeMap?.Map;
    protected GameMode GameMode => GameModeMap?.GameMode;

    // --- Constructor ---

    protected GameLobbyBaseViewModel(
        MapLoader mapLoader,
        DiscordHandler discordHandler,
        IGameProcessService gameProcessService,
        IUIThreadMarshaller uiThreadMarshaller,
        Random random)
    {
        MapLoader = mapLoader;
        DiscordHandler = discordHandler;
        GameProcessService = gameProcessService;
        UIThreadMarshaller = uiThreadMarshaller;
        this.random = random;
    }

    // --- Lifecycle ---

    public virtual void Initialize()
    {
        MPColors = MultiplayerColor.LoadColors();

        GameOptionsIni = new IniFile(SafePath.CombineFilePath(
            ProgramConstants.GetBaseResourcePath(), ClientConfiguration.GAME_OPTIONS));

        string[] sides = ClientConfiguration.Instance.Sides.Split(',').ToArray();
        SideCount = sides.Length;

        List<string> selectorNames = new();
        GetRandomSelectors(selectorNames, RandomSelectors);
        RandomSelectorCount = RandomSelectors.Count + 1;

        // Initialize player slots
        var slots = new PlayerSlotObservable[MAX_PLAYER_COUNT];
        for (int i = 0; i < MAX_PLAYER_COUNT; i++)
        {
            slots[i] = new PlayerSlotObservable();
            InitPlayerSlotOptions(slots[i], sides, selectorNames);
        }
        PlayerSlots = slots;

        // Initialize observable checkbox/dropdown wrappers from settings
        RefreshGameOptionWrappers();

        // Initialize game mode filter
        RefreshGameModeFilter();

        // Subscribe to map changes
        MapLoader.MapChanged += MapLoader_MapChanged;

        // Subscribe to game process exit
        GameProcessService.GameProcessExited += OnGameProcessExited;

        // Load default map
        LoadDefaultGameModeMap();
    }

    protected virtual void Clean()
    {
        MapLoader.MapChanged -= MapLoader_MapChanged;
        GameProcessService.GameProcessExited -= OnGameProcessExited;
    }

    // --- Player slot initialization ---

    private void InitPlayerSlotOptions(PlayerSlotObservable slot, string[] sides, List<string> selectorNames)
    {
        // Name options: empty + AI names
        var nameOptions = new List<string> { string.Empty };
        nameOptions.AddRange(ProgramConstants.AI_PLAYER_NAMES);
        slot.NameOptions = nameOptions;

        // Side options: Random + selectors + sides
        var sideOptions = new List<string> { "Random".L10N("Client:Sides:RandomSide") };
        sideOptions.AddRange(selectorNames);
        sideOptions.AddRange(sides.Select(s => s.L10N($"INI:Sides:{s}")));
        slot.SideOptions = sideOptions;
        slot.SideSelectable = Enumerable.Repeat(true, sideOptions.Count).ToArray();

        // Color options: Random + MPColors
        string randomColor = GameOptionsIni.GetStringValue("General", "RandomColor", "255,255,255");
        var colorOptions = new List<string> { "Random".L10N("Client:Main:RandomColor") };
        colorOptions.AddRange(MPColors.Select(c => c.Name));
        slot.ColorOptions = colorOptions;
        slot.ColorSelectable = Enumerable.Repeat(true, colorOptions.Count).ToArray();

        // Start options
        var startOptions = new List<string> { "???" };
        for (int i = 1; i <= MAX_PLAYER_COUNT; i++)
            startOptions.Add(i.ToString());
        slot.StartOptions = startOptions;

        // Team options
        var teamOptions = new List<string> { "-" };
        teamOptions.AddRange(ProgramConstants.TEAMS);
        slot.TeamOptions = teamOptions;
    }

    // --- Game option wrapper refresh ---

    private void RefreshGameOptionWrappers()
    {
        CheckBoxes = CheckBoxSettings.Select(s => new GameOptionCheckBox(s)).ToList();
        DropDowns = DropDownSettings.Select(s => new GameOptionDropDown(s)).ToList();
    }

    // --- Map management ---

    private void MapLoader_MapChanged(object sender, MapChangedEventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action(() =>
        {
            switch (e.ChangeType)
            {
                case MapChangeType.Added:
                    HandleMapAdded(e.Map);
                    break;
                case MapChangeType.Updated:
                    HandleMapUpdated(e.Map, e.PreviousMapSHA1);
                    break;
                case MapChangeType.Removed:
                    HandleMapRemoved(e.Map);
                    break;
            }
        }));
    }

    protected virtual void HandleMapAdded(Map addedMap)
    {
        RefreshGameModeFilter();
        if (ShouldShowMapInCurrentFilter(addedMap))
            ListMaps();
    }

    protected virtual void HandleMapUpdated(Map updatedMap, string previousSHA1)
    {
        if (Map != null && (Map.SHA1 == previousSHA1 || Map.SHA1 == updatedMap.SHA1))
        {
            var updatedGameModeMap = GameModeMaps
                .FirstOrDefault(gmm => gmm.Map.SHA1 == updatedMap.SHA1);
            if (updatedGameModeMap != null)
                ChangeMap(updatedGameModeMap);
        }
        RefreshGameModeFilter();
        ListMaps();
    }

    private void HandleMapRemoved(Map removedMap)
    {
        if (Map != null && Map.SHA1 == removedMap.SHA1)
        {
            var currentGameModeName = GameMode?.Name;
            var availableMaps = GameModeMaps
                .Where(gmm => gmm.GameMode.Name == currentGameModeName)
                .ToList();
            if (availableMaps.Any())
            {
                ChangeMap(availableMaps.First());
            }
            else
            {
                var firstAvailable = GameModeMaps.FirstOrDefault();
                if (firstAvailable != null)
                {
                    ChangeMap(firstAvailable);
                    RefreshMapSelectionUI();
                }
            }
        }
        RefreshGameModeFilter();
        ListMaps();
    }

    private bool ShouldShowMapInCurrentFilter(Map map)
    {
        if (map?.GameModes == null || gameModeMapFilter == null)
            return false;

        return map.GameModes.Any(gameModeName =>
        {
            var gameMode = MapLoader.GameModes.FirstOrDefault(gm => gm.Name == gameModeName);
            if (gameMode == null) return false;
            return gameModeMapFilter.GetGameModeMaps().Any(gmm =>
                gmm.GameMode.Name == gameMode.Name && gmm.Map.SHA1 == map.SHA1);
        });
    }

    protected bool IsFavoriteMapsSelected() =>
        SelectedGameModeFilterIndex >= 0 && SelectedGameModeFilterIndex < GameModeFilterOptions.Count
        && GameModeFilterOptions[SelectedGameModeFilterIndex] == FavoriteMapsLabel;

    private List<GameModeMap> GetFavoriteGameModeMaps() =>
        GameModeMaps.Where(gmm => gmm.IsFavorite).ToList();

    private Func<List<GameModeMap>> GetGameModeMaps(GameMode gm) => () =>
        GameModeMaps.Where(gmm => gmm.GameMode.Name == gm.Name).ToList();

    protected List<GameModeMap> GetSortedGameModeMaps()
    {
        var maps = searchAllGameModes ? GameModeMaps.ToList() : gameModeMapFilter?.GetGameModeMaps() ?? new List<GameModeMap>();

        if (IsMapSortButtonEnabled && IsMapSortButtonVisible)
        {
            switch ((SortDirection)SortDirectionState)
            {
                case SortDirection.Asc:
                    maps = maps.OrderBy(gmm => gmm.Map.Name).ToList();
                    break;
                case SortDirection.Desc:
                    maps = maps.OrderByDescending(gmm => gmm.Map.Name).ToList();
                    break;
            }
        }

        return maps;
    }

    protected void ListMaps()
    {
        var isFavoriteMapsSelected = IsFavoriteMapsSelected();
        var maps = GetSortedGameModeMaps();

        List<GameModeMap> filteredMaps;
        if (!string.IsNullOrEmpty(MapSearchText))
        {
            string search = MapSearchText.Trim();
            string[] searchWords = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var exactMatches = maps.Where(gmm =>
                gmm.Map.Name.Equals(search, StringComparison.CurrentCultureIgnoreCase) ||
                gmm.Map.UntranslatedName.Equals(search, StringComparison.InvariantCultureIgnoreCase)).ToList();

            var substringMatches = maps.Except(exactMatches).Where(gmm =>
                gmm.Map.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                gmm.Map.UntranslatedName.Contains(search, StringComparison.InvariantCultureIgnoreCase)).ToList();

            var multiWordMatches = maps.Except(exactMatches).Except(substringMatches).Where(gmm =>
            {
                bool allInTranslated = searchWords.All(word =>
                    gmm.Map.Name.Contains(word, StringComparison.CurrentCultureIgnoreCase));
                bool allInUntranslated = searchWords.All(word =>
                    gmm.Map.UntranslatedName.Contains(word, StringComparison.InvariantCultureIgnoreCase));
                return allInTranslated || allInUntranslated;
            }).ToList();

            filteredMaps = exactMatches.Concat(substringMatches).Concat(multiWordMatches).ToList();
        }
        else
        {
            filteredMaps = maps;
        }

        int mapIndex = -1;
        bool gameModeMapChanged = false;
        var items = new List<MapListItem>();

        for (int i = 0; i < filteredMaps.Count; i++)
        {
            var gmm = filteredMaps[i];
            int rankIndex = 0;

            if (gmm.IsCoop)
            {
                if (StatisticsManager.Instance.HasBeatCoOpMap(gmm.Map.UntranslatedName, gmm.GameMode.UntranslatedUIName))
                    rankIndex = Math.Abs(2 - gmm.CoopDifficultyLevel) + 1;
                else
                    rankIndex = 0;
            }
            else
            {
                rankIndex = GetDefaultMapRankIndex(gmm) + 1;
            }

            var mapNameText = gmm.Map.Name;
            if (isFavoriteMapsSelected || searchAllGameModes)
                mapNameText += $" - {gmm.GameMode.UIName}";

            var item = new MapListItem
            {
                Source = gmm,
                RankIndex = rankIndex,
                MapName = mapNameText,
                GameModeName = gmm.GameMode.UIName,
                IsDisabled = gmm.MultiplayerOnly && !IsMultiplayer
            };
            items.Add(item);

            if (gmm == GameModeMap)
            {
                mapIndex = i;
                gameModeMapChanged = false;
            }

            if (mapIndex == -1 && (gmm?.Map?.Equals(GameModeMap?.Map) ?? false))
            {
                mapIndex = i;
                gameModeMapChanged = true;
            }
        }

        MapListItems = items;

        if (mapIndex > -1)
            SelectedMapIndex = mapIndex;

        if (gameModeMapChanged)
            OnMapSelectionChanged();
    }

    protected abstract int GetDefaultMapRankIndex(GameModeMap gameModeMap);
    protected abstract bool IsMultiplayer { get; }

    partial void OnSelectedMapIndexChanged(int value)
    {
        OnMapSelectionChanged();
    }

    private void OnMapSelectionChanged()
    {
        if (SelectedMapIndex < 0 || SelectedMapIndex >= MapListItems.Count)
        {
            ChangeMap(null);
            return;
        }

        var item = MapListItems[SelectedMapIndex];
        ChangeMap(item.Source);
    }

    partial void OnMapSearchTextChanged(string value)
    {
        ListMaps();
    }

    partial void OnSelectedGameModeFilterIndexChanged(int value)
    {
        if (value < 0 || value >= GameModeFilterOptions.Count)
            return;

        // Rebuild the filter from the options
        RefreshGameModeFilterFromIndex(value);
        MapSearchText = string.Empty;
        ListMaps();

        if (SelectedMapIndex == -1)
            SelectedMapIndex = 0;
        else
            ChangeMap(GameModeMap);
    }

    partial void OnSortDirectionStateChanged(int value)
    {
        UserINISettings.Instance.MapSortState.Value = value;
        UserINISettings.Instance.SaveSettings();
        ListMaps();
    }

    // --- Game mode filter ---

    protected void RefreshGameModeFilter()
    {
        var currentSelection = SelectedGameModeFilterIndex >= 0 && SelectedGameModeFilterIndex < GameModeFilterOptions.Count
            ? GameModeFilterOptions[SelectedGameModeFilterIndex]
            : null;

        var options = new List<string> { FavoriteMapsLabel };
        foreach (GameMode gm in GameModeMaps.GameModes)
            options.Add(gm.UIName);

        GameModeFilterOptions = options;

        int selectedIndex = options.FindIndex(o => o == currentSelection);
        SelectedGameModeFilterIndex = selectedIndex >= 0 ? selectedIndex : 0;
    }

    private void RefreshGameModeFilterFromIndex(int index)
    {
        if (index == 0)
        {
            gameModeMapFilter = new GameModeMapFilter(GetFavoriteGameModeMaps);
        }
        else
        {
            var gameModeIndex = index - 1;
            if (gameModeIndex >= 0 && gameModeIndex < GameModeMaps.GameModes.Count)
            {
                var gm = GameModeMaps.GameModes[gameModeIndex];
                gameModeMapFilter = new GameModeMapFilter(GetGameModeMaps(gm));
            }
        }
    }

    protected void RefreshMapSelectionUI()
    {
        if (GameMode == null)
            return;

        int filterIndex = GameModeFilterOptions.ToList().FindIndex(o => o == GameMode.UIName);
        if (filterIndex == -1)
            return;

        if (SelectedGameModeFilterIndex == filterIndex)
            RefreshGameModeFilterFromIndex(filterIndex);

        SelectedGameModeFilterIndex = filterIndex;
    }

    protected void LoadDefaultGameModeMap()
    {
        if (GameModeFilterOptions.Count > 0)
        {
            SelectedGameModeFilterIndex = GetDefaultGameModeMapFilterIndex();
            SelectedMapIndex = 0;
        }
    }

    protected int GetDefaultGameModeMapFilterIndex()
    {
        // Find first non-empty filter (skip index 0 which is Favorites)
        for (int i = 1; i < GameModeFilterOptions.Count; i++)
        {
            var filter = CreateFilterForIndex(i);
            if (filter != null && filter.Any())
                return i;
        }
        return 0;
    }

    private GameModeMapFilter CreateFilterForIndex(int index)
    {
        if (index == 0)
            return new GameModeMapFilter(GetFavoriteGameModeMaps);

        var gameModeIndex = index - 1;
        if (gameModeIndex >= 0 && gameModeIndex < GameModeMaps.GameModes.Count)
        {
            var gm = GameModeMaps.GameModes[gameModeIndex];
            return new GameModeMapFilter(GetGameModeMaps(gm));
        }
        return null;
    }

    // --- Change map ---

    protected virtual void ChangeMap(GameModeMap gameModeMap)
    {
        GameModeMap = gameModeMap;
        _ = UpdateLaunchGameButtonStatus();
        SetMapLabels();

        if (GameMode == null || Map == null)
        {
            OnGameOptionChanged();
            return;
        }

        disableGameOptionUpdateBroadcast = true;

        // Reset forced options
        foreach (var cb in CheckBoxes)
            cb.IsEnabled = true;
        foreach (var dd in DropDowns)
            dd.IsEnabled = true;

        // Clone lists to track which options were NOT forced
        var checkBoxListClone = new List<GameOptionCheckBox>(CheckBoxes);
        var dropDownListClone = new List<GameOptionDropDown>(DropDowns);

        // Apply forced options from GameMode and Map
        ApplyForcedCheckBoxOptions(checkBoxListClone, GameMode.ForcedCheckBoxValues);
        ApplyForcedCheckBoxOptions(checkBoxListClone, Map.ForcedCheckBoxValues);
        ApplyForcedDropDownOptions(dropDownListClone, GameMode.ForcedDropDownValues);
        ApplyForcedDropDownOptions(dropDownListClone, Map.ForcedDropDownValues);

        // Restore non-forced options to host's selections
        foreach (var cb in checkBoxListClone)
            cb.IsChecked = cb.HostChecked;
        foreach (var dd in dropDownListClone)
            dd.SelectedIndex = dd.HostSelectedIndex;

        // Reset player options based on map constraints
        var concatPlayerList = Players.Concat(AIPlayers).ToList();
        foreach (PlayerInfo pInfo in concatPlayerList)
        {
            if (!GameModeMap.AllowedStartingLocations.Contains(pInfo.StartingLocation) ||
                GameModeMap.ForceRandomStartLocations)
                pInfo.StartingLocation = 0;
            if (!GameModeMap.IsCoop && GameModeMap.ForceNoTeams)
                pInfo.TeamId = 0;
        }

        if (GameModeMap.CoopInfo != null)
        {
            // Co-Op map disallowed color logic
            foreach (int disallowedColorIndex in GameModeMap.CoopInfo.DisallowedPlayerColors)
            {
                if (disallowedColorIndex >= MPColors.Count)
                    continue;

                foreach (PlayerInfo pInfo in concatPlayerList)
                {
                    if (pInfo.ColorId == disallowedColorIndex + 1)
                        pInfo.ColorId = 0;
                }
            }

            // Force teams for co-op
            foreach (PlayerInfo pInfo in concatPlayerList)
                pInfo.TeamId = 1;
        }

        OnGameOptionChanged();
        CopyPlayerDataToUI();

        disableGameOptionUpdateBroadcast = false;
    }

    protected virtual void SetMapLabels()
    {
        if (GameMode == null || Map == null)
        {
            MapName = "Map: Unknown".L10N("Client:Main:MapUnknown");
            MapAuthor = "By Unknown Author".L10N("Client:Main:AuthorByUnknown");
            GameModeName = "Game mode: Unknown".L10N("Client:Main:GameModeUnknown");
            MapSize = "Size: Not available".L10N("Client:Main:MapSizeUnknown");
            return;
        }

        MapName = "Map:".L10N("Client:Main:Map") + " " + Map.Name;
        MapAuthor = "By".L10N("Client:Main:AuthorBy") + " " + Map.Author;
        GameModeName = "Game mode:".L10N("Client:Main:GameModeLabel") + " " + GameMode.UIName;
        MapSize = "Size:".L10N("Client:Main:MapSize") + " " + Map.GetSizeString();
    }

    // --- Game options ---

    protected virtual void OnGameOptionChanged()
    {
        CheckDisallowedSides();
        LaunchButtonRank = GetRank();
    }

    private void ApplyForcedCheckBoxOptions(List<GameOptionCheckBox> optionList, List<KeyValuePair<string, bool>> forcedOptions)
    {
        foreach (var option in forcedOptions)
        {
            var cb = CheckBoxes.FirstOrDefault(c => c.Name == option.Key);
            if (cb != null)
            {
                cb.IsChecked = option.Value;
                cb.IsEnabled = false;
                optionList.Remove(cb);
            }
        }
    }

    private void ApplyForcedDropDownOptions(List<GameOptionDropDown> optionList, List<KeyValuePair<string, int>> forcedOptions)
    {
        foreach (var option in forcedOptions)
        {
            var dd = DropDowns.FirstOrDefault(d => d.Name == option.Key);
            if (dd != null)
            {
                dd.SelectedIndex = option.Value;
                dd.IsEnabled = false;
                optionList.Remove(dd);
            }
        }
    }

    // --- Player data ---

    protected PlayerInfo GetPlayerInfoForIndex(int playerIndex)
    {
        if (playerIndex < Players.Count)
            return Players[playerIndex];
        if (playerIndex < Players.Count + AIPlayers.Count)
            return AIPlayers[playerIndex - Players.Count];
        return null;
    }

    protected virtual PlayerInfo FindLocalPlayer() => Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

    protected void ClearReadyStatuses(bool resetAutoReady = false)
    {
        for (int i = 1; i < Players.Count; i++)
        {
            if (resetAutoReady || !Players[i].AutoReady || Players[i].IsInGame)
                Players[i].Ready = false;
        }
    }

    protected virtual void CopyPlayerDataToUI()
    {
        PlayerUpdatingInProgress = true;

        var slots = (List<PlayerSlotObservable>)PlayerSlots;
        bool allowOptionsChange = AllowPlayerOptionsChange();
        var extraOpts = playerExtraOptions;

        // Human players
        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];
            pInfo.Index = pId;
            var slot = slots[pId];

            slot.PlayerName = pInfo.Name;
            slot.SelectedNameIndex = 0;
            slot.IsNameDropdownEnabled = false;

            bool allowPlayerOptionsChange = allowOptionsChange || pInfo.Name == ProgramConstants.PLAYERNAME;

            // Apply PlayerExtraOptions: force random sides
            if (extraOpts.IsForceRandomSides && pInfo.SideId != 0)
                pInfo.SideId = 0;

            slot.SelectedSideIndex = pInfo.SideId;
            slot.IsSideDropdownEnabled = !extraOpts.IsForceRandomSides && allowPlayerOptionsChange;

            // Apply PlayerExtraOptions: force random colors
            if (extraOpts.IsForceRandomColors && pInfo.ColorId != 0)
                pInfo.ColorId = 0;

            slot.SelectedColorIndex = pInfo.ColorId;
            slot.IsColorDropdownEnabled = !extraOpts.IsForceRandomColors && allowPlayerOptionsChange;

            // Apply PlayerExtraOptions: force random starts
            if (extraOpts.IsForceRandomStarts && pInfo.StartingLocation != 0)
                pInfo.StartingLocation = 0;

            slot.SelectedStartIndex = pInfo.StartingLocation;

            // Apply PlayerExtraOptions: force no teams
            if (extraOpts.IsForceNoTeams && pInfo.TeamId != 0)
                pInfo.TeamId = 0;

            slot.SelectedTeamIndex = pInfo.TeamId;

            if (GameModeMap != null)
            {
                slot.IsTeamDropdownEnabled = !extraOpts.IsForceNoTeams && allowPlayerOptionsChange && !GameModeMap.IsCoop && !GameModeMap.ForceNoTeams;
                slot.IsStartDropdownEnabled = !extraOpts.IsForceRandomStarts && allowPlayerOptionsChange && !GameModeMap.ForceRandomStartLocations;
            }
        }

        // AI players
        for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
        {
            PlayerInfo aiInfo = AIPlayers[aiId];
            int index = Players.Count + aiId;
            aiInfo.Index = index;
            var slot = slots[index];

            slot.SelectedNameIndex = 1 + aiInfo.AILevel;
            slot.IsNameDropdownEnabled = allowOptionsChange;

            // Apply PlayerExtraOptions: force random sides
            if (extraOpts.IsForceRandomSides && aiInfo.SideId != 0)
                aiInfo.SideId = 0;

            slot.SelectedSideIndex = aiInfo.SideId;
            slot.IsSideDropdownEnabled = !extraOpts.IsForceRandomSides && allowOptionsChange;

            // Apply PlayerExtraOptions: force random colors
            if (extraOpts.IsForceRandomColors && aiInfo.ColorId != 0)
                aiInfo.ColorId = 0;

            slot.SelectedColorIndex = aiInfo.ColorId;
            slot.IsColorDropdownEnabled = !extraOpts.IsForceRandomColors && allowOptionsChange;

            // Apply PlayerExtraOptions: force random starts
            if (extraOpts.IsForceRandomStarts && aiInfo.StartingLocation != 0)
                aiInfo.StartingLocation = 0;

            slot.SelectedStartIndex = aiInfo.StartingLocation;

            // Apply PlayerExtraOptions: force no teams
            if (extraOpts.IsForceNoTeams && aiInfo.TeamId != 0)
                aiInfo.TeamId = 0;

            slot.SelectedTeamIndex = aiInfo.TeamId;

            if (GameModeMap != null)
            {
                slot.IsTeamDropdownEnabled = !extraOpts.IsForceNoTeams && allowOptionsChange && !GameModeMap.IsCoop && !GameModeMap.ForceNoTeams;
                slot.IsStartDropdownEnabled = !extraOpts.IsForceRandomStarts && allowOptionsChange && !GameModeMap.ForceRandomStartLocations;
            }
        }

        // Unused slots
        for (int ddIndex = Players.Count + AIPlayers.Count; ddIndex < MAX_PLAYER_COUNT; ddIndex++)
        {
            var slot = slots[ddIndex];
            slot.SelectedNameIndex = 0;
            slot.IsNameDropdownEnabled = false;
            slot.SelectedSideIndex = -1;
            slot.IsSideDropdownEnabled = false;
            slot.SelectedColorIndex = -1;
            slot.IsColorDropdownEnabled = false;
            slot.SelectedStartIndex = -1;
            slot.IsStartDropdownEnabled = false;
            slot.SelectedTeamIndex = -1;
            slot.IsTeamDropdownEnabled = false;
        }

        // Enable adding AI to the next slot
        if (allowOptionsChange && Players.Count + AIPlayers.Count < MAX_PLAYER_COUNT)
            slots[Players.Count + AIPlayers.Count].IsNameDropdownEnabled = true;

        CheckDisallowedSides();

        // Update PlayerNames for compatibility
        PlayerNames = Players.Select(p => p.Name).ToList();

        PlayerUpdatingInProgress = false;
    }

    protected virtual void CopyPlayerDataFromUI()
    {
        if (PlayerUpdatingInProgress)
            return;

        ClearReadyStatuses();

        var slots = PlayerSlots;

        var oldSideId = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId;

        if (Players.Count > MAX_PLAYER_COUNT)
            throw new Exception($"Player count exceeds maximum of {MAX_PLAYER_COUNT}.");

        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];
            var slot = slots[pId];

            pInfo.ColorId = slot.SelectedColorIndex;
            pInfo.SideId = slot.SelectedSideIndex;
            pInfo.StartingLocation = slot.SelectedStartIndex;
            pInfo.TeamId = slot.SelectedTeamIndex;

            if (pInfo.SideId == SideCount + RandomSelectorCount)
                pInfo.StartingLocation = 0;
        }

        AIPlayers.Clear();
        for (int cmbId = Players.Count; cmbId < MAX_PLAYER_COUNT; cmbId++)
        {
            var slot = slots[cmbId];
            if (slot.SelectedNameIndex < 1)
                continue;

            PlayerInfo aiPlayer = new PlayerInfo
            {
                Name = ProgramConstants.AI_PLAYER_NAMES[slot.SelectedNameIndex - 1],
                AILevel = slot.SelectedNameIndex - 1,
                SideId = Math.Max(slot.SelectedSideIndex, 0),
                ColorId = Math.Max(slot.SelectedColorIndex, 0),
                StartingLocation = Math.Max(slot.SelectedStartIndex, 0),
                TeamId = Map != null && GameModeMap.IsCoop ? 1 : Math.Max(slot.SelectedTeamIndex, 0),
                IsAI = true
            };
            AIPlayers.Add(aiPlayer);
        }

        CopyPlayerDataToUI();
        LaunchButtonRank = GetRank();

        if (oldSideId != Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId)
            UpdateDiscordPresence();
    }

    // --- Disallowed sides ---

    protected void CheckDisallowedSides()
    {
        CheckDisallowedSidesForGroup(forHumanPlayers: false);
        CheckDisallowedSidesForGroup(forHumanPlayers: true);
    }

    protected void CheckDisallowedSidesForGroup(bool forHumanPlayers)
    {
        var disallowedSideArray = GetDisallowedSidesForGroup(forHumanPlayers);
        var playerInfos = forHumanPlayers ? Players : AIPlayers;
        int defaultSide = 0;
        int allowedSideCount = disallowedSideArray.Count(b => !b);

        var slots = PlayerSlots;

        if (allowedSideCount == 1)
        {
            for (int i = 0; i < disallowedSideArray.Length; i++)
            {
                if (!disallowedSideArray[i])
                    defaultSide = i + RandomSelectorCount;
            }

            foreach (PlayerInfo pInfo in playerInfos)
            {
                var sideSelectable = new List<bool>(slots[pInfo.Index].SideSelectable);
                for (int i = 0; i < RandomSelectorCount; i++)
                    sideSelectable[i] = false;
                slots[pInfo.Index].SideSelectable = sideSelectable;
            }
        }
        else
        {
            foreach (PlayerInfo pInfo in playerInfos)
            {
                var sideSelectable = new List<bool>(slots[pInfo.Index].SideSelectable);
                for (int i = 0; i < RandomSelectorCount; i++)
                    sideSelectable[i] = true;
                slots[pInfo.Index].SideSelectable = sideSelectable;
            }
        }

        // Custom random groups
        int c = 0;
        foreach (int[] randomSides in RandomSelectors)
        {
            int disableCount = randomSides.Count(side => disallowedSideArray[side]);
            bool disabled = disableCount >= randomSides.Length - 1;

            foreach (PlayerInfo pInfo in playerInfos)
            {
                var sideSelectable = new List<bool>(slots[pInfo.Index].SideSelectable);
                sideSelectable[1 + c] = !disabled;
                slots[pInfo.Index].SideSelectable = sideSelectable;

                if (pInfo.SideId == 1 + c && disabled)
                    pInfo.SideId = defaultSide;
            }
            c++;
        }

        // Individual sides
        for (int i = 0; i < disallowedSideArray.Length; i++)
        {
            bool disabled = disallowedSideArray[i];
            if (disabled)
            {
                foreach (PlayerInfo pInfo in playerInfos)
                {
                    var sideSelectable = new List<bool>(slots[pInfo.Index].SideSelectable);
                    sideSelectable[i + RandomSelectorCount] = false;
                    slots[pInfo.Index].SideSelectable = sideSelectable;

                    if (pInfo.SideId == i + RandomSelectorCount)
                        pInfo.SideId = defaultSide;
                }
            }
            else
            {
                foreach (PlayerInfo pInfo in playerInfos)
                {
                    var sideSelectable = new List<bool>(slots[pInfo.Index].SideSelectable);
                    sideSelectable[i + RandomSelectorCount] = true;
                    slots[pInfo.Index].SideSelectable = sideSelectable;
                }
            }
        }

        if (allowedSideCount == 1)
        {
            foreach (PlayerInfo pInfo in playerInfos)
            {
                if (pInfo.SideId == 0)
                    pInfo.SideId = defaultSide;
            }
        }

        // Co-op spectator disallow
        if (GameModeMap != null && GameModeMap.CoopInfo != null)
        {
            foreach (PlayerInfo pInfo in playerInfos)
            {
                if (pInfo.SideId == GetSpectatorSideIndex())
                    pInfo.SideId = defaultSide;
            }

            foreach (PlayerInfo pInfo in playerInfos)
            {
                var sideSelectable = new List<bool>(slots[pInfo.Index].SideSelectable);
                if (sideSelectable.Count > GetSpectatorSideIndex())
                    sideSelectable[GetSpectatorSideIndex()] = false;
                slots[pInfo.Index].SideSelectable = sideSelectable;
            }
        }
        else
        {
            foreach (PlayerInfo pInfo in playerInfos)
            {
                var sideSelectable = new List<bool>(slots[pInfo.Index].SideSelectable);
                if (sideSelectable.Count > SideCount + RandomSelectorCount)
                    sideSelectable[SideCount + RandomSelectorCount] = true;
                slots[pInfo.Index].SideSelectable = sideSelectable;
            }
        }
    }

    protected bool[] GetDisallowedSidesForGroup(bool forHumanPlayers)
    {
        var returnValue = GetDisallowedSides();
        var sides = forHumanPlayers ? GameMode?.DisallowedHumanPlayerSides : GameMode?.DisallowedComputerPlayerSides;
        if (sides != null)
        {
            foreach (int i in sides)
                returnValue[i] = true;
        }
        return returnValue;
    }

    protected bool[] GetDisallowedSides()
    {
        var returnValue = new bool[SideCount];

        if (GameModeMap?.CoopInfo != null)
        {
            foreach (int disallowedSideIndex in GameModeMap.CoopInfo.DisallowedPlayerSides)
                returnValue[disallowedSideIndex] = true;
        }

        if (GameMode != null)
        {
            foreach (int disallowedSideIndex in GameMode.DisallowedPlayerSides)
                returnValue[disallowedSideIndex] = true;
        }

        foreach (var cb in CheckBoxes)
            cb.Setting.ApplyDisallowedSideIndex(returnValue);

        return returnValue;
    }

    private int GetSpectatorSideIndex() => SideCount + RandomSelectorCount;

    protected bool IsPlayerSpectator(PlayerInfo pInfo) => pInfo.SideId == GetSpectatorSideIndex();

    // --- Random selectors ---

    private void GetRandomSelectors(List<string> selectorNames, List<int[]> selectorSides)
    {
        List<string> keys = GameOptionsIni.GetSectionKeys("RandomSelectors");
        if (keys == null)
            return;

        foreach (string randomSelector in keys)
        {
            List<int> randomSides = new();
            try
            {
                string[] tmp = GameOptionsIni.GetStringListValue("RandomSelectors", randomSelector, string.Empty);
                randomSides = Array.ConvertAll(tmp, int.Parse).ToList();
                randomSides.RemoveAll(x => x >= SideCount || x < 0);
            }
            catch (FormatException) { }

            if (randomSides.Count > 1)
            {
                selectorNames.Add(randomSelector);
                selectorSides.Add(randomSides.ToArray());
            }
        }
    }

    // --- Map commands ---

    [RelayCommand]
    private void PickRandomMap()
    {
        int totalPlayerCount = Players.Count(p => !IsPlayerSpectator(p)) + AIPlayers.Count;
        List<GameModeMap> gameModeMaps = GetRandomGameModeMaps(totalPlayerCount);
        if (gameModeMaps.Count < 1)
            return;

        int randomValue = random.Next(0, gameModeMaps.Count);
        GameModeMap = gameModeMaps[randomValue];
        Logger.Log("PickRandomMap: Rolled " + randomValue + " out of " + gameModeMaps.Count + ". Picked map: " + GameModeMap.Map.Name);

        ChangeMap(GameModeMap);
        MapSearchText = string.Empty;
        ListMaps();
    }

    private List<GameModeMap> GetRandomGameModeMaps(int playerCount)
    {
        List<GameModeMap> gameModeMaps = IsFavoriteMapsSelected()
            ? GetFavoriteGameModeMaps()
            : GameModeMaps.Where(gmm => gmm.GameMode.Name == GameMode?.Name).ToList();

        if (playerCount != 1)
        {
            gameModeMaps = gameModeMaps.Where(gmm => gmm.MaxPlayers == playerCount).ToList();
            if (gameModeMaps.Count < 1 && playerCount <= MAX_PLAYER_COUNT)
                return GetRandomGameModeMaps(playerCount + 1);
        }

        return gameModeMaps;
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        if (GameModeMap != null)
        {
            GameModeMap.IsFavorite = UserINISettings.Instance.ToggleFavoriteMap(
                Map.SHA1, GameMode.Name, GameModeMap.IsFavorite);
        }
    }

    [RelayCommand]
    private void DeleteMap()
    {
        if (Map == null || Map.Official || IsMultiplayer)
            return;

        try
        {
            string currentGameModeName = GameMode?.Name;
            MapLoader.DeleteCustomMap(GameModeMap);

            MapSearchText = string.Empty;
            bool currentGameModeHasMaps = GameModeMaps.Any(gmm => gmm.GameMode.Name == currentGameModeName);
            if (!currentGameModeHasMaps)
            {
                GameModeMap = GameModeMaps.FirstOrDefault(gm => gm.GameMode.Maps.Count > 0);
            }
            else
            {
                // Select adjacent map
                if (SelectedMapIndex > 0)
                    SelectedMapIndex--;
                else if (MapListItems.Count > 1)
                    SelectedMapIndex = 1;
            }

            ListMaps();
            ChangeMap(GameModeMap);
        }
        catch (IOException ex)
        {
            Logger.Log($"Deleting map {Map.BaseFilePath} failed! Message: {ex}");
            AddNotice("Deleting map failed! Reason:".L10N("Client:Main:DeleteMapFailedText") + " " + ex.Message);
        }
    }

    [RelayCommand]
    private void ShowMapInFolder()
    {
        Map?.OpenContainingFolder();
    }

    [RelayCommand]
    private void CycleSortDirection()
    {
        SortDirectionState = SortDirectionState switch
        {
            0 => 1,
            1 => 2,
            _ => 0
        };
    }

    [RelayCommand]
    private void ToggleSearchAllModes()
    {
        searchAllGameModes = !searchAllGameModes;
        UserINISettings.Instance.SearchAllGameModes.Value = searchAllGameModes;
        UserINISettings.Instance.SaveSettings();
        ListMaps();
    }

    // --- Presets ---

    [RelayCommand]
    private void SaveGameOptionPreset()
    {
        // The View will show a dialog and call HandleGameOptionPresetSaveCommand
        // For now, emit an event or let subclass handle
        OnSavePresetRequested();
    }

    [RelayCommand]
    private void LoadGameOptionPreset()
    {
        // The View will show a dialog and call HandleGameOptionPresetLoadCommand
        OnLoadPresetRequested();
    }

    protected void HandleGameOptionPresetSaveCommand(string presetName)
    {
        string error = AddGameOptionPreset(presetName);
        if (!string.IsNullOrEmpty(error))
            AddNotice(error);
    }

    protected void HandleGameOptionPresetLoadCommand(string presetName)
    {
        if (LoadGameOptionPreset(presetName))
            AddNotice("Game option preset loaded succesfully.".L10N("Client:Main:PresetLoaded"));
        else
            AddNotice(string.Format("Preset {0} not found!".L10N("Client:Main:PresetNotFound"), presetName));
    }

    protected string AddGameOptionPreset(string name)
    {
        string error = GameOptionPreset.IsNameValid(name);
        if (!string.IsNullOrEmpty(error))
            return error;

        GameOptionPreset preset = new GameOptionPreset(name);
        foreach (var cb in CheckBoxes)
            preset.AddCheckBoxValue(cb.Name, cb.IsChecked);
        foreach (var dd in DropDowns)
            preset.AddDropDownValue(dd.Name, dd.SelectedIndex);

        GameOptionPresets.Instance.AddPreset(preset);
        return null;
    }

    public bool LoadGameOptionPreset(string name)
    {
        GameOptionPreset preset = GameOptionPresets.Instance.GetPreset(name);
        if (preset == null)
            return false;

        disableGameOptionUpdateBroadcast = true;

        foreach (var kvp in preset.GetCheckBoxValues())
        {
            var cb = CheckBoxes.FirstOrDefault(c => c.Name == kvp.Key);
            if (cb != null && cb.IsEnabled)
            {
                cb.IsChecked = kvp.Value;
                cb.HostChecked = kvp.Value;
            }
        }

        foreach (var kvp in preset.GetDropDownValues())
        {
            var dd = DropDowns.FirstOrDefault(d => d.Name == kvp.Key);
            if (dd != null && dd.IsEnabled)
            {
                dd.SelectedIndex = kvp.Value;
                dd.HostSelectedIndex = kvp.Value;
            }
        }

        disableGameOptionUpdateBroadcast = false;
        OnGameOptionChanged();
        return true;
    }

    // --- Rank ---

    protected Rank GetRank()
    {
        if (GameMode == null || Map == null)
            return Rank.None;

        foreach (var cb in CheckBoxes)
        {
            if (!cb.Setting.AllowScoring)
                return Rank.None;
        }
        foreach (var dd in DropDowns)
        {
            if (!dd.Setting.AllowScoring)
                return Rank.None;
        }

        PlayerInfo localPlayer = FindLocalPlayer();
        if (localPlayer == null || IsPlayerSpectator(localPlayer))
            return Rank.None;

        int[] teamMemberCounts = new int[5];
        int lowestEnemyAILevel = 2;
        int highestAllyAILevel = 0;

        foreach (PlayerInfo aiPlayer in AIPlayers)
        {
            teamMemberCounts[aiPlayer.TeamId]++;
            if (aiPlayer.TeamId > 0 && aiPlayer.TeamId == localPlayer.TeamId)
            {
                if (aiPlayer.AILevel > highestAllyAILevel)
                    highestAllyAILevel = aiPlayer.AILevel;
            }
            else
            {
                if (aiPlayer.AILevel < lowestEnemyAILevel)
                    lowestEnemyAILevel = aiPlayer.AILevel;
            }
        }

        if (IsMultiplayer)
        {
            if (Players.Count == 1)
                return Rank.None;

            if (GameModeMap.MaxPlayers <= 3)
            {
                var filteredPlayers = Players.Where(p => !IsPlayerSpectator(p)).ToList();
                if (AIPlayers.Count > 0) return Rank.None;
                if (filteredPlayers.Count != GameModeMap.MaxPlayers) return Rank.None;
                int localTeamIndex = localPlayer.TeamId;
                if (localTeamIndex > 0 && filteredPlayers.Count(p => p.TeamId == localTeamIndex) > 1) return Rank.None;
                return Rank.Hard;
            }

            if (Players.Any(p => IsPlayerSpectator(p))) return Rank.None;
            if (AIPlayers.Count == 0) return Rank.None;
            if (Players.Any(p => p.TeamId != localPlayer.TeamId)) return Rank.None;
            if (Players.Any(p => p.TeamId == 0)) return Rank.None;
            if (AIPlayers.Any(p => p.TeamId == 0)) return Rank.None;

            teamMemberCounts[localPlayer.TeamId] += Players.Count;

            if (lowestEnemyAILevel < highestAllyAILevel) return Rank.None;

            int allyCount = teamMemberCounts[localPlayer.TeamId];
            for (int i = 1; i < 5; i++)
            {
                if (i == localPlayer.TeamId) continue;
                if (teamMemberCounts[i] > 0 && teamMemberCounts[i] < allyCount) return Rank.None;
            }

            return lowestEnemyAILevel + 1;
        }

        // Skirmish
        if (AIPlayers.Count != GameModeMap.MaxPlayers - 1)
            return Rank.None;

        teamMemberCounts[localPlayer.TeamId]++;

        if (lowestEnemyAILevel < highestAllyAILevel) return Rank.None;

        if (localPlayer.TeamId > 0)
        {
            int allyCount = teamMemberCounts[localPlayer.TeamId];
            for (int i = 1; i < 5; i++)
            {
                if (i == localPlayer.TeamId) continue;
                if (teamMemberCounts[i] > 0 && teamMemberCounts[i] < allyCount) return Rank.None;
            }

            bool pass = false;
            for (int i = 1; i < 5; i++)
            {
                if (i == localPlayer.TeamId) continue;
                if (teamMemberCounts[i] >= allyCount) { pass = true; break; }
            }
            if (!pass) return Rank.None;
        }

        return lowestEnemyAILevel + 1;
    }

    protected GameType GetGameType()
    {
        int teamCount = GetPvPTeamCount();
        if (teamCount == 0) return GameType.FFA;
        if (teamCount == 1) return GameType.Coop;
        return GameType.TeamGame;
    }

    private int GetPvPTeamCount()
    {
        int[] teamPlayerCounts = new int[4];
        int playerTeamCount = 0;

        foreach (PlayerInfo pInfo in Players)
        {
            if (pInfo.IsAI || IsPlayerSpectator(pInfo))
                continue;
            if (pInfo.TeamId > 0)
            {
                teamPlayerCounts[pInfo.TeamId - 1]++;
                if (teamPlayerCounts[pInfo.TeamId - 1] == 2)
                    playerTeamCount++;
            }
        }

        return playerTeamCount;
    }

    // --- Randomization ---

    protected virtual PlayerHouseInfo[] Randomize(List<TeamStartMapping> teamStartMappings, Random pseudoRandom)
    {
        int totalPlayerCount = Players.Count + AIPlayers.Count;
        PlayerHouseInfo[] houseInfos = new PlayerHouseInfo[totalPlayerCount];
        for (int i = 0; i < totalPlayerCount; i++)
            houseInfos[i] = new PlayerHouseInfo();

        for (int i = 0; i < Players.Count; i++)
            houseInfos[i].IsSpectator = Players[i].SideId == GetSpectatorSideIndex();

        // Available colors
        List<int> freeColors = new();
        for (int cId = 0; cId < MPColors.Count; cId++)
            freeColors.Add(cId);

        if (GameModeMap.CoopInfo != null)
        {
            foreach (int colorIndex in GameModeMap.CoopInfo.DisallowedPlayerColors)
                freeColors.Remove(colorIndex);
        }

        foreach (PlayerInfo player in Players)
            freeColors.Remove(player.ColorId - 1);
        foreach (PlayerInfo aiPlayer in AIPlayers)
            freeColors.Remove(aiPlayer.ColorId - 1);

        // Available starting locations
        List<int> freeStartingLocations = new();
        List<int> takenStartingLocations = new();

        foreach (int i in GameModeMap.AllowedStartingLocations)
            freeStartingLocations.Add(i - 1);

        for (int i = 0; i < Players.Count; i++)
        {
            if (!houseInfos[i].IsSpectator)
                freeStartingLocations.Remove(Players[i].StartingLocation - 1);
        }

        for (int i = 0; i < AIPlayers.Count; i++)
            freeStartingLocations.Remove(AIPlayers[i].StartingLocation - 1);

        foreach (var mapping in teamStartMappings.Where(m => m.IsBlock))
            freeStartingLocations.Remove(mapping.StartingWaypoint);

        // Randomize
        for (int i = 0; i < totalPlayerCount; i++)
        {
            PlayerInfo pInfo;
            PlayerHouseInfo pHouseInfo = houseInfos[i];
            bool[] disallowedSides;

            if (i < Players.Count)
            {
                pInfo = Players[i];
                disallowedSides = GetDisallowedSidesForGroup(forHumanPlayers: true);
            }
            else
            {
                pInfo = AIPlayers[i - Players.Count];
                disallowedSides = GetDisallowedSidesForGroup(forHumanPlayers: false);
            }

            pHouseInfo.RandomizeSide(pInfo, SideCount, pseudoRandom, disallowedSides, RandomSelectors, RandomSelectorCount);
            pHouseInfo.RandomizeColor(pInfo, freeColors, MPColors, pseudoRandom);

            bool overrideGameRandomLocations = teamStartMappings.Any()
                || GameModeMap.AllowedStartingLocations.Max() > GameModeMap.MaxPlayers;
            pHouseInfo.RandomizeStart(pInfo, pseudoRandom, freeStartingLocations, takenStartingLocations, overrideGameRandomLocations);
        }

        return houseInfos;
    }

    // --- Game launch ---

    protected virtual void StartGame()
    {
        Random pseudoRandom = new Random(RandomSeed);

        PlayerHouseInfo[] houseInfos = WriteSpawnIni(pseudoRandom);
        InitializeMatchStatistics(houseInfos);
        WriteMap(houseInfos, pseudoRandom);

        GameProcessService.StartGameProcess();
        UpdateDiscordPresence(true);
    }

    private PlayerHouseInfo[] WriteSpawnIni(Random pseudoRandom)
    {
        Logger.Log("Writing spawn.ini");

        FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);
        spawnerSettingsFile.Delete();

        if (GameModeMap.IsCoop)
        {
            foreach (PlayerInfo pInfo in Players)
                pInfo.TeamId = 1;
            foreach (PlayerInfo pInfo in AIPlayers)
                pInfo.TeamId = 1;
        }

        var teamStartMappings = GetTeamStartMappings();
        PlayerHouseInfo[] houseInfos = Randomize(teamStartMappings, pseudoRandom);

        IniFile spawnIni = new IniFile(spawnerSettingsFile.FullName);
        IniSection settings = new IniSection("Settings");

        settings.SetStringValue("Name", ProgramConstants.PLAYERNAME);
        settings.SetStringValue("Scenario", ProgramConstants.SPAWNMAP_INI);
        settings.SetStringValue("UIGameMode", GameMode.UntranslatedUIName);
        settings.SetStringValue("UIMapName", Map.UntranslatedName);

        if (Map.Official)
            settings.SetStringValue("MapID", Map.BaseFilePath);

        settings.SetIntValue("PlayerCount", Players.Count);
        int myIndex = Players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME);
        settings.SetIntValue("Side", houseInfos[myIndex].InternalSideIndex);
        settings.SetBooleanValue("IsSpectator", houseInfos[myIndex].IsSpectator);
        settings.SetIntValue("Color", houseInfos[myIndex].ColorIndex);
        settings.SetStringValue("CustomLoadScreen", LoadingScreenController.GetLoadScreenName(houseInfos[myIndex].InternalSideIndex.ToString()));
        settings.SetIntValue("AIPlayers", AIPlayers.Count);
        settings.SetIntValue("Seed", RandomSeed);
        if (GetPvPTeamCount() > 1)
            settings.SetBooleanValue("CoachMode", true);
        if (GetGameType() == GameType.Coop)
            settings.SetBooleanValue("AutoSurrender", false);
        spawnIni.AddSection(settings);
        WriteSpawnIniAdditions(spawnIni);

        foreach (var cb in CheckBoxes)
            cb.Setting.ApplySpawnIniCode(spawnIni);
        foreach (var dd in DropDowns)
            dd.Setting.ApplySpawnIniCode(spawnIni);

        // Forced spawn.ini options
        List<string> forcedKeys = GameOptionsIni.GetSectionKeys("ForcedSpawnIniOptions");
        if (forcedKeys != null)
        {
            foreach (string key in forcedKeys)
            {
                spawnIni.SetStringValue("Settings", key,
                    GameOptionsIni.GetStringValue("ForcedSpawnIniOptions", key, string.Empty));
            }
        }

        GameMode.ApplySpawnIniCode(spawnIni);
        Map.ApplySpawnIniCode(spawnIni, Players.Count + AIPlayers.Count,
            AIPlayers.Count, GameModeMap.IsCoop, GameModeMap.CoopInfo, GameModeMap.CoopDifficultyLevel, pseudoRandom, SideCount);

        // Player options
        int otherId = 1;
        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];
            PlayerHouseInfo pHouseInfo = houseInfos[pId];
            if (pInfo.Name == ProgramConstants.PLAYERNAME)
                continue;

            string sectionName = "Other" + otherId;
            spawnIni.SetStringValue(sectionName, "Name", pInfo.Name);
            spawnIni.SetIntValue(sectionName, "Side", pHouseInfo.InternalSideIndex);
            spawnIni.SetBooleanValue(sectionName, "IsSpectator", pHouseInfo.IsSpectator);
            spawnIni.SetIntValue(sectionName, "Color", pHouseInfo.ColorIndex);
            spawnIni.SetStringValue(sectionName, "Ip", GetIPAddressForPlayer(pInfo));
            spawnIni.SetIntValue(sectionName, "Port", pInfo.Port);
            otherId++;
        }

        // Color-based multi indexes
        List<int> multiCmbIndexes = new();
        var sortedColorList = MPColors.OrderBy(mpc => mpc.GameColorIndex).ToList();
        for (int cId = 0; cId < sortedColorList.Count; cId++)
        {
            for (int pId = 0; pId < Players.Count; pId++)
            {
                if (houseInfos[pId].ColorIndex == sortedColorList[cId].GameColorIndex)
                    multiCmbIndexes.Add(pId);
            }
        }

        if (AIPlayers.Count > 0)
        {
            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                int multiId = multiCmbIndexes.Count + aiId + 1;
                string keyName = "Multi" + multiId;
                spawnIni.SetIntValue("HouseHandicaps", keyName, AIPlayers[aiId].HouseHandicapAILevel);
                spawnIni.SetIntValue("HouseCountries", keyName, houseInfos[Players.Count + aiId].InternalSideIndex);
                spawnIni.SetIntValue("HouseColors", keyName, houseInfos[Players.Count + aiId].ColorIndex);
            }
        }

        for (int multiId = 0; multiId < multiCmbIndexes.Count; multiId++)
        {
            int pIndex = multiCmbIndexes[multiId];
            if (houseInfos[pIndex].IsSpectator)
                spawnIni.SetBooleanValue("IsSpectator", "Multi" + (multiId + 1), true);
        }

        AllianceHolder.WriteInfoToSpawnIni(Players, AIPlayers, multiCmbIndexes, houseInfos.ToList(), teamStartMappings, spawnIni);

        for (int pId = 0; pId < Players.Count; pId++)
        {
            int startingWaypoint = houseInfos[multiCmbIndexes[pId]].StartingWaypoint;
            if (startingWaypoint > -1)
                spawnIni.SetIntValue("SpawnLocations", "Multi" + (pId + 1), startingWaypoint);
        }

        for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
        {
            int startingWaypoint = houseInfos[Players.Count + aiId].StartingWaypoint;
            if (startingWaypoint > -1)
                spawnIni.SetIntValue("SpawnLocations", "Multi" + (Players.Count + aiId + 1), startingWaypoint);
        }

        spawnIni.WriteIniFile();
        return houseInfos;
    }

    private void WriteMap(PlayerHouseInfo[] houseInfos, Random pseudoRandom)
    {
        FileInfo spawnMapIniFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNMAP_INI);

        DeleteSupplementalMapFiles();
        spawnMapIniFile.Delete();

        Logger.Log("Writing map.");
        Logger.Log("Loading map INI from " + Map.CompleteFilePath);

        IniFile mapIni = Map.GetMapIni();
        IniFile globalCodeIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "Map Code", "GlobalCode.ini"));

        foreach (IniFile iniFile in GameMode.GetMapRulesIniFiles(pseudoRandom))
            MapCodeHelper.ApplyMapCode(mapIni, iniFile);

        MapCodeHelper.ApplyMapCode(mapIni, globalCodeIni);

        if (IsMultiplayer)
        {
            IniFile mpGlobalCodeIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "Map Code", "MultiplayerGlobalCode.ini"));
            MapCodeHelper.ApplyMapCode(mapIni, mpGlobalCodeIni);
        }
        else
        {
            string mapIniFileName = Path.GetFileName(mapIni.FileName);
            mapIni.SetStringValue("Basic", "OriginalFilename", mapIniFileName);
        }

        foreach (var cb in CheckBoxes)
            cb.Setting.ApplyMapCode(mapIni, GameMode);
        foreach (var dd in DropDowns)
            dd.Setting.ApplyMapCode(mapIni, GameMode);

        mapIni.MoveSectionToFirst("MultiplayerDialogSettings");

        CopySupplementalMapFiles(mapIni);
        ManipulateStartingLocations(mapIni, houseInfos);

        mapIni.WriteIniFile(spawnMapIniFile.FullName);
    }

    private void CopySupplementalMapFiles(IniFile mapIni)
    {
        var mapFileInfo = new FileInfo(mapIni.FileName);
        string mapFileBaseName = Path.GetFileNameWithoutExtension(mapFileInfo.Name);

        var supplementalMapFiles = GetSupplementalMapFiles(mapFileInfo.DirectoryName, mapFileBaseName).ToList();
        if (!supplementalMapFiles.Any())
            return;

        List<string> supplementalFileNames = new();
        foreach (string file in supplementalMapFiles)
        {
            try
            {
                string supplementalFileName = $"spawnmap{Path.GetExtension(file)}";
                File.Copy(file, SafePath.CombineFilePath(ProgramConstants.GamePath, supplementalFileName), true);
                supplementalFileNames.Add(supplementalFileName);
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to copy supplemental map file".L10N("Client:Main:SupplementalFileCopyError") + $" {file}";
                Logger.Log(errorMessage);
                Logger.Log(ex.ToString());
            }
        }

        mapIni.SetStringValue("Basic", "SupplementalFiles", string.Join(",", supplementalFileNames));
    }

    private void DeleteSupplementalMapFiles()
    {
        var paths = GetSupplementalMapFiles(ProgramConstants.GamePath, "spawnmap").ToList();
        if (!paths.Any())
            return;

        foreach (string path in paths)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to delete supplemental map file".L10N("Client:Main:SupplementalFileDeleteError") + $" {path}");
                Logger.Log(ex.ToString());
            }
        }
    }

    private static IEnumerable<string> GetSupplementalMapFiles(string basePath, string baseFileName)
    {
        var supplementalMapFileNames = ClientConfiguration.Instance.SupplementalMapFileExtensions
            .Select(ext => $"{baseFileName}.{ext}")
            .ToList();

        if (!supplementalMapFileNames.Any())
            return new List<string>();

        return Directory.GetFiles(basePath, $"{baseFileName}.*")
            .Where(f => supplementalMapFileNames.Contains(Path.GetFileName(f)));
    }

    private void ManipulateStartingLocations(IniFile mapIni, PlayerHouseInfo[] houseInfos)
    {
        if (RemoveStartingLocations)
        {
            if (GameModeMap.EnforceMaxPlayers)
                return;

            IniSection waypointSection = mapIni.GetSection("Waypoints");
            if (waypointSection == null)
                return;

            for (int i = 0; i <= 7; i++)
            {
                int index = waypointSection.Keys.FindIndex(k => !string.IsNullOrEmpty(k.Key) && k.Key == i.ToString());
                if (index > -1)
                    waypointSection.Keys.RemoveAt(index);
            }
        }

        bool[] startingLocationUsed = new bool[MAX_PLAYER_COUNT];
        bool stackedStartingLocations = false;
        foreach (PlayerHouseInfo houseInfo in houseInfos)
        {
            if (houseInfo.RealStartingWaypoint > -1)
            {
                startingLocationUsed[houseInfo.RealStartingWaypoint] = true;
                if (houseInfo.StartingWaypoint == -1)
                    stackedStartingLocations = true;
            }
        }

        if (!stackedStartingLocations)
            return;

        IniFile spawnIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS));

        for (int pId = 0; pId < houseInfos.Length; pId++)
        {
            PlayerHouseInfo houseInfo = houseInfos[pId];
            if (houseInfo.RealStartingWaypoint > -1 && houseInfo.StartingWaypoint == -1)
            {
                int unusedLocation = -1;
                for (int i = 0; i < startingLocationUsed.Length; i++)
                {
                    if (!startingLocationUsed[i])
                    {
                        unusedLocation = i;
                        startingLocationUsed[i] = true;
                        break;
                    }
                }

                houseInfo.StartingWaypoint = unusedLocation;
                mapIni.SetIntValue("Waypoints", unusedLocation.ToString(),
                    mapIni.GetIntValue("Waypoints", houseInfo.RealStartingWaypoint.ToString(), 0));
                spawnIni.SetIntValue("SpawnLocations", $"Multi{pId + 1}", unusedLocation);
            }
        }

        spawnIni.WriteIniFile();
    }

    private void InitializeMatchStatistics(PlayerHouseInfo[] houseInfos)
    {
        matchStatistics = new MatchStatistics(ProgramConstants.GAME_VERSION, UniqueGameID,
            Map.UntranslatedName, GameMode.UntranslatedUIName, Players.Count, GameModeMap.IsCoop);

        bool isValidForStar = true;
        foreach (var cb in CheckBoxes)
        {
            if (!cb.Setting.AllowScoring)
            {
                isValidForStar = false;
                break;
            }
        }
        if (isValidForStar)
        {
            foreach (var dd in DropDowns)
            {
                if (!dd.Setting.AllowScoring)
                {
                    isValidForStar = false;
                    break;
                }
            }
        }

        matchStatistics.IsValidForStar = isValidForStar;

        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];
            matchStatistics.AddPlayer(pInfo.Name, pInfo.Name == ProgramConstants.PLAYERNAME,
                false, pInfo.SideId == SideCount + RandomSelectorCount, houseInfos[pId].SideIndex + 1, pInfo.TeamId,
                MPColors.FindIndex(c => c.GameColorIndex == houseInfos[pId].ColorIndex), 10);
        }

        for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
        {
            var pHouseInfo = houseInfos[Players.Count + aiId];
            PlayerInfo aiInfo = AIPlayers[aiId];
            matchStatistics.AddPlayer("Computer", false, true, false,
                pHouseInfo.SideIndex + 1, aiInfo.TeamId,
                MPColors.FindIndex(c => c.GameColorIndex == pHouseInfo.ColorIndex),
                aiInfo.AILevel);
        }
    }

    // --- Game process ---

    private void OnGameProcessExited()
    {
        UIThreadMarshaller.AddCallback(new Action(() =>
        {
            Logger.Log("GameProcessExited: Parsing statistics.");
            matchStatistics?.ParseStatistics(ProgramConstants.GamePath, ClientConfiguration.Instance.LocalGame, false);

            Logger.Log("GameProcessExited: Adding match to statistics.");
            StatisticsManager.Instance.AddMatchAndSaveDatabase(true, matchStatistics);

            ClearReadyStatuses();
            CopyPlayerDataToUI();
            UpdateDiscordPresence(true);
        }));
    }

    // --- Discord ---

    protected abstract void UpdateDiscordPresence(bool resetTimer = false);

    protected void ResetDiscordPresence() => DiscordHandler.UpdatePresence();

    // --- Abstract / virtual methods for subclasses ---

    protected abstract bool AllowPlayerOptionsChange();

    protected abstract bool UpdateLaunchGameButtonStatus();

    protected abstract void AddNotice(string message);

    protected virtual void KickPlayer(int playerIndex) { }

    protected virtual void BanPlayer(int playerIndex) { }

    protected virtual string GetIPAddressForPlayer(PlayerInfo player) => "0.0.0.0";

    protected virtual void WriteSpawnIniAdditions(IniFile iniFile) { }

    protected virtual List<TeamStartMapping> GetTeamStartMappings() => new();

    protected virtual void OnSavePresetRequested() { }

    protected virtual void OnLoadPresetRequested() { }

    // --- Launch command (subclasses provide implementation) ---

    [RelayCommand]
    protected virtual void LeaveGame()
    {
        // Subclasses override
    }

    [RelayCommand]
    protected virtual void LaunchGame()
    {
        // Subclasses override
    }

    [RelayCommand]
    private void OpenGameSettings()
    {
        // Subclasses override to show settings UI
    }

    [RelayCommand]
    private void OpenMapSelection()
    {
        // Subclasses override to show map selection UI
    }

    // --- IsHost (abstract, subclasses set) ---
    public abstract bool IsHost { get; }

    // --- Helper ---
    protected string AILevelToName(int aiLevel) => ProgramConstants.GetAILevelName(aiLevel);
}
