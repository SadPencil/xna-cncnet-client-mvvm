using System;

namespace AvMainClientViewModel.Multiplayer.GameLobby.CommandHandlers;

public class IntNotificationHandler : CommandHandlerBase
{
    private readonly Action<string, int, Action<int>> action;
    private readonly Action<int> innerAction;

    public IntNotificationHandler(string commandName, Action<string, int, Action<int>> action,
        Action<int> innerAction) : base(commandName)
    {
        this.action = action;
        this.innerAction = innerAction;
    }

    public override bool Handle(string sender, string message)
    {
        if (message.StartsWith(CommandName))
        {
            string intPart = message.Substring(CommandName.Length + 1);
            bool success = int.TryParse(intPart, out int value);

            action(sender, value, innerAction);
            return true;
        }

        return false;
    }
}
