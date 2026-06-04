using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// An option in the start location dropdown.
/// </summary>
public interface IPlayerStart : INotifyPropertyChanged
{
    int Index { get; set; }
    string Name { get; set; }
}
