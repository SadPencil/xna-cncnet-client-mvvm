using AvMainClientMvvmContract.Multiplayer.GameLobby;

namespace AvMainClientView.Multiplayer.GameLobby;

public interface IMultiplayerGameLobbyView : IGameLobbyView
{
    new IMultiplayerGameLobbyViewModel? ViewModel { get; set; }
}
