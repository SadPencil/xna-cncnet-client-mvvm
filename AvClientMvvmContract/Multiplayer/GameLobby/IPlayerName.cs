using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// An option in the player name dropdown (human player, AI, or empty slot).
/// </summary>
public interface IPlayerName : INotifyPropertyChanged
{
    int Index { get; set; }
    string Name { get; set; }
    byte[]? Icon { get; set; }
    int Latency { get; set; }
}
