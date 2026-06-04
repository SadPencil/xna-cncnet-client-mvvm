using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Observable state for a single player slot row in the game lobby.
/// All list properties use ObservableCollection so Avalonia bindings
/// can track INotifyCollectionChanged alongside PropertyChanged.
/// All scalar properties have setters so TwoWay SelectedIndex bindings
/// can write user input back to the ViewModel.
/// </summary>
public interface IPlayerSlotObservable : INotifyPropertyChanged
{
    // -- Name dropdown --
    string PlayerName { get; set; }
    ObservableCollection<string> NameOptions { get; set; }
    int SelectedNameIndex { get; set; }
    bool IsNameDropdownEnabled { get; set; }

    // -- Side dropdown --
    ObservableCollection<string> SideOptions { get; set; }
    int SelectedSideIndex { get; set; }
    bool IsSideDropdownEnabled { get; set; }
    ObservableCollection<bool> SideSelectable { get; set; }

    // -- Color dropdown --
    ObservableCollection<string> ColorOptions { get; set; }
    int SelectedColorIndex { get; set; }
    bool IsColorDropdownEnabled { get; set; }
    ObservableCollection<bool> ColorSelectable { get; set; }

    // -- Start dropdown --
    ObservableCollection<string> StartOptions { get; set; }
    int SelectedStartIndex { get; set; }
    bool IsStartDropdownEnabled { get; set; }
    ObservableCollection<bool> StartSelectable { get; set; }

    // -- Team dropdown --
    ObservableCollection<string> TeamOptions { get; set; }
    int SelectedTeamIndex { get; set; }
    bool IsTeamDropdownEnabled { get; set; }
}
