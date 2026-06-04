using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// An option in the color dropdown.
/// </summary>
public interface IPlayerColor : INotifyPropertyChanged
{
    int Index { get; set; }
    string Name { get; set; }
    uint Color { get; set; }
}
