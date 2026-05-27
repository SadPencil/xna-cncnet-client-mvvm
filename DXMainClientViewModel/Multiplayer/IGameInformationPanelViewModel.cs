using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer;

public interface IGameInformationPanelViewModel : INotifyPropertyChanged
{
    string SelectedGameName { get; }
    string HostName { get; }
    string MapName { get; }
    string GameModeName { get; }
    string PlayerCountText { get; }
    string PingText { get; }
    bool IsLocked { get; }
    bool IsPasswordProtected { get; }
    bool IsCompatible { get; }
    IReadOnlyList<string> PlayerNames { get; }

    IRelayCommand RefreshCommand { get; }
    IRelayCommand ClearSelectionCommand { get; }
}
