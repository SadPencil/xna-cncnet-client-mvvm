#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IPasswordRequestWindowView
{
    IPasswordRequestWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
