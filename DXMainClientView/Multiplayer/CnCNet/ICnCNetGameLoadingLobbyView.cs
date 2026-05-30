using DXMainClientMVVMContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ICnCNetGameLoadingLobbyView : IGameLoadingLobbyView
{
    new ICnCNetGameLoadingLobbyViewModel? ViewModel { get; set; }
}
