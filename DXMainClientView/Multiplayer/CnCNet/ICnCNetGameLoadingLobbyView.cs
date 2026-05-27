#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ICnCNetGameLoadingLobbyView : IGameLoadingLobbyView
{
    new ICnCNetGameLoadingLobbyViewModel? ViewModel { get; set; }
}
