using AvMainClientMvvmContract.Multiplayer.GameLobby;

namespace AvMainClientView.Multiplayer.GameLobby;

public interface ICnCNetGameLobbyView : IMultiplayerGameLobbyView
{
    new ICnCNetGameLobbyViewModel? ViewModel { get; set; }
}
