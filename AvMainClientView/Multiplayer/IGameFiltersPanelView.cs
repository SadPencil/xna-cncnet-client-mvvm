using AvMainClientMvvmContract.Multiplayer;

namespace AvMainClientView.Multiplayer;

public interface IGameFiltersPanelView
{
    IGameFiltersPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
