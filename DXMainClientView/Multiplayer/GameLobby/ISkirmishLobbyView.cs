using DXMainClientMvvmContract.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface ISkirmishLobbyView : IGameLobbyView
{
    new ISkirmishLobbyViewModel? ViewModel { get; set; }
}
