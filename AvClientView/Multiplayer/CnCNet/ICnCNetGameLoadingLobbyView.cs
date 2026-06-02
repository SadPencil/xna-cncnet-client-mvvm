using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface ICnCNetGameLoadingLobbyView : IGameLoadingLobbyView
{
    new ICnCNetGameLoadingLobbyViewModel? ViewModel { get; set; }
}
