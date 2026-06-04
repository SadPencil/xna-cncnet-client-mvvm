using System.Collections.Generic;
using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Observable state for a single player slot. Properties with setters are
/// written by the View's TwoWay bindings when the user changes dropdown selections.
/// </summary>
public interface IPlayerSlotObservable : INotifyPropertyChanged
{
    string PlayerName { get; }
    IReadOnlyList<string> NameOptions { get; }
    int SelectedNameIndex { get; set; }
    bool IsNameDropdownEnabled { get; }

    IReadOnlyList<string> SideOptions { get; }
    int SelectedSideIndex { get; set; }
    bool IsSideDropdownEnabled { get; }
    IReadOnlyList<bool> SideSelectable { get; }

    IReadOnlyList<string> ColorOptions { get; }
    int SelectedColorIndex { get; set; }
    bool IsColorDropdownEnabled { get; }
    IReadOnlyList<bool> ColorSelectable { get; }

    IReadOnlyList<string> StartOptions { get; }
    int SelectedStartIndex { get; set; }
    bool IsStartDropdownEnabled { get; }
    IReadOnlyList<bool> StartSelectable { get; }

    IReadOnlyList<string> TeamOptions { get; }
    int SelectedTeamIndex { get; set; }
    bool IsTeamDropdownEnabled { get; }
}
