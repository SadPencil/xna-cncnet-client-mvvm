#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameLoadingLobbyView
{
    IGameLoadingLobbyViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
