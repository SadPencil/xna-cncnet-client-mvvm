#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IGameListBoxViewModel
{
    IReadOnlyList<string> GameNames { get; }
    int SelectedGameIndex { get; set; }
    string? SelectedGameName { get; }
    IReadOnlyList<string> SortOptions { get; }
    int SelectedSortOptionIndex { get; set; }

    IRelayCommand JoinSelectedGameCommand { get; }
    IRelayCommand RefreshGamesCommand { get; }
}
