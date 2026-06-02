using System;

using AvMainClientViewModel.Domain.Multiplayer.CnCNet;

namespace AvMainClientViewModel.Multiplayer.CnCNet;

public class GameCreationEventArgs : EventArgs
{
    public GameCreationEventArgs(string roomName, int maxPlayers,
        string password, CnCNetTunnel tunnel, int skillLevel)
    {
        GameRoomName = roomName;
        MaxPlayers = maxPlayers;
        Password = password;
        Tunnel = tunnel;
        SkillLevel = skillLevel;
    }

    public string GameRoomName { get; }
    public int MaxPlayers { get; }
    public string Password { get; }
    public CnCNetTunnel Tunnel { get; }
    public int SkillLevel { get; }
}
