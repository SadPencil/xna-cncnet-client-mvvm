#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IRecentPlayerTableViewModel
{
    IReadOnlyList<string> RecentPlayerNames { get; }
    int SelectedPlayerIndex { get; set; }
    string? SelectedPlayerName { get; }

    IRelayCommand OpenSelectedPlayerCommand { get; }
    IRelayCommand RefreshRecentPlayersCommand { get; }
    IRelayCommand ClearRecentPlayersCommand { get; }
}
