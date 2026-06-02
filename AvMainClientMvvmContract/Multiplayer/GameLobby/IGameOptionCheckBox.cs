using System.ComponentModel;

namespace AvMainClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Read-only view of a game option checkbox.
/// </summary>
public interface IGameOptionCheckBox : INotifyPropertyChanged
{
    string Name { get; }
    bool IsChecked { get; }
    bool HostChecked { get; }
    bool IsEnabled { get; }
}
