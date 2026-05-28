using System;
using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface ITunnelSelectionWindowViewModel : INotifyPropertyChanged
{
    string DescriptionText { get; }
    IReadOnlyList<string> TunnelNames { get; }
    int SelectedTunnelIndex { get; set; }
    string? SelectedTunnelName { get; }
    bool IsConfirmEnabled { get; }
    bool IsWindowVisible { get; set; }

    IRelayCommand ConfirmSelectionCommand { get; }
    IRelayCommand CancelCommand { get; }

    event EventHandler<TunnelEventArgs>? TunnelSelected;
}
