using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Multiplayer.CnCNet;

public interface ITunnelListBoxViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> TunnelNames { get; }
    IReadOnlyList<string> TunnelAddresses { get; }
    int SelectedTunnelIndex { get; set; }
    string? SelectedTunnelName { get; }

    IRelayCommand RefreshTunnelsCommand { get; }
    IRelayCommand SelectTunnelCommand { get; }
}
