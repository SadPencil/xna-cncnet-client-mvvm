using System;
using System.Collections.Generic;
using System.Net;

using AvClientViewModel.Domain.Multiplayer.LAN;

namespace AvClientViewModel.LAN;

/// <summary>
/// Service interface for LAN lobby player management.
/// Tracks connected players and notifies the View of changes.
/// </summary>
public interface ILANPlayerManagerService
{
    int Count { get; }

    event EventHandler<PlayerAddedEventArgs> PlayerAdded;
    event EventHandler<PlayerRemovedEventArgs> PlayerRemoved;
    event EventHandler PlayersCleared;

    LANLobbyUser GetOrCreatePlayer(IPEndPoint endPoint, string name);
    LANLobbyUser? GetPlayerIfExist(IPEndPoint endPoint);
    bool RemovePlayer(IPEndPoint endPoint);
    List<LANLobbyUser> GetAllPlayers();
    void Clear();
}

public class PlayerAddedEventArgs : EventArgs
{
    public LANLobbyUser Player { get; }
    public bool IsNewUsername { get; }

    public PlayerAddedEventArgs(LANLobbyUser player, bool isNewUsername)
    {
        Player = player;
        IsNewUsername = isNewUsername;
    }
}

public class PlayerRemovedEventArgs : EventArgs
{
    public LANLobbyUser Player { get; }
    public bool UsernameRemoved { get; }

    public PlayerRemovedEventArgs(LANLobbyUser player, bool usernameRemoved)
    {
        Player = player;
        UsernameRemoved = usernameRemoved;
    }
}
