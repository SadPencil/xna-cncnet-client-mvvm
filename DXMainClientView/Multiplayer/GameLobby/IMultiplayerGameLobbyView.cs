#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface IMultiplayerGameLobbyView : IGameLobbyView
{
    new IMultiplayerGameLobbyViewModel? ViewModel { get; set; }
}
