#nullable enable
using System.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IPlayerExtraOptionsPanelViewModel : INotifyPropertyChanged
{
    bool ForceRandomSides { get; set; }
    bool ForceRandomColors { get; set; }
    bool ForceRandomStarts { get; set; }
    bool ForceNoTeams { get; set; }
    bool UseTeamStartMappings { get; set; }
    IReadOnlyList<string> TeamStartMappingPresetNames { get; }
    int SelectedTeamStartMappingPresetIndex { get; set; }
    IReadOnlyList<string> TeamStartMappingSummaries { get; }

    IRelayCommand ShowHelpCommand { get; }
    IRelayCommand ResetMappingsCommand { get; }
    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
}
