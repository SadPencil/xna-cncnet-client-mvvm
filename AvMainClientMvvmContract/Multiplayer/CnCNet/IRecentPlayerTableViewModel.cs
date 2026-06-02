using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Multiplayer.CnCNet;

public interface IRecentPlayerTableViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> RecentPlayerNames { get; }
    int SelectedPlayerIndex { get; set; }
    string? SelectedPlayerName { get; }

    IRelayCommand OpenSelectedPlayerCommand { get; }
    IRelayCommand RefreshRecentPlayersCommand { get; }
    IRelayCommand ClearRecentPlayersCommand { get; }
}
