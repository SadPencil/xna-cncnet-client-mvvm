using AvMainClientMvvmContract.Multiplayer.GameLobby;

namespace AvMainClientView.Multiplayer.GameLobby;

public interface ILANGameLobbyView : IMultiplayerGameLobbyView
{
    new ILANGameLobbyViewModel? ViewModel { get; set; }
}
