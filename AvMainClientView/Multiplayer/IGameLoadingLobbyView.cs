using AvMainClientMvvmContract.Multiplayer;

namespace AvMainClientView.Multiplayer;

public interface IGameLoadingLobbyView
{
    IGameLoadingLobbyViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
