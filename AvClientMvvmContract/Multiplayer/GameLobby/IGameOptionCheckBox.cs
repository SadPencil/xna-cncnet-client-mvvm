using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Observable view of a game option checkbox for View binding.
/// </summary>
public interface IGameOptionCheckBox : INotifyPropertyChanged
{
    /// <summary>Internal identifier (e.g., "chkBases").</summary>
    string Name { get; }

    /// <summary>Human-readable display text (e.g., "Bases").</summary>
    string DisplayName { get; }

    /// <summary>Checked state (TwoWay binding writes back to ViewModel).</summary>
    bool IsChecked { get; set; }

    bool HostChecked { get; }
    bool IsEnabled { get; }
}
