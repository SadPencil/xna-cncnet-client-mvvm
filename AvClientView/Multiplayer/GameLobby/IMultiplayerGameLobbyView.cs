using AvClientMvvmContract.Multiplayer.GameLobby;

namespace AvClientView.Multiplayer.GameLobby;

public interface IMultiplayerGameLobbyView : IGameLobbyView
{
    new IMultiplayerGameLobbyViewModel? ViewModel { get; set; }
}
