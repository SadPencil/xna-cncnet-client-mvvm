using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface IGameFiltersPanelView
{
    IGameFiltersPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
