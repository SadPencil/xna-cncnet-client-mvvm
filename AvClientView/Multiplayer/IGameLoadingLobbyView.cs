using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface IGameLoadingLobbyView
{
    IGameLoadingLobbyViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
