
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// ViewModel for the game lobby settings window.
/// Contains all business logic from GameLobbySettingsWindow.cs except XNA UI rendering.
/// </summary>
public partial class GameLobbySettingsWindowViewModel : ObservableObject, IGameLobbySettingsWindowViewModel
{
    // --- Observable state ---

    [ObservableProperty]
    private string _gameName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private int _maxPlayers = 8;

    [ObservableProperty]
    private int _selectedSkillLevelIndex;

    [ObservableProperty]
    private bool _isWindowVisible;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _maxPlayerOptions = new();
    public IReadOnlyList<string> MaxPlayerOptions => _maxPlayerOptions;

    private readonly ObservableCollection<string> _skillLevelNames = new();
    public IReadOnlyList<string> SkillLevelNames => _skillLevelNames;

    // --- Events ---

    public event EventHandler<GameLobbySettingsEventArgs>? SettingsChanged;
    public event EventHandler? Cancelled;
    public event EventHandler<string>? ValidationError;

    // --- Constructor ---

    public GameLobbySettingsWindowViewModel()
    {
        // Initialize max player options (8 down to 2)
        for (int i = 8; i > 1; i--)
            _maxPlayerOptions.Add(i.ToString());

        // Initialize skill level options
        string[] skillLevelOptions = ClientConfiguration.Instance.GetSkillLevelOptions();
        for (int i = 0; i < skillLevelOptions.Length; i++)
        {
            string localizedSkillLevel = skillLevelOptions[i].L10N($"INI:ClientDefinitions:SkillLevel:{i}");
            _skillLevelNames.Add(localizedSkillLevel);
        }

        SelectedSkillLevelIndex = ClientConfiguration.Instance.DefaultSkillLevelIndex;
    }

    // --- Commands ---

    [RelayCommand]
    private void SaveSettings()
    {
        string sanitizedName = NameValidator.GetSanitizedGameName(GameName);

        NameValidationError validationError = NameValidator.IsGameNameValid(sanitizedName, out string errorMessage);
        if (validationError != NameValidationError.None)
        {
            ValidationError?.Invoke(this, errorMessage);
            return;
        }

        int maxPlayers = MaxPlayers;

        SettingsChanged?.Invoke(this, new GameLobbySettingsEventArgs(
            sanitizedName, maxPlayers, SelectedSkillLevelIndex, Password));

        IsWindowVisible = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsWindowVisible = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    // --- Public methods ---

    /// <summary>
    /// Opens the settings window with the current game settings.
    /// </summary>
    public void Open(string currentGameName, int currentMaxPlayers, int currentSkillLevel, string? currentPassword)
    {
        GameName = currentGameName;
        Password = currentPassword ?? string.Empty;
        MaxPlayers = currentMaxPlayers;
        SelectedSkillLevelIndex = currentSkillLevel;
        IsWindowVisible = true;
    }
}
