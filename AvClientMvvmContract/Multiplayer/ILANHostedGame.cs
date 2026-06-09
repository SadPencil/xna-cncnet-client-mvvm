namespace AvClientMvvmContract.Multiplayer;

/// <summary>
/// Read-only view of a hosted LAN game exposed to the View.
/// </summary>
public interface ILANHostedGame
{
    string RoomName { get; }
    string HostName { get; }
    string GameMode { get; }
    string Map { get; }
    string GameVersion { get; }
    string[] Players { get; }
    int MaxPlayers { get; }
    bool Locked { get; }
    bool Incompatible { get; }
    bool IsLoadedGame { get; }
    bool Passworded { get; }
}
