using AvClientMvvmContract.Multiplayer;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using AvClientViewModel.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer.CnCNet;

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
    private string _selectedGameName = string.Empty;

    [ObservableProperty]
    private string _hostName = string.Empty;

    [ObservableProperty]
    private string _mapName = string.Empty;

    [ObservableProperty]
    private string _gameModeName = string.Empty;

    [ObservableProperty]
    private int _playerCount;

    [ObservableProperty]
    private int _maxPlayers;

    [ObservableProperty]
    private int _ping;

    [ObservableProperty]
    private string _gameVersion = string.Empty;

    [ObservableProperty]
    private int _skillLevelIndex = -1;

    [ObservableProperty]
    private string _skillLevelName = string.Empty;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private bool _isPasswordProtected;

    [ObservableProperty]
    private bool _isCompatible;

    [ObservableProperty]
    private bool _hasGameInfo;

    [ObservableProperty]
    private string? _mapHash;

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

