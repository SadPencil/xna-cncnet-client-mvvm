using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using AvClientMvvmContract;
using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Online;

using ClientCore;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace AvClientViewModel.Multiplayer.GameLobby;


/// <summary>
/// ViewModel for the map preview box.
/// Contains non-rendering business logic from MapPreviewBox.cs.
/// Rendering, texture loading, and mouse interaction are View concerns.
/// </summary>
public partial class MapPreviewBoxViewModel : ObservableObject, IMapPreviewBoxViewModel
{
    private readonly MapLoader mapLoader;
    private readonly IUIThreadMarshaller uiThreadMarshaller;
    private GameModeMap? gameModeMap;
    private List<PlayerInfo>? players;
    private List<PlayerInfo>? aiPlayers;
    private List<MultiplayerColor>? mpColors;

    // --- Observable state ---

    [ObservableProperty]
    public partial string SelectedMapName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedGameModeName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MapAuthorName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MapSizeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedStartingLocationIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedPlayerIndex { get; set; }

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    [ObservableProperty]
    public partial bool ShowExtraTextures { get; set; }

    [ObservableProperty]
    public partial bool EnableContextMenu { get; set; }

    [ObservableProperty]
    public partial bool EnableStartLocationSelection { get; set; } = true;

    [ObservableProperty]
    public partial byte[]? MapPreviewImageBytes { get; set; }

    // --- Observable collections ---

    private readonly ObservableCollection<string> _startingLocationSummaries = new();
    public IReadOnlyList<string> StartingLocationSummaries => _startingLocationSummaries;

    private readonly ObservableCollection<StartingLocationIndicatorData> _startingLocationIndicators = new();
    public IReadOnlyList<IStartingLocationIndicatorData> StartingLocationIndicators => _startingLocationIndicators;

    private readonly Action? onFavoriteToggled;
    private Action? onStartingLocationApplied;
    private Action<int>? onLocalStartingLocationSelected;

    // --- Constructor ---

    public MapPreviewBoxViewModel(MapLoader mapLoader, IUIThreadMarshaller uiThreadMarshaller, Action? onFavoriteToggled = null, Action? onStartingLocationApplied = null, Action<int>? onLocalStartingLocationSelected = null)
    {
        this.mapLoader = mapLoader;
        this.uiThreadMarshaller = uiThreadMarshaller;
        this.onFavoriteToggled = onFavoriteToggled;
        this.onStartingLocationApplied = onStartingLocationApplied;
        this.onLocalStartingLocationSelected = onLocalStartingLocationSelected;

        ShowExtraTextures = UserINISettings.Instance.DisplayToggleableExtraTextures;
    }

    // --- Commands ---

    [RelayCommand]
    private void SelectStartingLocation()
    {
        if (!EnableStartLocationSelection || gameModeMap == null)
            return;

        if (!EnableContextMenu)
        {
            // In non-context-menu mode (e.g., skirmish), directly select the location
            if (gameModeMap.EnforceMaxPlayers && players != null && aiPlayers != null)
            {
                foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
                {
                    if (pInfo.StartingLocation == SelectedStartingLocationIndex)
                        return; // Location already taken
                }
            }

            onLocalStartingLocationSelected?.Invoke(SelectedStartingLocationIndex);
            return;
        }

        // In context-menu mode, the View opens the context menu
        // After the user selects a player from the context menu, AssignStartingLocation is called
    }

    [RelayCommand]
    private void AssignStartingLocation()
    {
        if (gameModeMap == null || players == null || aiPlayers == null)
            return;

        int locationIndex = SelectedStartingLocationIndex;
        int playerIndex = SelectedPlayerIndex;

        if (gameModeMap.EnforceMaxPlayers)
        {
            foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
            {
                if (pInfo.StartingLocation == locationIndex)
                    pInfo.StartingLocation = 0;
            }
        }

        PlayerInfo player;
        if (playerIndex >= players.Count)
        {
            int aiIndex = playerIndex - players.Count;
            if (aiIndex >= aiPlayers.Count)
                return;
            player = aiPlayers[aiIndex];
        }
        else
        {
            player = players[playerIndex];
        }

        player.StartingLocation = locationIndex;
        onStartingLocationApplied?.Invoke();
    }

