using AvClientMvvmContract.Multiplayer.GameLobby;

namespace AvClientView.Multiplayer.GameLobby;

public interface ICnCNetGameLobbyView : IMultiplayerGameLobbyView
{
    new ICnCNetGameLobbyViewModel? ViewModel { get; set; }
}
