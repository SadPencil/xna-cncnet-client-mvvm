#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ITunnelSelectionWindowViewModel
{
    string DescriptionText { get; }
    IReadOnlyList<string> TunnelNames { get; }
    int SelectedTunnelIndex { get; set; }
    string? SelectedTunnelName { get; }

    IRelayCommand ConfirmSelectionCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand RefreshTunnelsCommand { get; }
}
