using System.Collections.Generic;
using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Observable view of a game option dropdown for View binding.
/// </summary>
public interface IGameOptionDropDown : INotifyPropertyChanged
{
    /// <summary>Internal identifier (e.g., "cmbCredits").</summary>
    string Name { get; }

    /// <summary>Human-readable display text (e.g., "Starting Credits").</summary>
    string DisplayName { get; }

    /// <summary>Selected index (TwoWay binding writes back to ViewModel).</summary>
    int SelectedIndex { get; set; }

    int HostSelectedIndex { get; }
    bool IsEnabled { get; }
    IReadOnlyList<string> Items { get; }
}
