#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IPlayerListBoxViewModel
{
    IReadOnlyList<string> PlayerNames { get; }
    int SelectedPlayerIndex { get; set; }
    string? SelectedPlayerName { get; }

    IRelayCommand OpenSelectedPlayerCommand { get; }
    IRelayCommand RefreshPlayersCommand { get; }
}
