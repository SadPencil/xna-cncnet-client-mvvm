#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ICnCNetGameLoadingLobbyView : IGameLoadingLobbyView
{
    new ICnCNetGameLoadingLobbyViewModel? ViewModel { get; set; }
}
