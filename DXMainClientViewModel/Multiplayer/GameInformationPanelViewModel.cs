
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the game information panel.
/// Contains all business logic from GameInformationPanel.cs except XNA UI rendering.
/// </summary>
public partial class GameInformationPanelViewModel : ObservableObject, IGameInformationPanelViewModel
{
    private readonly MapLoader mapLoader;
    private GenericHostedGame? currentGame;
    private string[] skillLevelOptions;

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
    private string _playerCountText = string.Empty;

    [ObservableProperty]
    private string _pingText = string.Empty;

    [ObservableProperty]
    private string _gameVersion = string.Empty;

    [ObservableProperty]
    private string _skillLevelText = string.Empty;

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

    // --- Events ---

    public event EventHandler? MapPreviewRequested;

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

    // --- Public methods ---

    public void SetInfo(GenericHostedGame game)
    {
        ClearInfo();

        currentGame = game;
        HasGameInfo = true;

        SelectedGameName = game.RoomName ?? string.Empty;
        HostName = game.HostName ?? string.Empty;

        // Map name resolution
        string translatedMapName = "Unknown".L10N("Client:Main:Unknown");

        if (!string.IsNullOrEmpty(game.MapHash) && mapLoader != null)
        {
            Map map = mapLoader.FindMapByHash(game.MapHash);

            if (map != null)
                translatedMapName = map.Name ?? map.UntranslatedName;
            else if (!string.IsNullOrEmpty(game.Map))
                translatedMapName = game.Map; // fallback to broadcasted name
        }
        else if (!string.IsNullOrEmpty(game.Map))
        {
            translatedMapName = game.Map;
        }

        MapName = translatedMapName;

        // Game mode name
        GameModeName = string.IsNullOrEmpty(game.GameMode)
            ? "Unknown".L10N("Client:Main:Unknown")
            : game.GameMode.L10N($"INI:GameModes:{game.GameMode}:UIName", notify: false);

        // Ping
        PingText = game.Ping > 0
            ? "Ping:".L10N("Client:Main:GameInfoPing") + " " + game.Ping.ToString() + " ms"
            : "Ping: Unknown".L10N("Client:Main:GameInfoPingUnknown");

        // Player count
        PlayerCountText = "Players".L10N("Client:Main:GameInfoPlayers") + " (" + game.Players.Length + " / " + game.MaxPlayers + "):";

        // Player names
        _playerNames.Clear();
        for (int i = 0; i < game.Players.Length; i++)
            _playerNames.Add(game.Players[i]);

        // Skill level
        int skillLevelIndex = game.SkillLevel;
        if (skillLevelIndex >= 0 && skillLevelIndex < skillLevelOptions.Length)
        {
            string skillLevel = skillLevelOptions[skillLevelIndex];
            string localizedSkillLevel = skillLevel.L10N($"INI:ClientDefinitions:SkillLevel:{skillLevelIndex}");
            SkillLevelText = "Preferred Skill Level:".L10N("Client:Main:GameInfoSkillLevel") + " " + localizedSkillLevel;
        }
        else
        {
            SkillLevelText = string.Empty;
        }

        // Game version
        GameVersion = game.GameVersion ?? string.Empty;

        // Status flags
        IsLocked = game.Locked;
        IsPasswordProtected = game.Passworded;
        IsCompatible = !game.Incompatible;

        // Map hash for preview
        MapHash = game.MapHash;

        // Notify that map preview should be updated
        MapPreviewRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ClearInfo()
    {
        currentGame = null;
        HasGameInfo = false;
        SelectedGameName = string.Empty;
        HostName = string.Empty;
        MapName = string.Empty;
        GameModeName = string.Empty;
        PlayerCountText = string.Empty;
        PingText = string.Empty;
        GameVersion = string.Empty;
        SkillLevelText = string.Empty;
        IsLocked = false;
        IsPasswordProtected = false;
        IsCompatible = true;
        MapHash = null;
        _playerNames.Clear();
    }

    public GenericHostedGame? GetCurrentGame() => currentGame;
}
// checked
