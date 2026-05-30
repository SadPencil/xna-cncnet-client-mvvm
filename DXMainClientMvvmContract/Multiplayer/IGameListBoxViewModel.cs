using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer;

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
