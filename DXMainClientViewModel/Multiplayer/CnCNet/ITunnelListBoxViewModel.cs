#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ITunnelListBoxViewModel
{
    IReadOnlyList<string> TunnelNames { get; }
    IReadOnlyList<string> TunnelAddresses { get; }
    int SelectedTunnelIndex { get; set; }
    string? SelectedTunnelName { get; }

    IRelayCommand RefreshTunnelsCommand { get; }
    IRelayCommand SelectTunnelCommand { get; }
}
