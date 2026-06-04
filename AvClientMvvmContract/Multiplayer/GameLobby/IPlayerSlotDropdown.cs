using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// One dropdown group for a player slot (name, side, color, start, or team).
/// Avalonia ComboBox bindings:
///   ItemsSource  = Options     (OneWay, View reads the list)
///   SelectedItem = SelectedOption (TwoWay, View reads &amp; writes user selection)
///   IsEnabled    = IsEnabled   (OneWay, View reads)
///   Selectable   = per-item bool flags (OneWay, View reads)
/// </summary>
public interface IPlayerSlotDropdown : INotifyPropertyChanged
{
    ObservableCollection<string> Options { get; set; }
    ObservableCollection<bool> Selectable { get; set; }
    string SelectedOption { get; set; }
    bool IsEnabled { get; set; }
}
