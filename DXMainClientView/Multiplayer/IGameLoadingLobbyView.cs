using DXMainClientMVVMContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IGameLoadingLobbyView
{
    IGameLoadingLobbyViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
