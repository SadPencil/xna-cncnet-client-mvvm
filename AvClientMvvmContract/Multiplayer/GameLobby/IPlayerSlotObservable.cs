using System.Collections.Generic;
using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Observable state for a single player slot row in the game lobby.
///
/// All properties are both gettable and settable so Avalonia compiled bindings
/// can establish TwoWay binding where needed (SelectedIndex on ComboBox) and
/// reliably track PropertyChanged for OneWay bindings (ItemsSource, IsEnabled).
/// Get-only interface properties silently fail TwoWay writes, preventing the
/// View from propagating user input back to the ViewModel.
/// </summary>
public interface IPlayerSlotObservable : INotifyPropertyChanged
{
    // -- Name dropdown --
    string PlayerName { get; set; }
    IReadOnlyList<string> NameOptions { get; set; }
    int SelectedNameIndex { get; set; }
    bool IsNameDropdownEnabled { get; set; }

    // -- Side dropdown --
    IReadOnlyList<string> SideOptions { get; set; }
    int SelectedSideIndex { get; set; }
    bool IsSideDropdownEnabled { get; set; }
    IReadOnlyList<bool> SideSelectable { get; set; }

    // -- Color dropdown --
    IReadOnlyList<string> ColorOptions { get; set; }
    int SelectedColorIndex { get; set; }
    bool IsColorDropdownEnabled { get; set; }
    IReadOnlyList<bool> ColorSelectable { get; set; }

    // -- Start dropdown --
    IReadOnlyList<string> StartOptions { get; set; }
    int SelectedStartIndex { get; set; }
    bool IsStartDropdownEnabled { get; set; }
    IReadOnlyList<bool> StartSelectable { get; set; }

    // -- Team dropdown --
    IReadOnlyList<string> TeamOptions { get; set; }
    int SelectedTeamIndex { get; set; }
    bool IsTeamDropdownEnabled { get; set; }
}
