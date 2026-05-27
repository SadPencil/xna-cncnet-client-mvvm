#nullable enable
using System.ComponentModel;
using System.Collections.Generic;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer;

public interface ITeamStartMappingsPanelViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> MappingSummaries { get; }
    bool IsEnabled { get; set; }

    IRelayCommand ApplyMappingsCommand { get; }
    IRelayCommand ResetMappingsCommand { get; }
}
