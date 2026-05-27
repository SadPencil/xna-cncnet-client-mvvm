#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface ILANGameLoadingLobbyView : IGameLoadingLobbyView
{
    new ILANGameLoadingLobbyViewModel? ViewModel { get; set; }
}
