using DXMainClientViewModel.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IGameFiltersPanelView
{
    IGameFiltersPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
