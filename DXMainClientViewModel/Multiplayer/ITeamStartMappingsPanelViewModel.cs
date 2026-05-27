#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ITeamStartMappingsPanelViewModel
{
    IReadOnlyList<string> MappingSummaries { get; }
    bool IsEnabled { get; set; }

    IRelayCommand ApplyMappingsCommand { get; }
    IRelayCommand ResetMappingsCommand { get; }
}
