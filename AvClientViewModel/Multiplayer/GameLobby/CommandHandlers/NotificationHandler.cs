using System;

namespace AvClientViewModel.Multiplayer.GameLobby.CommandHandlers;

public class NotificationHandler : CommandHandlerBase
{
    private readonly Action<string, Action> action;
    private readonly Action innerAction;

    public NotificationHandler(string commandName, Action<string, Action> action,
        Action innerAction) : base(commandName)
    {
        this.action = action;
        this.innerAction = innerAction;
    }

    public override bool Handle(string sender, string message)
    {
        if (message == CommandName)
        {
            action(sender, innerAction);
            return true;
        }

        return false;
    }
}
