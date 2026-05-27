#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IGameFiltersPanelViewModel
{
    bool ShowFriendsOnlyGames { get; set; }
    bool HideLockedGames { get; set; }
    bool HidePasswordProtectedGames { get; set; }
    bool HideIncompatibleGames { get; set; }
    int MaxPlayerCount { get; set; }

    IRelayCommand ApplyFiltersCommand { get; }
    IRelayCommand ResetFiltersCommand { get; }
    IRelayCommand CloseCommand { get; }
}
