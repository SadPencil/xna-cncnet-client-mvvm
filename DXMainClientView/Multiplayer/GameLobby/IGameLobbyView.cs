using DXMainClientMvvmContract.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface IGameLobbyView : ISwitchableView
{
    IGameLobbyViewModel? ViewModel { get; set; }
}
