using AvClientMvvmContract.Multiplayer.GameLobby;

namespace AvClientView.Multiplayer.GameLobby;

public interface ISkirmishLobbyView : IGameLobbyView
{
    new ISkirmishLobbyViewModel? ViewModel { get; set; }
}
