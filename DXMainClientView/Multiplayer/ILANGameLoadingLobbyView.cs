using DXMainClientMvvmContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface ILANGameLoadingLobbyView : IGameLoadingLobbyView
{
    new ILANGameLoadingLobbyViewModel? ViewModel { get; set; }
}
