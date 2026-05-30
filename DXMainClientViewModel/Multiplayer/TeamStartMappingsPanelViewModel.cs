using DXMainClientMVVMContract.Multiplayer;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer;

namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the team start mappings panel.
/// Contains all business logic from TeamStartMappingsPanel.cs except XNA UI rendering.
/// </summary>
public partial class TeamStartMappingsPanelViewModel : ObservableObject, ITeamStartMappingsPanelViewModel
{
    private const int MaxStartCount = 8;

    // --- State ---

    private List<TeamStartMapping> currentMappings = new();
    private List<int> allowedStartingLocations = new();

    // --- Observable state ---

    [ObservableProperty]
    private bool _isEnabled;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _mappingSummaries = new();
    public IReadOnlyList<string> MappingSummaries => _mappingSummaries;

    // --- Callbacks ---

    private readonly Action? onMappingsChanged;

    // --- Constructor ---

    public TeamStartMappingsPanelViewModel(Action? onMappingsChanged = null)
    {
        this.onMappingsChanged = onMappingsChanged;
    }

    // --- Commands ---

    [RelayCommand]
    private void ApplyMappings()
    {
        onMappingsChanged?.Invoke();
    }

    [RelayCommand]
    private void ResetMappings()
    {
        currentMappings.Clear();
        RefreshSummaries();
        onMappingsChanged?.Invoke();
    }

    // --- Public methods ---

    public void SetAllowedStartingLocations(List<int> locations)
    {
        allowedStartingLocations = locations ?? new List<int>();
        RefreshSummaries();
    }

    public void SetTeamStartMappings(List<TeamStartMapping> mappings)
    {
        currentMappings = mappings != null ? new List<TeamStartMapping>(mappings) : new List<TeamStartMapping>();
        RefreshSummaries();
    }

    public List<TeamStartMapping> GetTeamStartMappings()
    {
        return currentMappings.Where(m => m.IsValid).ToList();
    }

    public void SetTeamStartMapping(int startLocation, TeamStartMapping mapping)
    {
        // Ensure list is large enough
        while (currentMappings.Count < startLocation)
            currentMappings.Add(new TeamStartMapping { Team = TeamStartMapping.NO_TEAM, Start = currentMappings.Count + 1 });

        currentMappings[startLocation - 1] = mapping;
        RefreshSummaries();
        onMappingsChanged?.Invoke();
    }

    public void ClearMappings()
    {
        currentMappings.Clear();
        RefreshSummaries();
    }

    // --- Helpers ---

    private void RefreshSummaries()
    {
        _mappingSummaries.Clear();

        for (int i = 0; i < MaxStartCount; i++)
        {
            int startLocation = i + 1;
            bool isAllowed = allowedStartingLocations.Contains(startLocation);

            if (!isAllowed)
            {
                _mappingSummaries.Add($"#{startLocation}: N/A");
                continue;
            }

            var mapping = i < currentMappings.Count ? currentMappings[i] : null;
            if (mapping == null || !mapping.IsValid)
            {
                _mappingSummaries.Add($"#{startLocation}: -");
            }
            else
            {
                string teamDisplay = mapping.Team == TeamStartMapping.NO_PLAYER ? "Blocked" :
                    mapping.Team == TeamStartMapping.NO_TEAM ? "No Team" : mapping.Team;
                _mappingSummaries.Add($"#{startLocation}: {teamDisplay}");
            }
        }
    }
}


