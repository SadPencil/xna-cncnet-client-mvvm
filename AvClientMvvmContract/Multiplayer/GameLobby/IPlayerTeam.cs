using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// An option in the team dropdown.
/// </summary>
public interface IPlayerTeam : INotifyPropertyChanged
{
    int Index { get; set; }
    string Name { get; set; }
}
