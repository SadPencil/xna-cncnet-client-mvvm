using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

using ClientCore;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

using Rampastring.Tools;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

/// <summary>
/// ViewModel for the CnCNet game creation window.
/// Contains all business logic from GameCreationWindow.cs except XNA UI rendering.
/// </summary>
public partial class GameCreationWindowViewModel : ObservableObject, IGameCreationWindowViewModel
{
    private readonly TunnelHandler tunnelHandler;

    // --- Observable state ---

    [ObservableProperty]
    private string _gameName = string.Format("{0}'s Game", ProgramConstants.PLAYERNAME);

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private int _maxPlayers = 8;

    [ObservableProperty]
    private bool _isPrivateGame;

    [ObservableProperty]
    private bool _isLoadedGame;

    [ObservableProperty]
    private int _selectedTunnelIndex;

    [ObservableProperty]
    private int _selectedSkillLevel;

    [ObservableProperty]
    private bool _isAdvancedOptionsVisible;

    [ObservableProperty]
    private bool _canCreateGame;

    [ObservableProperty]
    private bool _canLoadGame;

    [ObservableProperty]
    private string _validationErrorMessage = string.Empty;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _tunnelNames = new();
    public IReadOnlyList<string> TunnelNames => _tunnelNames;

    private readonly ObservableCollection<string> _maxPlayersOptions = new();
    public IReadOnlyList<string> MaxPlayersOptions => _maxPlayersOptions;

    private readonly ObservableCollection<string> _skillLevelOptions = new();
    public IReadOnlyList<string> SkillLevelOptions => _skillLevelOptions;

    // --- Events ---

    public event EventHandler<GameCreationEventArgs>? GameCreated;
    public event EventHandler<GameCreationEventArgs>? LoadedGameCreated;
    public event EventHandler? Cancelled;

    // --- Constructor ---

    public GameCreationWindowViewModel(TunnelHandler tunnelHandler)
    {
        this.tunnelHandler = tunnelHandler;

        // Initialize max players options
        for (int i = 8; i > 1; i--)
            _maxPlayersOptions.Add(i.ToString());

        // Initialize skill level options
        string[] skillLevels = ClientConfiguration.Instance.GetSkillLevelOptions();
        for (int i = 0; i < skillLevels.Length; i++)
        {
            string localizedSkillLevel = skillLevels[i].L10N($"INI:ClientDefinitions:SkillLevel:{i}");
            _skillLevelOptions.Add(localizedSkillLevel);
        }

        SelectedSkillLevel = ClientConfiguration.Instance.DefaultSkillLevelIndex;

        // Initialize tunnel list
        RefreshTunnelList();

        // Check if loading game is allowed
        CanLoadGame = AllowLoadingGame();

        // Auto-show advanced options if configured
        if (UserINISettings.Instance.AlwaysDisplayTunnelList)
            IsAdvancedOptionsVisible = true;

        UserINISettings.Instance.SettingsSaved += (_, _) =>
        {
            GameName = string.Format("{0}'s Game", UserINISettings.Instance.PlayerName.Value);
        };
    }

    // --- Commands ---

    [RelayCommand]
    private async Task CreateGameAsync()
    {
        string sanitizedName = NameValidator.GetSanitizedGameName(GameName);

        NameValidationError validationError = NameValidator.IsGameNameValid(sanitizedName, out string errorMessage);
        if (validationError != NameValidationError.None)
        {
            ValidationErrorMessage = errorMessage;
            return;
        }

        if (!IsValidTunnelSelected())
            return;

        GameCreated?.Invoke(this,
            new GameCreationEventArgs(sanitizedName, MaxPlayers, Password,
                tunnelHandler.Tunnels[SelectedTunnelIndex], SelectedSkillLevel));
    }

    [RelayCommand]
    private async Task CreateLoadedGameAsync()
    {
        string sanitizedName = NameValidator.GetSanitizedGameName(GameName);

        NameValidationError validationError = NameValidator.IsGameNameValid(sanitizedName, out string errorMessage);
        if (validationError != NameValidationError.None)
        {
            ValidationErrorMessage = errorMessage;
            return;
        }

        if (!IsValidTunnelSelected())
            return;

        IniFile spawnSGIni =
            new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));

        string password = Utilities.CalculateSHA1ForString(
            spawnSGIni.GetStringValue("Settings", "GameID", string.Empty)).Substring(0, 10);

        GameCreationEventArgs ea = new GameCreationEventArgs(sanitizedName,
            spawnSGIni.GetIntValue("Settings", "PlayerCount", 2), password,
            tunnelHandler.Tunnels[SelectedTunnelIndex], SelectedSkillLevel);

        LoadedGameCreated?.Invoke(this, ea);
    }

    [RelayCommand]
    private void OpenAdvancedOptions()
    {
        IsAdvancedOptionsVisible = true;
        RefreshTunnelList();
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    // --- Helpers ---

    private void RefreshTunnelList()
    {
        _tunnelNames.Clear();
        foreach (var tunnel in tunnelHandler.Tunnels)
            _tunnelNames.Add(tunnel.Name);

        CanCreateGame = _tunnelNames.Count > 0;
    }

    private bool IsValidTunnelSelected()
    {
        return SelectedTunnelIndex >= 0 && SelectedTunnelIndex < tunnelHandler.Tunnels.Count;
    }

    private bool AllowLoadingGame()
    {
        FileInfo savedGameSpawnIniFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI);

        if (!savedGameSpawnIniFile.Exists)
            return false;

        IniFile iniFile = new IniFile(savedGameSpawnIniFile.FullName);

        if (iniFile.GetStringValue("Settings", "Name", string.Empty) != ProgramConstants.PLAYERNAME)
            return false;

        if (!iniFile.GetBooleanValue("Settings", "Host", false))
            return false;

        return true;
    }

    public void Refresh()
    {
        CanLoadGame = AllowLoadingGame();
        RefreshTunnelList();
    }
}
