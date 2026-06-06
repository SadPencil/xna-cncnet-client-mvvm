using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using AvClientMvvmContract.Multiplayer;

using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.CnCNet;

using ClientCore;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the game information panel.
/// Contains all business logic from GameInformationPanel.cs except XNA UI rendering.
/// </summary>
public partial class GameInformationPanelViewModel : ObservableObject, IGameInformationPanelViewModel
{
    private readonly MapLoader mapLoader;
    private GenericHostedGame? currentGame;
    private readonly string[] skillLevelOptions;

    // --- Observable state ---

    [ObservableProperty]
    public partial string SelectedGameName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HostName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MapName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GameModeName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int PlayerCount { get; set; }

    [ObservableProperty]
    public partial int MaxPlayers { get; set; }

    [ObservableProperty]
    public partial int Ping { get; set; }

    [ObservableProperty]
    public partial string GameVersion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SkillLevelIndex { get; set; } = -1;

    [ObservableProperty]
    public partial string SkillLevelName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLocked { get; set; }

    [ObservableProperty]
    public partial bool IsPasswordProtected { get; set; }

    [ObservableProperty]
    public partial bool IsCompatible { get; set; }

    [ObservableProperty]
    public partial bool HasGameInfo { get; set; }

    [ObservableProperty]
    public partial string? MapHash { get; set; }

    // --- Observable collections ---

    private readonly ObservableCollection<string> _playerNames = new();
    public IReadOnlyList<string> PlayerNames => _playerNames;

    // --- Constructor ---

    public GameInformationPanelViewModel(MapLoader mapLoader)
    {
        this.mapLoader = mapLoader;
        this.skillLevelOptions = ClientConfiguration.Instance.GetSkillLevelOptions();
    }

    // --- Commands ---

    [RelayCommand]
    private void Refresh()
    {
        if (currentGame != null)
            SetInfo(currentGame);
    }

    [RelayCommand]
    private void ClearSelection()
    {
        ClearInfo();
    }

    // --- Public methods (called by parent ViewModel, not View) ---

    public void SetInfo(GenericHostedGame game)
    {
        ClearInfo();

        currentGame = game;
        HasGameInfo = true;

        SelectedGameName = game.RoomName ?? string.Empty;
        HostName = game.HostName ?? string.Empty;

        // Map name resolution
        string resolvedMapName = "Unknown".L10N("Client:Main:Unknown");

        if (!string.IsNullOrEmpty(game.MapHash) && mapLoader != null)
        {
            Map map = mapLoader.FindMapByHash(game.MapHash);

            if (map != null)
                resolvedMapName = map.Name ?? map.UntranslatedName;
            else if (!string.IsNullOrEmpty(game.Map))
                resolvedMapName = game.Map; // fallback to broadcasted name
        }
        else if (!string.IsNullOrEmpty(game.Map))
        {
            resolvedMapName = game.Map;
        }

        MapName = resolvedMapName;

        // Game mode name
        GameModeName = string.IsNullOrEmpty(game.GameMode)
            ? "Unknown".L10N("Client:Main:Unknown")
            : game.GameMode.L10N($"INI:GameModes:{game.GameMode}:UIName", notify: false);

        // Ping
        Ping = game.Ping;

        // Player count
        PlayerCount = game.Players.Length;
        MaxPlayers = game.MaxPlayers;

        // Player names
        _playerNames.Clear();
        for (int i = 0; i < game.Players.Length; i++)
            _playerNames.Add(game.Players[i]);

        // Skill level
        SkillLevelIndex = game.SkillLevel;
        if (game.SkillLevel >= 0 && game.SkillLevel < skillLevelOptions.Length)
        {
            string skillLevel = skillLevelOptions[game.SkillLevel];
            SkillLevelName = skillLevel.L10N($"INI:ClientDefinitions:SkillLevel:{game.SkillLevel}");
        }
        else
        {
            SkillLevelName = string.Empty;
        }

        // Game version
        GameVersion = game.GameVersion ?? string.Empty;

        // Status flags
        IsLocked = game.Locked;
        IsPasswordProtected = game.Passworded;
        IsCompatible = !game.Incompatible;

        // Map hash for preview - View observes this property to load texture
        MapHash = game.MapHash;
    }

    public void ClearInfo()
    {
        currentGame = null;
        HasGameInfo = false;
        SelectedGameName = string.Empty;
        HostName = string.Empty;
        MapName = string.Empty;
        GameModeName = string.Empty;
        PlayerCount = 0;
        MaxPlayers = 0;
        Ping = 0;
        GameVersion = string.Empty;
        SkillLevelIndex = -1;
        SkillLevelName = string.Empty;
        IsLocked = false;
        IsPasswordProtected = false;
        IsCompatible = true;
        MapHash = null;
        _playerNames.Clear();
    }

    public GenericHostedGame? GetCurrentGame() => currentGame;
}

