using AvClientMvvmContract.Multiplayer.GameLobby;

namespace AvClientView.Multiplayer.GameLobby;

public interface ILANGameLobbyView : IMultiplayerGameLobbyView
{
    new ILANGameLobbyViewModel? ViewModel { get; set; }
}
