using DXMainClientMVVMContract.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface ILANGameLobbyView : IMultiplayerGameLobbyView
{
    new ILANGameLobbyViewModel? ViewModel { get; set; }
}
