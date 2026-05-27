#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface ISkirmishLobbyView : IGameLobbyView
{
    new ISkirmishLobbyViewModel? ViewModel { get; set; }
}
