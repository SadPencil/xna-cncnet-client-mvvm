using System;
using System.Collections.Generic;
using System.Linq;

using AvClientMvvmContract.Multiplayer;

using ClientCore;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the game filters panel.
/// Contains all business logic from GameFiltersPanel.cs except XNA UI rendering.
/// </summary>
public partial class GameFiltersPanelViewModel : ObservableObject, IGameFiltersPanelViewModel
{
    private const int MinPlayerCount = 2;
    private const int MaxPlayerCountLimit = 8;

    private readonly UserINISettings iniSettings;

    // --- Game option filters ---

    private readonly List<GameOptionFilterDefinition> _filterDefinitions = new();
    private readonly List<GameOptionFilterValue> _filterValues = new();

    /// <summary>
    /// The available game option filter definitions.
    /// The View uses these to create the filter UI controls.
    /// </summary>
    public IReadOnlyList<IGameOptionFilterDefinition> FilterDefinitions => _filterDefinitions;

    /// <summary>
    /// The current filter values. The View binds dropdown selected indices to these.
    /// </summary>
    public IReadOnlyList<IGameOptionFilterValue> FilterValues => _filterValues;

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
    private bool _isVisible;

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
        IsVisible = false;
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
        IsVisible = false;
    }

    // --- Public methods (called by parent ViewModel) ---

    public void Show()
    {
        Load();
        IsVisible = true;
    }

    /// <summary>
    /// Registers game option filter definitions from the game lobby.
    /// Called by the parent ViewModel when the game lobby is initialized.
    /// </summary>
    public void RegisterGameOptionFilters(IEnumerable<GameOptionFilterDefinition> definitions)
    {
        _filterDefinitions.Clear();
        _filterDefinitions.AddRange(definitions);
        _filterValues.Clear();

        foreach (var def in _filterDefinitions)
        {
            int? storedValue = iniSettings.GetGameOptionFilterValue(def.OptionName);
            int selectedIndex;

            if (def.IsCheckbox)
            {
                // Storage: null = All, 1 = On, 0 = Off
                // UI: 0 = All, 1 = On, 2 = Off
                selectedIndex = storedValue switch
                {
                    null => 0,
                    1 => 1,
                    0 => 2,
                    _ => 0
                };
            }
            else
            {
                // Storage: null = All, otherwise actual index
                // UI: 0 = All, 1+ = game option indices
                selectedIndex = storedValue == null ? 0 : storedValue.Value + 1;
            }

            _filterValues.Add(new GameOptionFilterValue
            {
                Definition = def,
                SelectedIndex = selectedIndex
            });
        }
    }

    // --- Helpers ---

    private void Load()
    {
        ShowFriendsOnlyGames = iniSettings.ShowFriendGamesOnly.Value;
        HideLockedGames = iniSettings.HideLockedGames.Value;
        HidePasswordProtectedGames = iniSettings.HidePasswordedGames.Value;
        HideIncompatibleGames = iniSettings.HideIncompatibleGames.Value;
        MaxPlayerCount = iniSettings.MaxPlayerCount.Value;

        // Reload game option filter values
        foreach (var filterValue in _filterValues)
        {
            int? storedValue = iniSettings.GetGameOptionFilterValue(filterValue.Definition.OptionName);

            if (filterValue.Definition.IsCheckbox)
            {
                filterValue.SelectedIndex = storedValue switch
                {
                    null => 0,
                    1 => 1,
                    0 => 2,
                    _ => 0
                };
            }
            else
            {
                filterValue.SelectedIndex = storedValue == null ? 0 : storedValue.Value + 1;
            }
        }
    }

    private void Save()
    {
        iniSettings.ShowFriendGamesOnly.Value = ShowFriendsOnlyGames;
        iniSettings.HideLockedGames.Value = HideLockedGames;
        iniSettings.HidePasswordedGames.Value = HidePasswordProtectedGames;
        iniSettings.HideIncompatibleGames.Value = HideIncompatibleGames;
        iniSettings.MaxPlayerCount.Value = MaxPlayerCount;

        // Save game option filter values
        foreach (var filterValue in _filterValues)
        {
            if (filterValue.Definition.IsCheckbox)
            {
                // UI: 0 = All, 1 = On, 2 = Off
                // Storage: null = All, 1 = On, 0 = Off
                int? filterVal = filterValue.SelectedIndex switch
                {
                    0 => null,
                    1 => 1,
                    2 => 0,
                    _ => null
                };
                if (filterVal != null)
                    iniSettings.SetGameOptionFilterValue(filterValue.Definition.OptionName, filterVal);
            }
            else
            {
                // UI: 0 = All, 1+ = game option indices
                // Storage: null = All, otherwise actual index
                int? filterVal = filterValue.SelectedIndex == 0 ? null : filterValue.SelectedIndex - 1;
                if (filterVal != null)
                    iniSettings.SetGameOptionFilterValue(filterValue.Definition.OptionName, filterVal);
            }
        }

        iniSettings.SaveSettings();
    }
}

