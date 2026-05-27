#nullable enable
using System.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IGameListBoxViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> GameNames { get; }
    int SelectedGameIndex { get; set; }
    string? SelectedGameName { get; }
    IReadOnlyList<string> SortOptions { get; }
    int SelectedSortOptionIndex { get; set; }

    IRelayCommand JoinSelectedGameCommand { get; }
    IRelayCommand RefreshGamesCommand { get; }
}
