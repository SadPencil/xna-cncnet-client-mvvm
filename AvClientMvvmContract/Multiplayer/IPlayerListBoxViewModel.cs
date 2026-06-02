using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer;

public interface IPlayerListBoxViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> PlayerNames { get; }
    int SelectedPlayerIndex { get; set; }
    string? SelectedPlayerName { get; }

    IRelayCommand OpenSelectedPlayerCommand { get; }
    IRelayCommand RefreshPlayersCommand { get; }
}
