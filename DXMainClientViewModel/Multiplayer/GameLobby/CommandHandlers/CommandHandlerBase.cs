namespace DXMainClientViewModel.Multiplayer.GameLobby.CommandHandlers;

public abstract class CommandHandlerBase
{
    public CommandHandlerBase(string commandName)
    {
        CommandName = commandName;
    }

    public string CommandName { get; }

    public abstract bool Handle(string sender, string message);
}
