using AvClientMvvmContract.Multiplayer.GameLobby;

namespace AvClientView.Multiplayer.GameLobby;

public interface IGameLobbyView : ISwitchableView
{
    IGameLobbyViewModel? ViewModel { get; set; }
}
