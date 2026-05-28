using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// ViewModel for the map preview box.
/// Contains non-rendering business logic from MapPreviewBox.cs.
/// Rendering, texture loading, and mouse interaction are View concerns.
/// </summary>
public partial class MapPreviewBoxViewModel : ObservableObject, IMapPreviewBoxViewModel
{
    private GameModeMap? gameModeMap;

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
    private bool _isFavorite;

    [ObservableProperty]
    private bool _showExtraTextures;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _startingLocationSummaries = new();
    public IReadOnlyList<string> StartingLocationSummaries => _startingLocationSummaries;

    // --- Events ---

    public event EventHandler? FavoriteToggled;
    public event EventHandler? StartingLocationApplied;

    // --- Constructor ---

    public MapPreviewBoxViewModel()
    {
        ShowExtraTextures = UserINISettings.Instance.DisplayToggleableExtraTextures;
    }

    // --- Commands ---

    [RelayCommand]
    private void SelectStartingLocation()
    {
        // The View handles the actual click on the indicator.
        // This command is invoked when the user selects a starting location.
        StartingLocationApplied?.Invoke(this, EventArgs.Empty);
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
    /// Toggles the favorite status of the current map.
    /// </summary>
    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        FavoriteToggled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Toggles the display of extra textures.
    /// </summary>
    public void ToggleExtraTextures()
    {
        ShowExtraTextures = !ShowExtraTextures;
        UserINISettings.Instance.DisplayToggleableExtraTextures.Value = ShowExtraTextures;
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

    /// <summary>
    /// Assigns a starting location to a player.
    /// </summary>
    public void AssignStartingLocation(int playerIndex, int locationIndex, List<PlayerInfo> players, List<PlayerInfo> aiPlayers, bool enforceMaxPlayers)
    {
        if (enforceMaxPlayers)
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
        StartingLocationApplied?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears a starting location assignment.
    /// </summary>
    public void ClearStartingLocation(int locationIndex, List<PlayerInfo> players, List<PlayerInfo> aiPlayers)
    {
        foreach (PlayerInfo pInfo in players.Union(aiPlayers))
        {
            if (pInfo.StartingLocation == locationIndex)
                pInfo.StartingLocation = 0;
        }

        StartingLocationApplied?.Invoke(this, EventArgs.Empty);
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
    }
}
