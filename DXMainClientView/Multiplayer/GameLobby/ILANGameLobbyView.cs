#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ILANGameLobbyView : IMultiplayerGameLobbyView
{
    new ILANGameLobbyViewModel? ViewModel { get; set; }
}
