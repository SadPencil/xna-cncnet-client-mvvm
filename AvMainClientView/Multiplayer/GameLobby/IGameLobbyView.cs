using AvMainClientMvvmContract.Multiplayer.GameLobby;

namespace AvMainClientView.Multiplayer.GameLobby;

public interface IGameLobbyView : ISwitchableView
{
    IGameLobbyViewModel? ViewModel { get; set; }
}
