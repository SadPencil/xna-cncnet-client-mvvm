using System;

namespace DXMainClientMVVMContract.Domain.Multiplayer;

/// <summary>
/// Read-only view of a hosted CnCNet game.
/// Combines properties from GenericHostedGame and HostedCnCNetGame.
/// </summary>
public interface IHostedCnCNetGame
{
    // From GenericHostedGame
    string RoomName { get; }
    bool Incompatible { get; }
    bool Locked { get; }
    bool IsLoadedGame { get; }
    bool Passworded { get; }
    string GameMode { get; }
    string Map { get; }
    string MapHash { get; }
    string GameVersion { get; }
    string HostName { get; }
    string[] Players { get; }
    int MaxPlayers { get; }
    int Ping { get; }
    DateTime LastRefreshTime { get; }
    int SkillLevel { get; }

    // From HostedCnCNetGame
    string ChannelName { get; }
    string Revision { get; }
    bool Tunneled { get; }
    bool IsLadder { get; }
    string MatchID { get; }
    ICnCNetTunnel TunnelServer { get; }
    int[] BroadcastedGameOptionValues { get; }
}
