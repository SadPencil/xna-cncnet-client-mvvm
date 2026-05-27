#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ICnCNetGameLobbyView : IMultiplayerGameLobbyView
{
    new ICnCNetGameLobbyViewModel? ViewModel { get; set; }
}
