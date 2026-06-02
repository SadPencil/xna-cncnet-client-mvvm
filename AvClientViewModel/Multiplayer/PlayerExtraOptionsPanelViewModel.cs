using AvClientMvvmContract.Multiplayer;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using AvClientViewModel.Domain.Multiplayer;

namespace AvClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the player extra options panel.
/// Contains all business logic from PlayerExtraOptionsPanel.cs except XNA UI rendering.
/// </summary>
public partial class PlayerExtraOptionsPanelViewModel : ObservableObject, IPlayerExtraOptionsPanelViewModel
{
    private const int MaxStartCount = 8;

    private readonly string customPresetName = "Custom".L10N("Client:Main:CustomPresetName");

    // --- State ---

    private bool _isHost;
    private bool ignoreMappingChanges;
    private GameModeMap? _gameModeMap;

    // Current team start mappings (data, not UI)
    private List<TeamStartMapping> currentTeamStartMappings = new();

    // --- Observable state ---

    [ObservableProperty]
    private bool _forceRandomSides;

    [ObservableProperty]
    private bool _forceRandomColors;

    [ObservableProperty]
    private bool _forceRandomStarts;

    [ObservableProperty]
    private bool _forceNoTeams;

    [ObservableProperty]
    private bool _forceNoTeamsAllowChecking;

    [ObservableProperty]
    private bool _useTeamStartMappings;

    [ObservableProperty]
    private bool _useTeamStartMappingsAllowChecking;

    [ObservableProperty]
    private int _selectedTeamStartMappingPresetIndex;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private bool _isHostControlsEnabled;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _teamStartMappingPresetNames = new();
    public IReadOnlyList<string> TeamStartMappingPresetNames => _teamStartMappingPresetNames;

    private readonly ObservableCollection<string> _teamStartMappingSummaries = new();
    public IReadOnlyList<string> TeamStartMappingSummaries => _teamStartMappingSummaries;

    // Preset data for applying mappings
    private readonly List<List<TeamStartMapping>> presetMappings = new();

    // --- Callbacks ---

    private readonly Action? onOptionsChanged;

    // --- Constructor ---

    public PlayerExtraOptionsPanelViewModel(Action? onOptionsChanged = null)
    {
        this.onOptionsChanged = onOptionsChanged;

        IsVisible = false;
        IsHostControlsEnabled = false;
        ForceNoTeamsAllowChecking = true;
        UseTeamStartMappingsAllowChecking = true;
    }

    // --- Commands ---

    [RelayCommand]
    private void ResetMappings()
    {
        ignoreMappingChanges = true;
        ClearTeamStartMappings();
        ignoreMappingChanges = false;

        SelectedTeamStartMappingPresetIndex = 0;
        RaiseOptionsChanged();
    }

    [RelayCommand]
    private void ClosePanel()
    {
        IsVisible = false;
    }

    // --- Property change handlers ---

    partial void OnUseTeamStartMappingsChanged(bool value)
    {
        RefreshTeamStartMappingsPanelState();
        ForceNoTeams = ForceNoTeams || value;
        RefreshForceNoTeamsAllowChecking();
        RefreshPresetDropdownState();
        RaiseOptionsChanged();
    }

    partial void OnForceNoTeamsAllowCheckingChanged(bool value)
    {
        RefreshForceNoTeamsAllowChecking();
    }

    partial void OnSelectedTeamStartMappingPresetIndexChanged(int value)
    {
        if (ignoreMappingChanges)
            return;

        if (value < 0 || value >= presetMappings.Count)
            return;

        // First item is "Custom" - don't apply
        if (value == 0)
            return;

        ignoreMappingChanges = true;
        currentTeamStartMappings = new List<TeamStartMapping>(presetMappings[value]);
        RefreshTeamStartMappingSummaries();
        ignoreMappingChanges = false;

        RaiseOptionsChanged();
    }

    partial void OnForceRandomSidesChanged(bool value) => RaiseOptionsChanged();
    partial void OnForceRandomColorsChanged(bool value) => RaiseOptionsChanged();
    partial void OnForceRandomStartsChanged(bool value) => RaiseOptionsChanged();
    partial void OnForceNoTeamsChanged(bool value) => RaiseOptionsChanged();

    // --- Public methods ---

    public void UpdateForGameModeMap(GameModeMap gameModeMap)
    {
        if (_gameModeMap == gameModeMap)
            return;

        _gameModeMap = gameModeMap;
        RefreshTeamStartMappingPanelsState();
    }

    public List<TeamStartMapping> GetTeamStartMappings()
        => UseTeamStartMappings ? currentTeamStartMappings : new List<TeamStartMapping>();

