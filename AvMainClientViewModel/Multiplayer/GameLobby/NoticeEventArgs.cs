using System;

namespace AvMainClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Event args for lobby notices with severity.
/// </summary>
public class NoticeEventArgs : EventArgs
{
    public string Message { get; }
    public NoticeSeverity Severity { get; }

    public NoticeEventArgs(string message, NoticeSeverity severity)
    {
        Message = message;
        Severity = severity;
    }
}
