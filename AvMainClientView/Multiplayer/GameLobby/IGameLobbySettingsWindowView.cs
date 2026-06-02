using AvMainClientMvvmContract.Multiplayer.GameLobby;

namespace AvMainClientView.Multiplayer.GameLobby;

public interface IGameLobbySettingsWindowView
{
    IGameLobbySettingsWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
