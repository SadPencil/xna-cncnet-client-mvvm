using System;

namespace AvMainClientViewModel.Multiplayer.GameLobby;

public class GameBroadcastEventArgs : EventArgs
{
    public GameBroadcastEventArgs(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
