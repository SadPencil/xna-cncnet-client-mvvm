using System;

namespace AvClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// A command that can be executed by typing a message starting with / on
/// a multiplayer game lobby's chat box.
/// </summary>
public class ChatBoxCommand
{
    public ChatBoxCommand(string command, string description, bool hostOnly, Action<string> action)
    {
        Command = command;
        Description = description;
        HostOnly = hostOnly;
        Action = action;
    }

    public string Command { get; }
    public string Description { get; }
    public bool HostOnly { get; }
    public Action<string> Action { get; }
}
