using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface ILANGameLoadingLobbyView : IGameLoadingLobbyView
{
    new ILANGameLoadingLobbyViewModel? ViewModel { get; set; }
}
