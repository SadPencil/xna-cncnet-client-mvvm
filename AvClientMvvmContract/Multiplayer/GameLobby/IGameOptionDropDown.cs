using System.Collections.Generic;
using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Read-only view of a game option dropdown.
/// </summary>
public interface IGameOptionDropDown : INotifyPropertyChanged
{
    string Name { get; }
    int SelectedIndex { get; }
    int HostSelectedIndex { get; }
    bool IsEnabled { get; }
    IReadOnlyList<string> Items { get; }
}
