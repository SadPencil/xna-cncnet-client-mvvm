#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface IGameLobbyView : ISwitchableView
{
    IGameLobbyViewModel? ViewModel { get; set; }
}
