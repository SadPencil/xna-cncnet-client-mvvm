using DXMainClientViewModel.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface ILANGameCreationWindowView
{
    ILANGameCreationWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
