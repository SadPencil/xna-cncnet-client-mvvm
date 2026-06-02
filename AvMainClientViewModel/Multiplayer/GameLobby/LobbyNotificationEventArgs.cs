using System;

namespace AvMainClientViewModel.Multiplayer.GameLobby;

public class LobbyNotificationEventArgs : EventArgs
{
    public LobbyNotificationEventArgs(string notification)
    {
        Notification = notification;
    }

    public string Notification { get; }
}