    [RelayCommand]
    private void ClearStartingLocation()
    {
        if (players == null || aiPlayers == null)
            return;

        int locationIndex = SelectedStartingLocationIndex;

        if (!EnableContextMenu)
        {
            // In non-context-menu mode, only clear the local player's location
            PlayerInfo? pInfo = players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
            if (pInfo != null && pInfo.StartingLocation == locationIndex)
            {
                onLocalStartingLocationSelected?.Invoke(0);
            }
            return;
        }

        // In context-menu mode, clear all players at this location
        foreach (PlayerInfo pInfo in players.Union(aiPlayers))
        {
            if (pInfo.StartingLocation == locationIndex)
                pInfo.StartingLocation = 0;
        }

        onStartingLocationApplied?.Invoke();
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        onFavoriteToggled?.Invoke();
    }

    [RelayCommand]
    private void ToggleExtraTextures()
    {
        ShowExtraTextures = !ShowExtraTextures;
        UserINISettings.Instance.DisplayToggleableExtraTextures.Value = ShowExtraTextures;
    }

    [RelayCommand]
    private void ShowInFolder()
    {
        gameModeMap?.Map.OpenContainingFolder();
    }

    [RelayCommand]
    private void RefreshPreview()
    {
        UpdateMapInfo();
    }

    // --- Public methods ---

    /// <summary>
    /// Sets the callback invoked when a starting location is applied.
    /// </summary>
    public void SetOnStartingLocationApplied(Action? callback)
    {
        onStartingLocationApplied = callback;
    }

    /// <summary>
    /// Sets the callback invoked when the local player selects a starting location on the map preview.
    /// </summary>
    public void SetOnLocalStartingLocationSelected(Action<int>? callback)
    {
        onLocalStartingLocationSelected = callback;
    }

    /// <summary>
    /// Sets the current game mode map and updates all display properties.
    /// </summary>
    public void SetGameModeMap(GameModeMap? gameModeMap)
    {
        this.gameModeMap = gameModeMap;
        UpdateMapInfo();
    }

    /// <summary>
    /// Sets the player lists for starting location management.
    /// </summary>
    public void SetPlayers(List<PlayerInfo> players, List<PlayerInfo> aiPlayers)
    {
        this.players = players;
        this.aiPlayers = aiPlayers;
    }

    /// <summary>
    /// Sets the multiplayer color definitions for indicator tinting.
    /// </summary>
    public void SetMPColors(List<MultiplayerColor> colors)
    {
        mpColors = colors;
    }

    /// <summary>
    /// Updates starting location summaries based on player info.
    /// </summary>
    public void UpdateStartingLocationSummaries(List<PlayerInfo> players, List<PlayerInfo> aiPlayers)
    {
        _startingLocationSummaries.Clear();

        var allPlayers = players.Concat(aiPlayers).ToList();
        foreach (var player in allPlayers)
        {
            string locationText = player.StartingLocation > 0
                ? $"Location {player.StartingLocation}: {player.Name}"
                : $"{player.Name}: Random";
            _startingLocationSummaries.Add(locationText);
        }
    }

    // --- Helpers ---

    private void UpdateMapInfo()
    {
        if (gameModeMap == null || gameModeMap.Map == null)
        {
            SelectedMapName = string.Empty;
            SelectedGameModeName = string.Empty;
            MapAuthorName = string.Empty;
            MapSizeText = string.Empty;
            IsFavorite = false;
            MapPreviewImageBytes = null;
            return;
        }

        SelectedMapName = gameModeMap.Map.UntranslatedName;
        SelectedGameModeName = gameModeMap.GameMode.Name;
        MapAuthorName = gameModeMap.Map.Author ?? string.Empty;

        MapSizeText = gameModeMap.Map.GetSizeString();

        IsFavorite = UserINISettings.Instance.IsFavoriteMap(
            gameModeMap.Map.SHA1,
            gameModeMap.Map.UntranslatedName,
            gameModeMap.GameMode.Name);

        UpdateMapPreviewImage();
    }

    private void UpdateMapPreviewImage()
    {
        try
        {
            if (gameModeMap == null || gameModeMap.Map == null)
            {
                uiThreadMarshaller.AddCallback(() => MapPreviewImageBytes = null);
                return;
            }

            using var lease = mapLoader.GetCachedPreviewImageFromMap(gameModeMap.Map, syncLoadOnCacheMiss: true);
            if (lease?.Value == null)
            {
                uiThreadMarshaller.AddCallback(() => MapPreviewImageBytes = null);
                return;
            }

            using var ms = new MemoryStream();
            lease.Value.Save(ms, new PngEncoder());
            byte[] bytes = ms.ToArray();
            uiThreadMarshaller.AddCallback(() => MapPreviewImageBytes = bytes);
        }
        catch
        {
            uiThreadMarshaller.AddCallback(() => MapPreviewImageBytes = null);
        }
    }

