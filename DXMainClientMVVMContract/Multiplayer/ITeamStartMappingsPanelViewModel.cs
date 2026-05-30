using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMVVMContract.Multiplayer;

public interface ITeamStartMappingsPanelViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> MappingSummaries { get; }
    bool IsEnabled { get; set; }

    IRelayCommand ApplyMappingsCommand { get; }
    IRelayCommand ResetMappingsCommand { get; }
}
