using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface ICnCNetGameLoadingLobbyView : IGameLoadingLobbyView
{
    new ICnCNetGameLoadingLobbyViewModel? ViewModel { get; set; }
}
