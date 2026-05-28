using System;

namespace DXMainClientViewModel.Multiplayer.GameLobby.CommandHandlers;

public class IntCommandHandler : CommandHandlerBase
{
    private readonly Action<string, int> handler;

    public IntCommandHandler(string commandName, Action<string, int> handler) : base(commandName)
    {
        this.handler = handler;
    }

    public override bool Handle(string sender, string message)
    {
        if (message.Length < CommandName.Length + 1)
            return false;

        if (message.StartsWith(CommandName))
        {
            bool success = int.TryParse(message.Substring(CommandName.Length + 1), out int value);

            if (success)
            {
                handler(sender, value);
                return true;
            }
        }

        return false;
    }
}
