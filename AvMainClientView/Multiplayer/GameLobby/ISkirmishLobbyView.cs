using AvMainClientMvvmContract.Multiplayer.GameLobby;

namespace AvMainClientView.Multiplayer.GameLobby;

public interface ISkirmishLobbyView : IGameLobbyView
{
    new ISkirmishLobbyViewModel? ViewModel { get; set; }
}
