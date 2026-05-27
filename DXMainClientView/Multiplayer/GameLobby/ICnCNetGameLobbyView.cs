#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface ICnCNetGameLobbyView : IMultiplayerGameLobbyView
{
    new ICnCNetGameLobbyViewModel? ViewModel { get; set; }
}
