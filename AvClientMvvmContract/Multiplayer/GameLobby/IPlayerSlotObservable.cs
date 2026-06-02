using System.Collections.Generic;
using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Read-only view of observable state for a single player slot.
/// </summary>
public interface IPlayerSlotObservable : INotifyPropertyChanged
{
    string PlayerName { get; }
    IReadOnlyList<string> NameOptions { get; }
    int SelectedNameIndex { get; }
    bool IsNameDropdownEnabled { get; }

    IReadOnlyList<string> SideOptions { get; }
    int SelectedSideIndex { get; }
    bool IsSideDropdownEnabled { get; }
    IReadOnlyList<bool> SideSelectable { get; }

    IReadOnlyList<string> ColorOptions { get; }
    int SelectedColorIndex { get; }
    bool IsColorDropdownEnabled { get; }
    IReadOnlyList<bool> ColorSelectable { get; }

    IReadOnlyList<string> StartOptions { get; }
    int SelectedStartIndex { get; }
    bool IsStartDropdownEnabled { get; }
    IReadOnlyList<bool> StartSelectable { get; }

    IReadOnlyList<string> TeamOptions { get; }
    int SelectedTeamIndex { get; }
    bool IsTeamDropdownEnabled { get; }
}
