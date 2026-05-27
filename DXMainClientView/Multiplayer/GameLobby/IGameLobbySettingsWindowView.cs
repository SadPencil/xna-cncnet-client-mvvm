#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameLobbySettingsWindowView
{
    IGameLobbySettingsWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
