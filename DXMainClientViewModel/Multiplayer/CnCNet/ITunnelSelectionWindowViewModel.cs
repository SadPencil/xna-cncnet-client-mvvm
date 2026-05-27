#nullable enable
using System.ComponentModel;
using System.Collections.Generic;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface ITunnelSelectionWindowViewModel : INotifyPropertyChanged
{
    string DescriptionText { get; }
    IReadOnlyList<string> TunnelNames { get; }
    int SelectedTunnelIndex { get; set; }
    string? SelectedTunnelName { get; }

    IRelayCommand ConfirmSelectionCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand RefreshTunnelsCommand { get; }
}
