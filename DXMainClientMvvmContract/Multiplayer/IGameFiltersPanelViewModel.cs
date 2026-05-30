using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMVVMContract.Multiplayer;

public interface IGameFiltersPanelViewModel : INotifyPropertyChanged
{
    bool ShowFriendsOnlyGames { get; set; }
    bool HideLockedGames { get; set; }
    bool HidePasswordProtectedGames { get; set; }
    bool HideIncompatibleGames { get; set; }
    int MaxPlayerCount { get; set; }
    bool IsPanelVisible { get; set; }

    IReadOnlyList<IGameOptionFilterDefinition> FilterDefinitions { get; }
    IReadOnlyList<IGameOptionFilterValue> FilterValues { get; }

    IRelayCommand ApplyFiltersCommand { get; }
    IRelayCommand ResetFiltersCommand { get; }
    IRelayCommand CloseCommand { get; }
}
