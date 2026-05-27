#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IMultiplayerGameLobbyView : IGameLobbyView
{
    new IMultiplayerGameLobbyViewModel? ViewModel { get; set; }
}
