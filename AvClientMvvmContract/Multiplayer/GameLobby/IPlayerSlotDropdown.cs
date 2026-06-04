using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// One dropdown group for a player slot. T is the option type
/// (IPlayerName, IPlayerSide, IPlayerColor, IPlayerStart, or IPlayerTeam).
///
/// Avalonia ComboBox bindings:
///   ItemsSource  = Options         (OneWay)
///   SelectedItem = SelectedOption  (TwoWay)
///   IsEnabled    = IsEnabled       (OneWay)
///   Selectable   = per-item bool[] (OneWay)
/// </summary>
public interface IPlayerSlotDropdown<T> : INotifyPropertyChanged
{
    ObservableCollection<T> Options { get; set; }
    ObservableCollection<bool> Selectable { get; set; }
    T? SelectedOption { get; set; }
    bool IsEnabled { get; set; }
}
