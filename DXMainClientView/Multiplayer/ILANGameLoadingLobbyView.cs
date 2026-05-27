#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ILANGameLoadingLobbyView : IGameLoadingLobbyView
{
    new ILANGameLoadingLobbyViewModel? ViewModel { get; set; }
}
