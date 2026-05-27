#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface IGameLobbySettingsWindowView
{
    IGameLobbySettingsWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
