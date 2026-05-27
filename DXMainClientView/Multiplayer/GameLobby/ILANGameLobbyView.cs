#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface ILANGameLobbyView : IMultiplayerGameLobbyView
{
    new ILANGameLobbyViewModel? ViewModel { get; set; }
}
