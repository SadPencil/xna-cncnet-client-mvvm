using System;

namespace AvMainClientViewModel.Multiplayer.GameLobby.CommandHandlers;

public class StringCommandHandler : CommandHandlerBase
{
    private readonly Action<string, string> commandHandler;

    public StringCommandHandler(string commandName, Action<string, string> commandHandler) : base(commandName)
    {
        this.commandHandler = commandHandler;
    }

    public override bool Handle(string sender, string message)
    {
        if (message.Length < CommandName.Length + 1)
            return false;

        if (message.StartsWith(CommandName))
        {
            string parameters = message.Substring(CommandName.Length + 1);
            commandHandler.Invoke(sender, parameters);
            return true;
        }

        return false;
    }
}
