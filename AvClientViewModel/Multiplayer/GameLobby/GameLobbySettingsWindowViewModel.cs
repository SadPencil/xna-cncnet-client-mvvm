using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientViewModel.Domain.Multiplayer.CnCNet;

using ClientCore;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// ViewModel for the game lobby settings window.
/// Contains all business logic from GameLobbySettingsWindow.cs except XNA UI rendering.
/// </summary>
public partial class GameLobbySettingsWindowViewModel : ObservableObject, IGameLobbySettingsWindowViewModel
{
    // --- Observable state ---

    [ObservableProperty]
    public partial string GameName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int MaxPlayers { get; set; } = 8;

    [ObservableProperty]
    public partial int SelectedSkillLevelIndex { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    // --- Observable collections ---

    private readonly ObservableCollection<string> _maxPlayerOptions = new();
    public IReadOnlyList<string> MaxPlayerOptions => _maxPlayerOptions;

    private readonly ObservableCollection<string> _skillLevelNames = new();
    public IReadOnlyList<string> SkillLevelNames => _skillLevelNames;

    private readonly Action<GameLobbySettingsEventArgs>? onSettingsChanged;
    private readonly Action? onCancelled;
    private readonly Action<string>? onValidationError;

    // --- Constructor ---

    public GameLobbySettingsWindowViewModel(Action<GameLobbySettingsEventArgs>? onSettingsChanged = null, Action? onCancelled = null, Action<string>? onValidationError = null)
    {
        this.onSettingsChanged = onSettingsChanged;
        this.onCancelled = onCancelled;
        this.onValidationError = onValidationError;

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
            onValidationError?.Invoke(errorMessage);
            return;
        }

        int maxPlayers = MaxPlayers;

        onSettingsChanged?.Invoke(new GameLobbySettingsEventArgs(
            sanitizedName, maxPlayers, SelectedSkillLevelIndex, Password));

        IsVisible = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsVisible = false;
        onCancelled?.Invoke();
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
        IsVisible = true;
    }
}

