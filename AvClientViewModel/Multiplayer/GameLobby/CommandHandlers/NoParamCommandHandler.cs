using System;

namespace AvClientViewModel.Multiplayer.GameLobby.CommandHandlers;

/// <summary>
/// A command handler that handles a command that has no parameter aside from the sender.
/// </summary>
public class NoParamCommandHandler : CommandHandlerBase
{
    private readonly Action<string> commandHandler;

    public NoParamCommandHandler(string commandName, Action<string> commandHandler) : base(commandName)
    {
        this.commandHandler = commandHandler;
    }

    public override bool Handle(string sender, string message)
    {
        if (message == CommandName)
        {
            commandHandler(sender);
            return true;
        }

        return false;
    }
}
