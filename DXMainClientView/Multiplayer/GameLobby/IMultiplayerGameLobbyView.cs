using DXMainClientMVVMContract.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface IMultiplayerGameLobbyView : IGameLobbyView
{
    new IMultiplayerGameLobbyViewModel? ViewModel { get; set; }
}