    public void SetPlayerExtraOptions(PlayerExtraOptions playerExtraOptions)
    {
        ForceRandomSides = playerExtraOptions.IsForceRandomSides;
        ForceRandomColors = playerExtraOptions.IsForceRandomColors;
        ForceNoTeams = playerExtraOptions.IsForceNoTeams;
        ForceRandomStarts = playerExtraOptions.IsForceRandomStarts;
        UseTeamStartMappings = playerExtraOptions.IsUseTeamStartMappings;

        ignoreMappingChanges = true;
        currentTeamStartMappings = playerExtraOptions.TeamStartMappings != null
            ? new List<TeamStartMapping>(playerExtraOptions.TeamStartMappings)
            : new List<TeamStartMapping>();
        RefreshTeamStartMappingSummaries();
        ignoreMappingChanges = false;
    }

    public PlayerExtraOptions GetPlayerExtraOptions()
        => new PlayerExtraOptions()
        {
            IsForceRandomSides = ForceRandomSides,
            IsForceRandomColors = ForceRandomColors,
            IsForceRandomStarts = ForceRandomStarts,
            IsForceNoTeams = ForceNoTeams,
            IsUseTeamStartMappings = UseTeamStartMappings,
            TeamStartMappings = GetTeamStartMappings()
        };

    public void SetIsHost(bool isHost)
    {
        _isHost = isHost;
        RefreshPresetDropdownState();
        IsHostControlsEnabled = _isHost;
        RefreshTeamStartMappingsPanelState();
        RefreshTeamStartMappingPanelsState();
    }

    public void SetTeamStartMapping(int startLocation, TeamStartMapping mapping)
    {
        // Ensure list is large enough
        while (currentTeamStartMappings.Count < startLocation)
            currentTeamStartMappings.Add(new TeamStartMapping { Team = TeamStartMapping.NO_TEAM, Start = currentTeamStartMappings.Count + 1 });

        currentTeamStartMappings[startLocation - 1] = mapping;
        RefreshTeamStartMappingSummaries();

        if (!ignoreMappingChanges)
        {
            SelectedTeamStartMappingPresetIndex = 0;
            RaiseOptionsChanged();
        }
    }

    public void OpenPanel()
    {
        IsVisible = true;
    }

    // --- Helpers ---

    private void RaiseOptionsChanged()
    {
        onOptionsChanged?.Invoke();
    }

    private void RefreshForceNoTeamsAllowChecking()
    {
        // ForceNoTeams can't be unchecked when UseTeamStartMappings is checked
        // This is a View concern (checkbox allow checking), but we track the state
    }

    private void RefreshTeamStartMappingsPanelState()
    {
        // View will observe UseTeamStartMappings and IsHostControlsEnabled to enable/disable panel
    }

    private void RefreshTeamStartMappingPanelsState()
    {
        // Refresh preset dropdown items based on current GameModeMap
        RefreshPresetItems();
        RefreshTeamStartMappingSummaries();
    }

    private void RefreshPresetDropdownState()
    {
        // View will observe IsHostControlsEnabled and UseTeamStartMappings to enable/disable dropdown
    }

    private void RefreshPresetItems()
    {
        _teamStartMappingPresetNames.Clear();
        presetMappings.Clear();

        // Add "Custom" as first item
        _teamStartMappingPresetNames.Add(customPresetName);
        presetMappings.Add(new List<TeamStartMapping>());

        var presets = _gameModeMap?.Map?.TeamStartMappingPresets;
        if (presets != null)
        {
            foreach (var preset in presets)
            {
                _teamStartMappingPresetNames.Add(preset.Name);
                presetMappings.Add(preset.TeamStartMappings);
            }
        }

        // Default to first preset if available (index 1, since 0 is "Custom")
        if (_teamStartMappingPresetNames.Count > 1)
            SelectedTeamStartMappingPresetIndex = 1;
        else
            SelectedTeamStartMappingPresetIndex = 0;
    }

    private void ClearTeamStartMappings()
    {
        currentTeamStartMappings.Clear();
        RefreshTeamStartMappingSummaries();
    }

    private void RefreshTeamStartMappingSummaries()
    {
        _teamStartMappingSummaries.Clear();

        if (_gameModeMap == null || !UseTeamStartMappings)
            return;

        var allowedLocations = _gameModeMap.AllowedStartingLocations;
        for (int i = 0; i < MaxStartCount; i++)
        {
            int startLocation = i + 1;
            bool isAllowed = allowedLocations.Contains(startLocation);

            if (!isAllowed)
            {
                _teamStartMappingSummaries.Add($"#{startLocation}: N/A");
                continue;
            }

            var mapping = i < currentTeamStartMappings.Count ? currentTeamStartMappings[i] : null;
            if (mapping == null || !mapping.IsValid)
            {
                _teamStartMappingSummaries.Add($"#{startLocation}: -");
            }
            else
            {
                string teamDisplay = mapping.Team == TeamStartMapping.NO_PLAYER ? "Blocked" :
                    mapping.Team == TeamStartMapping.NO_TEAM ? "No Team" : mapping.Team;
                _teamStartMappingSummaries.Add($"#{startLocation}: {teamDisplay}");
            }
        }
    }
}


