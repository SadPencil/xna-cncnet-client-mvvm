#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameLobbyView : ISwitchableView
{
    IGameLobbyViewModel? ViewModel { get; set; }
}
