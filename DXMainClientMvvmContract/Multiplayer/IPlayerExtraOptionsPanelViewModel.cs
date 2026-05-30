using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer;

public interface IPlayerExtraOptionsPanelViewModel : INotifyPropertyChanged
{
    bool ForceRandomSides { get; set; }
    bool ForceRandomColors { get; set; }
    bool ForceRandomStarts { get; set; }
    bool ForceNoTeams { get; set; }
    bool ForceNoTeamsAllowChecking { get; set; }
    bool UseTeamStartMappings { get; set; }
    bool UseTeamStartMappingsAllowChecking { get; set; }
    IReadOnlyList<string> TeamStartMappingPresetNames { get; }
    int SelectedTeamStartMappingPresetIndex { get; set; }
    IReadOnlyList<string> TeamStartMappingSummaries { get; }
    bool IsPanelVisible { get; set; }
    bool IsHostControlsEnabled { get; }

    IRelayCommand ResetMappingsCommand { get; }
    IRelayCommand ClosePanelCommand { get; }
}
