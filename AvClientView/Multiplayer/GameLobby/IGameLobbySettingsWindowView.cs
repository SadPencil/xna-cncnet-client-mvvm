using AvClientMvvmContract.Multiplayer.GameLobby;

namespace AvClientView.Multiplayer.GameLobby;

public interface IGameLobbySettingsWindowView
{
    IGameLobbySettingsWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
