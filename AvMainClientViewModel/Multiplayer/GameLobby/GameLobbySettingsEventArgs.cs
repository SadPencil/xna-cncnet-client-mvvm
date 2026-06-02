using System;

namespace AvMainClientViewModel.Multiplayer.GameLobby;

public class GameLobbySettingsEventArgs : EventArgs
{
    public GameLobbySettingsEventArgs(string gameRoomName, int maxPlayers, int skillLevel, string password)
    {
        GameRoomName = gameRoomName;
        MaxPlayers = maxPlayers;
        SkillLevel = skillLevel;
        Password = password;
    }

    public string GameRoomName { get; }
    public int MaxPlayers { get; }
    public int SkillLevel { get; }
    public string Password { get; }
}
