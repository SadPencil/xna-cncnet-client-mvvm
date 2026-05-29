
using System;
using System.IO;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer;

using Rampastring.Tools;

namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the LAN game creation window.
/// Contains all business logic from LANGameCreationWindow.cs except XNA UI rendering.
/// </summary>
public partial class LANGameCreationWindowViewModel : ObservableObject, ILANGameCreationWindowViewModel
{
    // --- Observable state ---

    [ObservableProperty]
    private string _gameName = string.Format("{0}'s Game", ProgramConstants.PLAYERNAME);

    [ObservableProperty]
    private bool _isLoadGameAvailable;

    [ObservableProperty]
    private bool _isWindowVisible;

    // --- Events ---

    public event EventHandler? NewGameRequested;
    public event EventHandler<GameLoadEventArgs>? LoadGameRequested;
    public event EventHandler? Cancelled;

    // --- Constructor ---

    public LANGameCreationWindowViewModel()
    {
    }

    // --- Commands ---

    [RelayCommand]
    private void CreateNewGame()
    {
        IsWindowVisible = false;
        NewGameRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void LoadGame()
    {
        IsWindowVisible = false;

        IniFile iniFile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));

        LoadGameRequested?.Invoke(this, new GameLoadEventArgs(iniFile.GetIntValue("Settings", "GameID", -1)));
    }

    [RelayCommand]
    private void Cancel()
    {
        IsWindowVisible = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    // --- Public methods ---

    public void Open()
    {
        IsLoadGameAvailable = AllowLoadingGame();
        IsWindowVisible = true;
    }

    // --- Helpers ---

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

        // Don't allow loading CnCNet games in LAN mode
        if (iniFile.SectionExists("Tunnel"))
            return false;

        return true;
    }
}