    private const int MAX_STARTING_LOCATIONS = 8;
    private const int PREVIEW_WIDTH = 400;
    private const int PREVIEW_HEIGHT = 300;

    /// <summary>
    /// Updates starting location indicators based on current map and player data.
    /// Matches the old MapPreviewBox.UpdateMap() + UpdateStartingLocationTexts() logic.
    /// </summary>
    public void UpdateStartingLocationIndicators()
    {
        _startingLocationIndicators.Clear();

        if (gameModeMap == null || gameModeMap.Map == null || MapPreviewImageBytes == null)
            return;

        // Get preview image dimensions from the loaded image
        int previewW, previewH;
        try
        {
            using var ms = new MemoryStream(MapPreviewImageBytes);
            using var img = SixLabors.ImageSharp.Image.Load(ms);
            previewW = img.Width;
            previewH = img.Height;
        }
        catch
        {
            return;
        }

        // Compute scale ratio and texture position (matching old MapPreviewBox.UpdateMap)
        double xRatio = (PREVIEW_WIDTH - 2) / (double)previewW;
        double yRatio = (PREVIEW_HEIGHT - 2) / (double)previewH;

        double ratio;
        int texturePositionX = 1;
        int texturePositionY = 1;

        if (xRatio > yRatio)
        {
            ratio = yRatio;
            texturePositionY = 1;
            int textureWidth = (int)(previewW * ratio);
            texturePositionX = (PREVIEW_WIDTH - 2 - textureWidth) / 2;
        }
        else
        {
            ratio = xRatio;
            texturePositionX = 1;
            int textureHeight = (int)(previewH * ratio);
            texturePositionY = (PREVIEW_HEIGHT - 2 - textureHeight) / 2 + 1;
        }

        // Get starting location coordinates in preview space
        var previewSize = new MapPreviewPoint(previewW, previewH);
        List<MapPreviewPoint> startingLocations = gameModeMap.Map.GetStartingLocationPreviewCoords(previewSize);

        // Build assigned-players lookup
        var assignedPlayers = new Dictionary<int, List<IIndicatorPlayerInfo>>();
        var allPlayers = (players ?? new List<PlayerInfo>()).Concat(aiPlayers ?? new List<PlayerInfo>());
        foreach (var pInfo in allPlayers)
        {
            if (pInfo.StartingLocation <= 0 || pInfo.StartingLocation > MAX_STARTING_LOCATIONS)
                continue;

            if (!assignedPlayers.ContainsKey(pInfo.StartingLocation))
                assignedPlayers[pInfo.StartingLocation] = new List<IIndicatorPlayerInfo>();

            // ColorId is 1-based (0 = no color, 1 = mpColors[0])
            var mc = mpColors != null && pInfo.ColorId > 0 && pInfo.ColorId <= mpColors.Count
                ? mpColors[pInfo.ColorId - 1]
                : null;
            var color = mc?.Color ?? new Rgb24Color(255, 255, 255);

            assignedPlayers[pInfo.StartingLocation].Add(new IndicatorPlayerInfo(
                pInfo.Name, pInfo.TeamId, color));
        }

        // Create indicator data for each starting location
        for (int i = 0; i < MAX_STARTING_LOCATIONS; i++)
        {
            int waypoint = i + 1;
            bool showLocation = i < startingLocations.Count
                && gameModeMap.AllowedStartingLocations.Contains(waypoint);

            if (!showLocation)
                continue;

            double x = texturePositionX + startingLocations[i].X * ratio;
            double y = texturePositionY + startingLocations[i].Y * ratio;

            assignedPlayers.TryGetValue(waypoint, out var playerList);
            bool isOccupied = playerList != null && playerList.Count > 0;

            // Tint color: use first player's color if occupied, white if empty
            IRgb24Color tintColor = isOccupied
                ? playerList![0].Color
                : new Rgb24Color(255, 255, 255);

            _startingLocationIndicators.Add(new StartingLocationIndicatorData(
                waypoint, x, y, true, isOccupied, tintColor,
                playerList ?? new List<IIndicatorPlayerInfo>()));
        }

        uiThreadMarshaller.AddCallback(() => OnPropertyChanged(nameof(StartingLocationIndicators)));
    }
}

internal record StartingLocationIndicatorData(
    int WaypointNumber,
    double X,
    double Y,
    bool IsVisible,
    bool IsOccupied,
    IRgb24Color TintColor,
    IReadOnlyList<IIndicatorPlayerInfo> Players) : IStartingLocationIndicatorData;

internal record IndicatorPlayerInfo(
    string Name,
    int TeamId,
    IRgb24Color Color) : IIndicatorPlayerInfo;

