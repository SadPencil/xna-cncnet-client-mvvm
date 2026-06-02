using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using AvClientViewModel.Domain.Multiplayer.LAN;

namespace AvClientViewModel.LAN;

/// <summary>
/// Thread-safe manager for LAN lobby players.
/// Tracks connected players and notifies subscribers of changes via events.
/// </summary>
public class LANPlayerManagerService : ILANPlayerManagerService
{
    private readonly object lockObject = new();
    private readonly Dictionary<string, LANLobbyUser> players = new();
    private readonly HashSet<string> knownUsernames = new();

    public event EventHandler<PlayerAddedEventArgs>? PlayerAdded;
    public event EventHandler<PlayerRemovedEventArgs>? PlayerRemoved;
    public event EventHandler? PlayersCleared;

    private static string GetKeyFromEndPoint(IPEndPoint endPoint)
        => endPoint.ToString();

    /// <summary>
    /// Gets or creates a player. Returns the LANLobbyUser instance (either newly created or existing).
    /// This operation is atomic - both the internal dictionary and events are updated together.
    /// </summary>
    /// <param name="endPoint">The endpoint (IP:Port) that uniquely identifies this connection.</param>
    /// <param name="name">The player's username.</param>
    /// <returns>The LANLobbyUser instance (either newly created or existing).</returns>
    public LANLobbyUser GetOrCreatePlayer(IPEndPoint endPoint, string name)
    {
        lock (lockObject)
        {
            string key = GetKeyFromEndPoint(endPoint);

            // If this endpoint already exists, return the existing user
            if (players.TryGetValue(key, out LANLobbyUser? existingUser))
            {
                return existingUser;
            }

            // Create new user
            var newUser = new LANLobbyUser(name, null, endPoint);
            players[key] = newUser;

            // Check if this username is new
            bool isNewUsername = knownUsernames.Add(name);

            PlayerAdded?.Invoke(this, new PlayerAddedEventArgs(newUser, isNewUsername));

            return newUser;
        }
    }

    /// <summary>
    /// Attempts to get a player by endpoint.
    /// </summary>
    public LANLobbyUser? GetPlayerIfExist(IPEndPoint endPoint)
    {
        lock (lockObject)
        {
            string key = GetKeyFromEndPoint(endPoint);
            _ = players.TryGetValue(key, out LANLobbyUser? user);
            return user;
        }
    }

    /// <summary>
    /// Removes a player by endpoint. This operation is atomic.
    /// </summary>
    /// <returns>True if the player was removed, false if not found.</returns>
    public bool RemovePlayer(IPEndPoint endPoint)
    {
        lock (lockObject)
        {
            string key = GetKeyFromEndPoint(endPoint);

            if (!players.TryGetValue(key, out LANLobbyUser? user))
                return false;

            _ = players.Remove(key);

            // Check if any other player has the same username
            bool usernameStillInUse = players.Values.Any(p => p.Name == user.Name);

            if (!usernameStillInUse)
                knownUsernames.Remove(user.Name);

            PlayerRemoved?.Invoke(this, new PlayerRemovedEventArgs(user, !usernameStillInUse));

            return true;
        }
    }

    /// <summary>
    /// Gets a thread-safe snapshot of all players.
    /// </summary>
    public List<LANLobbyUser> GetAllPlayers()
    {
        lock (lockObject)
        {
            return players.Values.ToList();
        }
    }

    /// <summary>
    /// Clears all players from both internal tracking and notifies subscribers.
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            players.Clear();
            knownUsernames.Clear();
            PlayersCleared?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the current player count.
    /// </summary>
    public int Count
    {
        get
        {
            lock (lockObject)
            {
                return players.Count;
            }
        }
    }
}
