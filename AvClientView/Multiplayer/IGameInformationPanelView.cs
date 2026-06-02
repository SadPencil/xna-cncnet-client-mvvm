using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface IGameInformationPanelView
{
    IGameInformationPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
