#nullable enable
using System.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IRecentPlayerTableViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> RecentPlayerNames { get; }
    int SelectedPlayerIndex { get; set; }
    string? SelectedPlayerName { get; }

    IRelayCommand OpenSelectedPlayerCommand { get; }
    IRelayCommand RefreshRecentPlayersCommand { get; }
    IRelayCommand ClearRecentPlayersCommand { get; }
}
