using AvMainClientMvvmContract.Multiplayer;

namespace AvMainClientView.Multiplayer;

public interface IGameInformationPanelView
{
    IGameInformationPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
