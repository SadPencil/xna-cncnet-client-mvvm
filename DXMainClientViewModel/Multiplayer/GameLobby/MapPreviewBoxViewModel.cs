using DXMainClientMvvmContract.Multiplayer.GameLobby;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace DXMainClientViewModel.Multiplayer.GameLobby;


/// <summary>
/// ViewModel for the map preview box.
/// Contains non-rendering business logic from MapPreviewBox.cs.
/// Rendering, texture loading, and mouse interaction are View concerns.
/// </summary>
public partial class MapPreviewBoxViewModel : ObservableObject, IMapPreviewBoxViewModel
{
    private readonly MapLoader mapLoader;
    private GameModeMap? gameModeMap;
    private List<PlayerInfo>? players;
    private List<PlayerInfo>? aiPlayers;

    // --- Observable state ---

    [ObservableProperty]
    private string _selectedMapName = string.Empty;

    [ObservableProperty]
    private string _selectedGameModeName = string.Empty;

    [ObservableProperty]
    private string _mapAuthorName = string.Empty;

    [ObservableProperty]
    private string _mapSizeText = string.Empty;

    [ObservableProperty]
    private int _selectedStartingLocationIndex;

    [ObservableProperty]
    private int _selectedPlayerIndex;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _showExtraTextures;

    [ObservableProperty]
    private bool _enableContextMenu;

    [ObservableProperty]
    private bool _enableStartLocationSelection = true;

    [ObservableProperty]
    private byte[]? _mapPreviewImageBytes;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _startingLocationSummaries = new();
    public IReadOnlyList<string> StartingLocationSummaries => _startingLocationSummaries;

    private readonly Action? onFavoriteToggled;
    private readonly Action? onStartingLocationApplied;
    private readonly Action<int>? onLocalStartingLocationSelected;

    // --- Constructor ---

    public MapPreviewBoxViewModel(MapLoader mapLoader, Action? onFavoriteToggled = null, Action? onStartingLocationApplied = null, Action<int>? onLocalStartingLocationSelected = null)
    {
        this.mapLoader = mapLoader;
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
        if (gameModeMap == null)
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
            if (gameModeMap == null)
            {
                MapPreviewImageBytes = null;
                return;
            }

            using var lease = mapLoader.GetCachedPreviewImageFromMap(gameModeMap.Map, syncLoadOnCacheMiss: true);
            if (lease?.Value == null)
            {
                MapPreviewImageBytes = null;
                return;
            }

            using var ms = new MemoryStream();
            lease.Value.Save(ms, new PngEncoder());
            MapPreviewImageBytes = ms.ToArray();
        }
        catch
        {
            MapPreviewImageBytes = null;
        }
    }
}

