using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer;

public interface IGameFiltersPanelViewModel : INotifyPropertyChanged
{
    bool ShowFriendsOnlyGames { get; set; }
    bool HideLockedGames { get; set; }
    bool HidePasswordProtectedGames { get; set; }
    bool HideIncompatibleGames { get; set; }
    int MaxPlayerCount { get; set; }
    bool IsPanelVisible { get; set; }

    IRelayCommand ApplyFiltersCommand { get; }
    IRelayCommand ResetFiltersCommand { get; }
    IRelayCommand CloseCommand { get; }
}
