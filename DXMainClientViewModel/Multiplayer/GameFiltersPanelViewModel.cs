using System;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the game filters panel.
/// Contains all business logic from GameFiltersPanel.cs except XNA UI rendering.
/// </summary>
public partial class GameFiltersPanelViewModel : ObservableObject, IGameFiltersPanelViewModel // checked
{
    private const int MinPlayerCount = 2;
    private const int MaxPlayerCountLimit = 8;

    private readonly UserINISettings iniSettings;

    // --- Observable state ---

    [ObservableProperty]
    private bool _showFriendsOnlyGames;

    [ObservableProperty]
    private bool _hideLockedGames;

    [ObservableProperty]
    private bool _hidePasswordProtectedGames;

    [ObservableProperty]
    private bool _hideIncompatibleGames;

    [ObservableProperty]
    private int _maxPlayerCount = MaxPlayerCountLimit;

    [ObservableProperty]
    private bool _isPanelVisible;

    // --- Events ---

    public event EventHandler? FiltersApplied;
    public event EventHandler? Closed;

    // --- Constructor ---

    public GameFiltersPanelViewModel(UserINISettings iniSettings)
    {
        this.iniSettings = iniSettings;
    }

    // --- Commands ---

    [RelayCommand]
    private void ApplyFilters()
    {
        Save();
        IsPanelVisible = false;
        FiltersApplied?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ResetFilters()
    {
        iniSettings.ResetGameFilters();
        Load();
    }

    [RelayCommand]
    private void Close()
    {
        IsPanelVisible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    // --- Public methods ---

    public void Show()
    {
        Load();
        IsPanelVisible = true;
    }

    // --- Helpers ---

    private void Load()
    {
        ShowFriendsOnlyGames = iniSettings.ShowFriendGamesOnly.Value;
        HideLockedGames = iniSettings.HideLockedGames.Value;
        HidePasswordProtectedGames = iniSettings.HidePasswordedGames.Value;
        HideIncompatibleGames = iniSettings.HideIncompatibleGames.Value;
        MaxPlayerCount = iniSettings.MaxPlayerCount.Value;
    }

    private void Save()
    {
        iniSettings.ShowFriendGamesOnly.Value = ShowFriendsOnlyGames;
        iniSettings.HideLockedGames.Value = HideLockedGames;
        iniSettings.HidePasswordedGames.Value = HidePasswordProtectedGames;
        iniSettings.HideIncompatibleGames.Value = HideIncompatibleGames;
        iniSettings.MaxPlayerCount.Value = MaxPlayerCount;

        iniSettings.SaveSettings();
    }
}
