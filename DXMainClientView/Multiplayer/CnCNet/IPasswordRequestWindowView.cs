using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IPasswordRequestWindowView
{
    IPasswordRequestWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
