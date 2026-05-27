#nullable enable
using System.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IPlayerListBoxViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> PlayerNames { get; }
    int SelectedPlayerIndex { get; set; }
    string? SelectedPlayerName { get; }

    IRelayCommand OpenSelectedPlayerCommand { get; }
    IRelayCommand RefreshPlayersCommand { get; }
}
