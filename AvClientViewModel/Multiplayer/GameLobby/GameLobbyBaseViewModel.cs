using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using AvClientMvvmContract;
using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.Mvvm;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Domain;
using AvClientViewModel.Domain.Multiplayer;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.Settings;
using ClientCore.Statistics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Multiplayer.GameLobby;


/// <summary>
/// Abstract base ViewModel for all game lobbies (Skirmish, LAN, CnCNet).
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
    protected IClipboardService ClipboardService { get; }
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
    protected GameModeMapFilter gameModeMapFilter;
    private bool searchAllGameModes;

    // Internal game option lists (GameSessionSetting)
    public List<GameSessionSetting> CheckBoxSettings { get; } = new();
    public List<GameSessionSetting> DropDownSettings { get; } = new();

    // --- Observable state for View binding ---

    [ObservableProperty]
    public partial string MapName { get; set; } = "Map: Unknown".L10N("Client:Main:MapUnknown");

    [ObservableProperty]
    public partial string MapAuthor { get; set; } = "By Unknown Author".L10N("Client:Main:AuthorByUnknown");

    [ObservableProperty]
    public partial string GameModeName { get; set; } = "Game mode: Unknown".L10N("Client:Main:GameModeUnknown");

    [ObservableProperty]
    public partial string MapSize { get; set; } = "Size: Not available".L10N("Client:Main:MapSizeUnknown");

    [ObservableProperty]
    public partial string GameName { get; set; } = string.Empty;

    private readonly CovariantReadOnlyObservableCollectionAdapter<MapListItem, IMapListItem> _mapListItemsAdapter = new();
    public ObservableCollection<MapListItem> MapListItems => _mapListItemsAdapter.Source;
    IReadOnlyList<IMapListItem> IGameLobbyViewModel.MapListItems => _mapListItemsAdapter.Target;

    [ObservableProperty]
    public partial int SelectedMapIndex { get; set; } = -1;

    [ObservableProperty]
    public partial IReadOnlyList<string> GameModeFilterOptions { get; set; } = Array.Empty<string>();

    [ObservableProperty]
    public partial int SelectedGameModeFilterIndex { get; set; }

    [ObservableProperty]
    public partial string MapSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? MapListTooltipText { get; set; }

    [ObservableProperty]
    public partial int SortDirectionState { get; set; }

    [ObservableProperty]
    public partial bool IsMapSortButtonVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsMapSortButtonEnabled { get; set; } = true;

    private readonly AvClientMvvmContract.Mvvm.CovariantObservableCollectionAdapter<PlayerSlotObservable, IPlayerSlotObservable> _playerSlotsAdapter = new();
    ObservableCollection<IPlayerSlotObservable> IGameLobbyViewModel.PlayerSlots => _playerSlotsAdapter.Target;
    protected ObservableCollection<PlayerSlotObservable> PlayerSlots => _playerSlotsAdapter.Source;

    private readonly CovariantReadOnlyObservableCollectionAdapter<GameOptionCheckBox, IGameOptionCheckBox> _checkBoxesAdapter = new();
    public ObservableCollection<GameOptionCheckBox> CheckBoxes => _checkBoxesAdapter.Source;
    IReadOnlyList<IGameOptionCheckBox> IGameLobbyViewModel.CheckBoxes => _checkBoxesAdapter.Target;

    private readonly CovariantReadOnlyObservableCollectionAdapter<GameOptionDropDown, IGameOptionDropDown> _dropDownsAdapter = new();
    public ObservableCollection<GameOptionDropDown> DropDowns => _dropDownsAdapter.Source;
    IReadOnlyList<IGameOptionDropDown> IGameLobbyViewModel.DropDowns => _dropDownsAdapter.Target;

    [ObservableProperty]
    public partial int LaunchButtonRank { get; set; }

    [ObservableProperty]
    public partial string LaunchButtonText { get; set; } = "Launch Game".L10N("Client:Main:ButtonLaunchGame");

    [ObservableProperty]
    public partial bool CanLaunchGame { get; set; } = true;

    [ObservableProperty]
    public partial IReadOnlyList<string> PlayerNames { get; set; } = Array.Empty<string>();

    private readonly CovariantReadOnlyObservableCollectionAdapter<ContextMenuItem, IContextMenuItem> _startingLocationAssignMenuItemsAdapter = new();
    public ObservableCollection<ContextMenuItem> StartingLocationAssignMenuItems => _startingLocationAssignMenuItemsAdapter.Source;
    IReadOnlyList<IContextMenuItem> IGameLobbyViewModel.StartingLocationAssignMenuItems => _startingLocationAssignMenuItemsAdapter.Target;

    private readonly CovariantReadOnlyObservableCollectionAdapter<ContextMenuItem, IContextMenuItem> _mapContextMenuItemsAdapter = new();
    public ObservableCollection<ContextMenuItem> MapContextMenuItems => _mapContextMenuItemsAdapter.Source;
    IReadOnlyList<IContextMenuItem> IGameLobbyViewModel.MapContextMenuItems => _mapContextMenuItemsAdapter.Target;

    [ObservableProperty]
    public partial int SelectedPlayerIndex { get; set; }

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
            OnPlayerExtraOptionsChanged();
        }
    }

    protected virtual void OnPlayerExtraOptionsChanged() { }

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

    protected readonly MapPreviewBoxViewModel mapPreviewBox;
    IMapPreviewBoxViewModel IGameLobbyViewModel.MapPreviewBox => mapPreviewBox;

    protected GameLobbyBaseViewModel(
        MapLoader mapLoader,
        DiscordHandler discordHandler,
        IGameProcessService gameProcessService,
        IUIThreadMarshaller uiThreadMarshaller,
        IClipboardService clipboardService,
        Random random)
    {
        MapLoader = mapLoader;
        DiscordHandler = discordHandler;
        GameProcessService = gameProcessService;
        UIThreadMarshaller = uiThreadMarshaller;
        ClipboardService = clipboardService;
        this.random = random;

        mapPreviewBox = new MapPreviewBoxViewModel(mapLoader, uiThreadMarshaller);
        mapPreviewBox.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IMapPreviewBoxViewModel.SelectedStartingLocationIndex))
                BuildStartingLocationAssignMenuItems();
        };

        // Initialize player slots (must be done in constructor, not Initialize, because
        // CopyPlayerDataToUI can be called from SetUp before Initialize runs)
        MPColors = MultiplayerColor.LoadColors();
        mapPreviewBox.SetMPColors(MPColors);

        GameOptionsIni = new IniFile(SafePath.CombineFilePath(
            ProgramConstants.GetBaseResourcePath(), ClientConfiguration.GAME_OPTIONS));

        string[] sides = ClientConfiguration.Instance.Sides.Split(',').ToArray();
        SideCount = sides.Length;

        List<string> selectorNames = new();
        GetRandomSelectors(selectorNames, RandomSelectors);
        RandomSelectorCount = RandomSelectors.Count + 1;

        PlayerSlots.Clear();
        for (int i = 0; i < MAX_PLAYER_COUNT; i++)
        {
            var slot = new PlayerSlotObservable();
            int slotIdx = i;
            slot.Name.PropertyChanged += (s, e) => PlayerSlotDropdown_PropertyChanged(slotIdx, e, clearReadyStatuses: true);
            slot.Side.PropertyChanged += (s, e) => PlayerSlotDropdown_PropertyChanged(slotIdx, e, clearReadyStatuses: true);
            slot.Color.PropertyChanged += (s, e) => PlayerSlotDropdown_PropertyChanged(slotIdx, e, clearReadyStatuses: false);
            slot.Start.PropertyChanged += (s, e) => PlayerSlotDropdown_PropertyChanged(slotIdx, e, clearReadyStatuses: true);
            slot.Team.PropertyChanged += (s, e) => PlayerSlotDropdown_PropertyChanged(slotIdx, e, clearReadyStatuses: true);
            InitPlayerSlotOptions(slot, sides, selectorNames);
            PlayerSlots.Add(slot);
        }

        LoadGameOptions();
        RefreshGameOptionWrappers();
    }

    // --- Game options INI loading ---

    /// <summary>
    /// Loads game option checkbox and dropdown definitions from INI files.
    /// Override in subclass lobbies to load additional/replacement INI files.
    /// </summary>
    protected virtual void LoadGameOptions()
    {
        LoadGameOptionsFromIni("GameLobbyBase.ini");
    }

    /// <summary>
    /// Parses game option checkbox/dropdown definitions from the specified INI file
    /// and populates <see cref="CheckBoxSettings"/> and <see cref="DropDownSettings"/>.
    /// If the same control name already exists, the new values override the old ones
    /// (allowing subclass INIs to override base defaults).
    /// </summary>
    protected void LoadGameOptionsFromIni(string iniFileName)
    {
        string iniPath = SafePath.CombineFilePath(
            ProgramConstants.GetBaseResourcePath(), iniFileName);
        var ini = new IniFile(iniPath);

        var panelSection = ini.GetSection("GameOptionsPanel");
        if (panelSection == null)
            return;

        foreach (var keyInfo in panelSection.Keys)
        {
            string key = keyInfo.Key;
            if (string.IsNullOrEmpty(key) || !key.StartsWith("$CC", StringComparison.Ordinal))
                continue;

            string value = keyInfo.Value;
            int colonIdx = value.IndexOf(':');
            if (colonIdx < 0)
                continue;

            string controlName = value.Substring(0, colonIdx);
            string controlType = value.Substring(colonIdx + 1);

            if (controlType == "GameLobbyCheckBox")
            {
                var setting = CheckBoxSettings.Find(s => s.Name == controlName);
                if (setting == null)
                {
                    setting = new GameSessionSetting { Name = controlName };
                    CheckBoxSettings.Add(setting);
                }
                ParseCheckBoxSetting(setting, ini, controlName);
            }
            else if (controlType == "GameLobbyDropDown")
            {
                var setting = DropDownSettings.Find(s => s.Name == controlName);
                if (setting == null)
                {
                    setting = new GameSessionSetting { Name = controlName };
                    DropDownSettings.Add(setting);
                }
                ParseDropDownSetting(setting, ini, controlName);
            }
        }
    }

    private static void ParseCheckBoxSetting(GameSessionSetting setting, IniFile ini, string sectionName)
    {
        setting.SpawnIniOption = ini.GetStringValue(sectionName, "SpawnIniOption", string.Empty);
        setting.CustomIniPath = ini.GetStringValue(sectionName, "CustomIniPath", string.Empty);
        setting.AffectsSpawnIni = !string.IsNullOrEmpty(setting.SpawnIniOption);
        setting.AffectsMapCode = !string.IsNullOrEmpty(setting.CustomIniPath);
        setting.Reversed = ini.GetBooleanValue(sectionName, "Reversed", false);
        setting.EnabledSpawnIniValue = ini.GetStringValue(sectionName, "EnabledSpawnIniValue", "True");
        setting.DisabledSpawnIniValue = ini.GetStringValue(sectionName, "DisabledSpawnIniValue", "False");
        setting.Text = ini.GetStringValue(sectionName, "Text", string.Empty);
        setting.BroadcastToLobby = ini.GetBooleanValue(sectionName, "BroadcastToLobby", false);

        bool checkedValue = ini.GetBooleanValue(sectionName, "Checked", false);
        setting.Value = checkedValue ? 1 : 0;

        string mapScoringModeStr = ini.GetStringValue(sectionName, "MapScoringMode", "Irrelevant");
        setting.MapScoringMode = mapScoringModeStr switch
        {
            "DenyWhenChecked" => CheckBoxMapScoringMode.DenyWhenChecked,
            "DenyWhenUnchecked" => CheckBoxMapScoringMode.DenyWhenUnchecked,
            _ => CheckBoxMapScoringMode.Irrelevant
        };

        string disallowedStr = ini.GetStringValue(sectionName, "DisallowedSideIndices", string.Empty);
        if (string.IsNullOrEmpty(disallowedStr))
            disallowedStr = ini.GetStringValue(sectionName, "DisallowedSideIndex", string.Empty);
        if (!string.IsNullOrEmpty(disallowedStr))
        {
            var parts = disallowedStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            setting.DisallowedSideIndices = new List<int>();
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int idx) && !setting.DisallowedSideIndices.Contains(idx))
                    setting.DisallowedSideIndices.Add(idx);
            }
        }
    }

    private static void ParseDropDownSetting(GameSessionSetting setting, IniFile ini, string sectionName)
    {
        string itemsStr = ini.GetStringValue(sectionName, "Items", string.Empty);
        if (!string.IsNullOrEmpty(itemsStr))
        {
            var rawItems = itemsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).ToList();
            setting.DropDownItemTags = new List<string>(rawItems);

            string itemLabelsStr = ini.GetStringValue(sectionName, "ItemLabels", string.Empty);
            if (!string.IsNullOrEmpty(itemLabelsStr))
            {
                var labels = itemLabelsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).ToList();
                // Pad labels if fewer than items
                while (labels.Count < rawItems.Count)
                    labels.Add(rawItems[labels.Count]);
                setting.DropDownDisplayItems = labels;
            }
            else
            {
                setting.DropDownDisplayItems = new List<string>(rawItems);
            }
        }

        string dataWriteModeStr = ini.GetStringValue(sectionName, "DataWriteMode", "String");
        setting.DataWriteMode = dataWriteModeStr.ToUpperInvariant() switch
        {
            "INDEX" => DropDownDataWriteMode.INDEX,
            "BOOLEAN" => DropDownDataWriteMode.BOOLEAN,
            "MAPCODE" => DropDownDataWriteMode.MAPCODE,
            _ => DropDownDataWriteMode.STRING
        };

        setting.AffectsSpawnIni = setting.DataWriteMode != DropDownDataWriteMode.MAPCODE;
        setting.AffectsMapCode = setting.DataWriteMode == DropDownDataWriteMode.MAPCODE;
        setting.SpawnIniOption = ini.GetStringValue(sectionName, "SpawnIniOption", string.Empty);
        setting.OptionName = ini.GetStringValue(sectionName, "OptionName", string.Empty);
        setting.BroadcastToLobby = ini.GetBooleanValue(sectionName, "BroadcastToLobby", false);

        int defaultIndex = ini.GetIntValue(sectionName, "DefaultIndex", 0);
        setting.Value = defaultIndex;
    }

    // --- Lifecycle ---

    public virtual void Initialize()
    {
        // Initialize game mode filter (requires StatisticsManager which is not
        // available during DI construction)
        RefreshGameModeFilter();

        // Subscribe to map changes
        MapLoader.MapChanged += MapLoader_MapChanged;

        // Load default map
        LoadDefaultGameModeMap();
    }

    protected virtual void Clean()
    {
        MapLoader.MapChanged -= MapLoader_MapChanged;
        GameProcessService.GameProcessExited -= OnGameProcessExited;
    }

    // --- Player slot helpers ---

    /// <summary>
    /// Returns the typed option at the given index, or default if out of range.
    /// </summary>
    protected static T? OptionAt<T>(ObservableCollection<T> options, int index)
    {
        return index >= 0 && index < options.Count ? options[index] : default;
    }

    // --- Player slot initialization ---

    private static ObservableCollection<IPlayerName> CreateNameOptionsWithFirstPlayerName(string firstPlayerName)
    {
        var nameOptions = new ObservableCollection<IPlayerName> { new PlayerNameOption { Index = 0, Name = firstPlayerName } };
        for (int i = 0; i < ProgramConstants.AI_PLAYER_NAMES.Count; i++)
            nameOptions.Add(new PlayerNameOption { Index = i + 1, Name = ProgramConstants.AI_PLAYER_NAMES[i] });
        return nameOptions;
    }

    private void InitPlayerSlotOptions(IPlayerSlotObservable slot, string[] sides, List<string> selectorNames)
    {
        // Name options: empty + AI names
        var nameOptions = new ObservableCollection<IPlayerName> { new PlayerNameOption { Index = 0, Name = string.Empty } };
        for (int i = 0; i < ProgramConstants.AI_PLAYER_NAMES.Count; i++)
            nameOptions.Add(new PlayerNameOption { Index = i + 1, Name = ProgramConstants.AI_PLAYER_NAMES[i] });
        slot.Name.Options = nameOptions;

        // Side options: Random + selectors + sides
        var sideOptions = new ObservableCollection<IPlayerSide>();
        int sideIdx = 0;
        sideOptions.Add(new PlayerSideOption { Index = sideIdx++, Name = "Random".L10N("Client:Sides:RandomSide") });
        foreach (var sn in selectorNames)
            sideOptions.Add(new PlayerSideOption { Index = sideIdx++, Name = sn });
        foreach (var s in sides.Select(s => s.L10N($"INI:Sides:{s}")))
            sideOptions.Add(new PlayerSideOption { Index = sideIdx++, Name = s });
        slot.Side.Options = sideOptions;
        slot.Side.Selectable = new ObservableCollection<bool>(Enumerable.Repeat(true, sideOptions.Count));

        // Color options: Random + MPColors
        var colorOptions = new ObservableCollection<IPlayerColor>();
        int colorIdx = 0;
        colorOptions.Add(new PlayerColorOption { Index = colorIdx++, Name = "Random".L10N("Client:Main:RandomColor"), Color = 0xFFFFFFFF });
        foreach (var c in MPColors)
        {
            uint argb = 0xFF000000 | ((uint)c.Color.R << 16) | ((uint)c.Color.G << 8) | c.Color.B;
            colorOptions.Add(new PlayerColorOption { Index = colorIdx++, Name = c.Name, Color = argb });
        }
        slot.Color.Options = colorOptions;
        slot.Color.Selectable = new ObservableCollection<bool>(Enumerable.Repeat(true, colorOptions.Count));

        // Start options
        var startOptions = new ObservableCollection<IPlayerStart> { new PlayerStartOption { Index = 0, Name = "???" } };
        for (int i = 1; i <= MAX_PLAYER_COUNT; i++)
            startOptions.Add(new PlayerStartOption { Index = i, Name = i.ToString() });
        slot.Start.Options = startOptions;
        slot.Start.Selectable = new ObservableCollection<bool>(Enumerable.Repeat(true, startOptions.Count));

        // Team options
        var teamOptions = new ObservableCollection<IPlayerTeam> { new PlayerTeamOption { Index = 0, Name = "-" } };
        for (int i = 0; i < ProgramConstants.TEAMS.Count; i++)
            teamOptions.Add(new PlayerTeamOption { Index = i + 1, Name = ProgramConstants.TEAMS[i] });
        slot.Team.Options = teamOptions;
    }

    // --- Game option wrapper refresh ---

    private void RefreshGameOptionWrappers()
    {
        // Unsubscribe from old items
        foreach (var cb in CheckBoxes)
            cb.PropertyChanged -= GameOptionCheckBox_PropertyChanged;
        foreach (var dd in DropDowns)
            dd.PropertyChanged -= GameOptionDropDown_PropertyChanged;

        CheckBoxes.Clear();
        foreach (var cb in CheckBoxSettings.Select(s => new GameOptionCheckBox(s)))
            CheckBoxes.Add(cb);
        DropDowns.Clear();
        foreach (var s in DropDownSettings)
        {
            var dd = new GameOptionDropDown(s);
            if (s.DropDownDisplayItems != null)
                dd.Items = s.DropDownDisplayItems;
            else if (s.DropDownItemTags != null)
                dd.Items = s.DropDownItemTags;
            DropDowns.Add(dd);
        }

        // Subscribe to new items
        foreach (var cb in CheckBoxes)
            cb.PropertyChanged += GameOptionCheckBox_PropertyChanged;
        foreach (var dd in DropDowns)
            dd.PropertyChanged += GameOptionDropDown_PropertyChanged;
    }

    private void GameOptionCheckBox_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(GameOptionCheckBox.IsChecked))
            return;
        if (disableGameOptionUpdateBroadcast)
            return;

        var cb = (GameOptionCheckBox)sender!;
        cb.HostChecked = cb.IsChecked;
        cb.UserChecked = cb.IsChecked;
        OnGameOptionChanged();
    }

    private void GameOptionDropDown_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(GameOptionDropDown.SelectedIndex))
            return;
        if (disableGameOptionUpdateBroadcast)
            return;

        var dd = (GameOptionDropDown)sender!;
        dd.HostSelectedIndex = dd.SelectedIndex;
        dd.UserSelectedIndex = dd.SelectedIndex;
        OnGameOptionChanged();
    }

    // --- Map management ---

    private void MapLoader_MapChanged(object sender, MapChangedEventArgs e)
    {
        UIThreadMarshaller.AddCallback(new Action(() =>
        {
            switch (e.ChangeType)
            {
                case MapChangeType.Added:
                    UI_HandleMapAdded(e.Map);
                    break;
                case MapChangeType.Updated:
                    UI_HandleMapUpdated(e.Map, e.PreviousMapSHA1);
                    break;
                case MapChangeType.Removed:
                    UI_HandleMapRemoved(e.Map);
                    break;
            }
        }));
    }

    protected virtual void UI_HandleMapAdded(Map addedMap)
    {
        RefreshGameModeFilter();
        if (ShouldShowMapInCurrentFilter(addedMap))
            ListMaps();
    }

    protected virtual void UI_HandleMapUpdated(Map updatedMap, string previousSHA1)
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

    private void UI_HandleMapRemoved(Map removedMap)
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
                RankTexturePath = rankIndex switch
                {
                    1 => SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "rankEasy.png"),
                    2 => SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "rankNormal.png"),
                    3 => SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "rankHard.png"),
                    _ => null
                },
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

        MapListItems.Clear();
        foreach (var item in items)
            MapListItems.Add(item);

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

    /// <summary>
    /// Called by the View when the hovered index in the map list changes.
    /// Updates the tooltip text for the map list.
    /// </summary>
    [RelayCommand]
    private void SetHoveredMapIndex(int hoveredIndex)
    {
        if (hoveredIndex < 0 || hoveredIndex >= MapListItems.Count)
        {
            MapListTooltipText = null;
            return;
        }

        var gmm = (MapListItems[hoveredIndex]).Source;
        if (gmm.Map.UntranslatedName != gmm.Map.Name)
            MapListTooltipText = "Original name:".L10N("Client:Main:OriginalMapName") + " " + gmm.Map.UntranslatedName;
        else
            MapListTooltipText = null;
    }

    partial void OnSelectedGameModeFilterIndexChanged(int value)
    {
        if (value < 0 || value >= GameModeFilterOptions.Count)
            return;

        // Rebuild the filter from the options
        RefreshGameModeFilterFromIndex(value);
        MapSearchText = string.Empty;
        // Reset selected map before ListMaps so ListMaps doesn't try to
        // match the old GameModeMap (which may not exist in the new list).
        SelectedMapIndex = -1;
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
        selectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

        if (SelectedGameModeFilterIndex == selectedIndex)
        {
            // Force refresh even if index didn't change
            RefreshGameModeFilterFromIndex(selectedIndex);
            MapSearchText = string.Empty;
            ListMaps();

            if (SelectedMapIndex == -1)
                SelectedMapIndex = 0;
            else
                ChangeMap(GameModeMap);
        }
        else
        {
            SelectedGameModeFilterIndex = selectedIndex;
        }
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
            mapPreviewBox.SetGameModeMap(null);
            OnGameOptionChanged();
            return;
        }

        disableGameOptionUpdateBroadcast = true;

        // Reset forced options
        foreach (GameOptionCheckBox cb in CheckBoxes)
            cb.IsEnabled = true;
        foreach (GameOptionDropDown dd in DropDowns)
            dd.IsEnabled = true;

        // Enable all sides and colors by default
        foreach (var slot in PlayerSlots)
        {
            slot.Side.Selectable = new ObservableCollection<bool>(Enumerable.Repeat(true, slot.Side.Selectable.Count));
            slot.Color.Selectable = new ObservableCollection<bool>(Enumerable.Repeat(true, slot.Color.Selectable.Count));
        }

        // Update start location selectable flags per map (don't replace Options - original XNA never does)
        foreach (var slot in PlayerSlots)
        {
            var startOptions = slot.Start.Options;
            // Rebuild selectable list: index 0 ("???") always selectable,
            // indices 1-8 selectable only if in AllowedStartingLocations
            var selectable = new ObservableCollection<bool> { true };
            for (int i = 1; i <= MAX_PLAYER_COUNT; i++)
                selectable.Add(GameModeMap.AllowedStartingLocations.Contains(i));
            slot.Start.Selectable = selectable;
        }

        // Check if AI players allowed
        bool aiAllowed = !GameModeMap.HumanPlayersOnly;
        if (!aiAllowed)
            AIPlayers.Clear();

        // Clone lists to track which options were NOT forced
        var checkBoxListClone = CheckBoxes.ToList();
        var dropDownListClone = DropDowns.ToList();

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
        mapPreviewBox.SetGameModeMap(gameModeMap);
        mapPreviewBox.SetPlayers(Players, AIPlayers);
        mapPreviewBox.UpdateStartingLocationIndicators();
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
            BuildMapContextMenuItems();
            return;
        }

        MapName = "Map:".L10N("Client:Main:Map") + " " + Map.Name;
        MapAuthor = "By".L10N("Client:Main:AuthorBy") + " " + Map.Author;
        GameModeName = "Game mode:".L10N("Client:Main:GameModeLabel") + " " + GameMode.UIName;
        MapSize = "Size:".L10N("Client:Main:MapSize") + " " + Map.GetSizeString();
        BuildMapContextMenuItems();
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
        UI_ClearReadyStatuses(resetAutoReady);
    }

    protected void UI_ClearReadyStatuses(bool resetAutoReady = false)
    {
        for (int i = 1; i < Players.Count; i++)
        {
            if (resetAutoReady || !Players[i].AutoReady || Players[i].IsInGame)
                Players[i].Ready = false;
        }
    }

    protected virtual void CopyPlayerDataToUI()
    {
        UI_CopyPlayerDataToUI();
    }

    protected void UI_CopyPlayerDataToUI()
    {
        PlayerUpdatingInProgress = true;

        var slots = PlayerSlots;
        bool allowOptionsChange = AllowPlayerOptionsChange();
        var extraOpts = playerExtraOptions;

        if (Players.Count > MAX_PLAYER_COUNT)
            throw new Exception($"Player count exceeds maximum of {MAX_PLAYER_COUNT}. How could this happen?");

        Debug.Assert(PlayerSlots.Count >= Players.Count + AIPlayers.Count, "PlayerSlots count should not be less than total player count");

        // Human players
        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];
            pInfo.Index = pId;
            var slot = slots[pId];

            slot.PlayerName = pInfo.Name;
            slot.Name.Options = CreateNameOptionsWithFirstPlayerName(pInfo.Name);
            slot.Name.SelectedOption = OptionAt(slot.Name.Options, 0);
            slot.Name.IsEnabled = false;

            bool allowPlayerOptionsChange = allowOptionsChange || pInfo.Name == ProgramConstants.PLAYERNAME;

            // Apply PlayerExtraOptions: force random sides
            if (extraOpts.IsForceRandomSides && pInfo.SideId != 0)
                pInfo.SideId = 0;

            slot.Side.SelectedOption = OptionAt(slot.Side.Options, pInfo.SideId);
            slot.Side.IsEnabled = !extraOpts.IsForceRandomSides && allowPlayerOptionsChange;

            // Apply PlayerExtraOptions: force random colors
            if (extraOpts.IsForceRandomColors && pInfo.ColorId != 0)
                pInfo.ColorId = 0;

            slot.Color.SelectedOption = OptionAt(slot.Color.Options, pInfo.ColorId);
            slot.Color.IsEnabled = !extraOpts.IsForceRandomColors && allowPlayerOptionsChange;

            // Apply PlayerExtraOptions: force random starts
            if (extraOpts.IsForceRandomStarts && pInfo.StartingLocation != 0)
                pInfo.StartingLocation = 0;

            slot.Start.SelectedOption = OptionAt(slot.Start.Options, pInfo.StartingLocation);

            // Apply PlayerExtraOptions: force no teams
            if (extraOpts.IsForceNoTeams && pInfo.TeamId != 0)
                pInfo.TeamId = 0;

            slot.Team.SelectedOption = OptionAt(slot.Team.Options, pInfo.TeamId);

            if (GameModeMap != null)
            {
                slot.Team.IsEnabled = !extraOpts.IsForceNoTeams && allowPlayerOptionsChange && !GameModeMap.IsCoop && !GameModeMap.ForceNoTeams;
                slot.Start.IsEnabled = !extraOpts.IsForceRandomStarts && allowPlayerOptionsChange && !GameModeMap.ForceRandomStartLocations;
            }
        }

        // AI players
        for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
        {
            PlayerInfo aiInfo = AIPlayers[aiId];
            int index = Players.Count + aiId;
            aiInfo.Index = index;
            var slot = slots[index];

            slot.Name.Options = CreateNameOptionsWithFirstPlayerName("-");
            slot.Name.SelectedOption = OptionAt(slot.Name.Options, 1 + aiInfo.AILevel);
            slot.Name.IsEnabled = allowOptionsChange;

            // Apply PlayerExtraOptions: force random sides
            if (extraOpts.IsForceRandomSides && aiInfo.SideId != 0)
                aiInfo.SideId = 0;

            slot.Side.SelectedOption = OptionAt(slot.Side.Options, aiInfo.SideId);
            slot.Side.IsEnabled = !extraOpts.IsForceRandomSides && allowOptionsChange;

            // Apply PlayerExtraOptions: force random colors
            if (extraOpts.IsForceRandomColors && aiInfo.ColorId != 0)
                aiInfo.ColorId = 0;

            slot.Color.SelectedOption = OptionAt(slot.Color.Options, aiInfo.ColorId);
            slot.Color.IsEnabled = !extraOpts.IsForceRandomColors && allowOptionsChange;

            // Apply PlayerExtraOptions: force random starts
            if (extraOpts.IsForceRandomStarts && aiInfo.StartingLocation != 0)
                aiInfo.StartingLocation = 0;

            slot.Start.SelectedOption = OptionAt(slot.Start.Options, aiInfo.StartingLocation);

            // Apply PlayerExtraOptions: force no teams
            if (extraOpts.IsForceNoTeams && aiInfo.TeamId != 0)
                aiInfo.TeamId = 0;

            slot.Team.SelectedOption = OptionAt(slot.Team.Options, aiInfo.TeamId);

            if (GameModeMap != null)
            {
                slot.Team.IsEnabled = !extraOpts.IsForceNoTeams && allowOptionsChange && !GameModeMap.IsCoop && !GameModeMap.ForceNoTeams;
                slot.Start.IsEnabled = !extraOpts.IsForceRandomStarts && allowOptionsChange && !GameModeMap.ForceRandomStartLocations;
            }
        }

        // Unused slots
        for (int ddIndex = Players.Count + AIPlayers.Count; ddIndex < MAX_PLAYER_COUNT; ddIndex++)
        {
            var slot = slots[ddIndex];
            slot.Name.SelectedOption = default;
            slot.Name.IsEnabled = false;
            slot.Side.SelectedOption = default;
            slot.Side.IsEnabled = false;
            slot.Color.SelectedOption = default;
            slot.Color.IsEnabled = false;
            slot.Start.SelectedOption = default;
            slot.Start.IsEnabled = false;
            slot.Team.SelectedOption = default;
            slot.Team.IsEnabled = false;
        }

        // Enable adding AI to the next slot
        if (allowOptionsChange && Players.Count + AIPlayers.Count < MAX_PLAYER_COUNT)
            slots[Players.Count + AIPlayers.Count].Name.IsEnabled = true;

        CheckDisallowedSides();

        // Update PlayerNames for compatibility
        PlayerNames = Players.Select(p => p.Name).ToList();

        mapPreviewBox.UpdateStartingLocationIndicators();

        PlayerUpdatingInProgress = false;
    }

    private void PlayerSlotDropdown_PropertyChanged(int slotIdx, PropertyChangedEventArgs e, bool clearReadyStatuses = true)
    {
        if (PlayerUpdatingInProgress)
            return;
        if (e.PropertyName != nameof(PlayerSlotDropdown<IPlayerName>.SelectedOption))
            return;

        CopyPlayerDataFromUI(clearReadyStatuses);
    }

    protected virtual void CopyPlayerDataFromUI(bool clearReadyStatuses = true)
    {
        if (PlayerUpdatingInProgress)
            return;

        if (clearReadyStatuses)
            ClearReadyStatuses();

        var slots = PlayerSlots;

        var oldSideId = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId;

        if (Players.Count > MAX_PLAYER_COUNT)
            throw new Exception($"Player count exceeds maximum of {MAX_PLAYER_COUNT}.");

        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];
            var slot = slots[pId];

            pInfo.ColorId = slot.Color.SelectedOption?.Index ?? 0;
            pInfo.SideId = slot.Side.SelectedOption?.Index ?? 0;
            pInfo.StartingLocation = slot.Start.SelectedOption?.Index ?? 0;
            pInfo.TeamId = slot.Team.SelectedOption?.Index ?? 0;

            if (pInfo.SideId == SideCount + RandomSelectorCount)
                pInfo.StartingLocation = 0;
        }

        AIPlayers.Clear();
        for (int cmbId = Players.Count; cmbId < MAX_PLAYER_COUNT; cmbId++)
        {
            var slot = slots[cmbId];
            if (slot.Name.SelectedOption == null || slot.Name.SelectedOption.Index < 1)
                continue;

            string aiName = slot.Name.SelectedOption.Name;
            int aiLevel = slot.Name.SelectedOption.Index - 1;
            if (aiLevel < 0)
                aiLevel = 0;

            PlayerInfo aiPlayer = new PlayerInfo
            {
                Name = aiName,
                AILevel = aiLevel,
                SideId = slot.Side.SelectedOption?.Index ?? 0,
                ColorId = slot.Color.SelectedOption?.Index ?? 0,
                StartingLocation = slot.Start.SelectedOption?.Index ?? 0,
                TeamId = Map != null && GameModeMap.IsCoop ? 1 : (slot.Team.SelectedOption?.Index ?? 0),
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
                var sideSelectable = new ObservableCollection<bool>(slots[pInfo.Index].Side.Selectable);
                for (int i = 0; i < RandomSelectorCount; i++)
                    sideSelectable[i] = false;
                slots[pInfo.Index].Side.Selectable = sideSelectable;
            }
        }
        else
        {
            foreach (PlayerInfo pInfo in playerInfos)
            {
                var sideSelectable = new ObservableCollection<bool>(slots[pInfo.Index].Side.Selectable);
                for (int i = 0; i < RandomSelectorCount; i++)
                    sideSelectable[i] = true;
                slots[pInfo.Index].Side.Selectable = sideSelectable;
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
                var sideSelectable = new ObservableCollection<bool>(slots[pInfo.Index].Side.Selectable);
                sideSelectable[1 + c] = !disabled;
                slots[pInfo.Index].Side.Selectable = sideSelectable;

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
                    var sideSelectable = new ObservableCollection<bool>(slots[pInfo.Index].Side.Selectable);
                    sideSelectable[i + RandomSelectorCount] = false;
                    slots[pInfo.Index].Side.Selectable = sideSelectable;

                    if (pInfo.SideId == i + RandomSelectorCount)
                        pInfo.SideId = defaultSide;
                }
            }
            else
            {
                foreach (PlayerInfo pInfo in playerInfos)
                {
                    var sideSelectable = new ObservableCollection<bool>(slots[pInfo.Index].Side.Selectable);
                    sideSelectable[i + RandomSelectorCount] = true;
                    slots[pInfo.Index].Side.Selectable = sideSelectable;
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
                var sideSelectable = new ObservableCollection<bool>(slots[pInfo.Index].Side.Selectable);
                if (sideSelectable.Count > GetSpectatorSideIndex())
                    sideSelectable[GetSpectatorSideIndex()] = false;
                slots[pInfo.Index].Side.Selectable = sideSelectable;
            }
        }
        else
        {
            foreach (PlayerInfo pInfo in playerInfos)
            {
                var sideSelectable = new ObservableCollection<bool>(slots[pInfo.Index].Side.Selectable);
                if (sideSelectable.Count > SideCount + RandomSelectorCount)
                    sideSelectable[SideCount + RandomSelectorCount] = true;
                slots[pInfo.Index].Side.Selectable = sideSelectable;
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

        foreach (GameOptionCheckBox cb in CheckBoxes)
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
        Log.Information("PickRandomMap: Rolled " + randomValue + " out of " + gameModeMaps.Count + ". Picked map: " + GameModeMap.Map.Name);

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
    protected virtual void ToggleFavorite()
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
            Log.Warning($"Deleting map {Map.BaseFilePath} failed! Message: {ex}");
            AddNotice("Deleting map failed! Reason:".L10N("Client:Main:DeleteMapFailedText") + " " + ex.Message);
        }
    }

    [RelayCommand]
    private void ShowMapInFolder()
    {
        Map?.OpenContainingFolder();
    }

    [RelayCommand]
    private void CopyMapName()
    {
        if (Map != null)
            ClipboardService.SetTextAsync(Map.Name);
    }

    [RelayCommand]
    private void CopyOriginalMapName()
    {
        if (Map != null)
            ClipboardService.SetTextAsync(Map.UntranslatedName);
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

    private void BuildStartingLocationAssignMenuItems()
    {
        var items = new List<ContextMenuItem>();
        int menuId = 1;
        var playerSlots = PlayerSlots;
        var playerNames = PlayerNames;
        for (int i = 0; i < playerSlots.Count; i++)
        {
            var slot = playerSlots[i];
            if (slot.Name.SelectedOption == null || slot.Name.SelectedOption.Index < 1)
                continue;

            string playerName;
            if (i < playerNames.Count)
                playerName = playerNames[i];
            else
            {
                playerName = slot.PlayerName ?? string.Empty;
                if (slot.Name.SelectedOption != null)
                    playerName = slot.Name.SelectedOption.Name;
            }

            if (string.IsNullOrEmpty(playerName))
                continue;

            var displayName = $"{menuId}. {playerName}";
            var playerIndex = i;
            items.Add(new ContextMenuItem(displayName, new RelayCommand(() =>
            {
                mapPreviewBox.SelectedPlayerIndex = playerIndex;
                mapPreviewBox.AssignStartingLocationCommand.Execute(null);
            })));
            menuId++;
        }
        StartingLocationAssignMenuItems.Clear();
        foreach (var item in items)
            StartingLocationAssignMenuItems.Add(item);
    }

    private void BuildMapContextMenuItems()
    {
        var items = new List<ContextMenuItem>();

        bool isFavorite = GameModeMap?.IsFavorite ?? false;
        items.Add(new ContextMenuItem(
            isFavorite
                ? "Remove Favorite".L10N("Client:UI:RemoveFavorite")
                : "Add Favorite".L10N("Client:UI:AddFavorite"),
            ToggleFavoriteCommand));

        items.Add(new ContextMenuItem("", IsSeparator: true));

        items.Add(new ContextMenuItem(
            "Copy Map Name".L10N("Client:Main:CopyMapName"),
            CopyMapNameCommand));

        if (Map != null && Map.UntranslatedName != Map.Name)
        {
            items.Add(new ContextMenuItem(
                "Copy Original Name".L10N("Client:Main:CopyOriginalMapName"),
                CopyOriginalMapNameCommand));
        }

        items.Add(new ContextMenuItem("", IsSeparator: true));

        bool canDelete = Map != null && !Map.Official && !IsMultiplayer;
        items.Add(new ContextMenuItem(
            "Delete Map".L10N("Client:UI:DeleteMap"),
            DeleteMapCommand,
            IsVisible: canDelete));

        items.Add(new ContextMenuItem(
            "Show in Folder".L10N("Client:UI:ShowInFolder"),
            ShowMapInFolderCommand));

        MapContextMenuItems.Clear();
        foreach (var item in items)
            MapContextMenuItems.Add(item);
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

        foreach (GameOptionCheckBox cb in CheckBoxes)
        {
            if (!cb.Setting.AllowScoring)
                return Rank.None;
        }
        foreach (GameOptionDropDown dd in DropDowns)
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
        // One-shot subscription: only the lobby that launches the game receives the exit event
        GameProcessService.GameProcessExited += OnGameProcessExited;

        Random pseudoRandom = new Random(RandomSeed);

        PlayerHouseInfo[] houseInfos = WriteSpawnIni(pseudoRandom);
        InitializeMatchStatistics(houseInfos);
        WriteMap(houseInfos, pseudoRandom);

        GameProcessService.StartGameProcess();
        UpdateDiscordPresence(true);
    }

    private PlayerHouseInfo[] WriteSpawnIni(Random pseudoRandom)
    {
        Log.Information("Writing spawn.ini");

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

        foreach (GameOptionCheckBox cb in CheckBoxes)
            cb.Setting.ApplySpawnIniCode(spawnIni);
        foreach (GameOptionDropDown dd in DropDowns)
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

        Log.Information("Writing map.");
        Log.Information("Loading map INI from " + Map.CompleteFilePath);

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

        foreach (GameOptionCheckBox cb in CheckBoxes)
            cb.Setting.ApplyMapCode(mapIni, GameMode);
        foreach (GameOptionDropDown dd in DropDowns)
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
                Log.Warning(errorMessage);
                Log.Warning(ex.ToString());
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
                Log.Warning("Unable to delete supplemental map file".L10N("Client:Main:SupplementalFileDeleteError") + $" {path}");
                Log.Warning(ex.ToString());
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
        foreach (GameOptionCheckBox cb in CheckBoxes)
        {
            if (!cb.Setting.AllowScoring)
            {
                isValidForStar = false;
                break;
            }
        }
        if (isValidForStar)
        {
            foreach (GameOptionDropDown dd in DropDowns)
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

    protected virtual void OnGameProcessExited()
    {
        // One-shot: unsubscribe so only the lobby that started the game handles this
        GameProcessService.GameProcessExited -= OnGameProcessExited;

        // File I/O and Discord RPC on current (threadpool) thread - not on UI thread
        Log.Information("GameProcessExited: Parsing statistics.");
        matchStatistics?.ParseStatistics(ProgramConstants.GamePath, ClientConfiguration.Instance.LocalGame, false);

        Log.Information("GameProcessExited: Adding match to statistics.");
        StatisticsManager.Instance.AddMatchAndSaveDatabase(true, matchStatistics);

        UpdateDiscordPresence(true);

        // Only marshal UI state changes
        UIThreadMarshaller.AddCallback(() =>
        {
            UI_ClearReadyStatuses();
            UI_CopyPlayerDataToUI();
        });
    }

    // --- Discord ---

    protected abstract void UpdateDiscordPresence(bool resetTimer = false);

    protected void ResetDiscordPresence() => DiscordHandler.UpdatePresence();

    // --- Abstract / virtual methods for subclasses ---

    protected abstract bool AllowPlayerOptionsChange();

    protected virtual bool UpdateLaunchGameButtonStatus() => true;

    protected abstract void AddNotice(string message);

    protected void UI_AddNotice(string message)
    {
        AddNotice(message);
    }

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
    public abstract bool IsHost { get; set; }

    // --- Helper ---
    protected string AILevelToName(int aiLevel) => ProgramConstants.GetAILevelName(aiLevel);
}


