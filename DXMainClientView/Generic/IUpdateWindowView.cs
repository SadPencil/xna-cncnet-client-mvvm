#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IUpdateWindowView
{
    IUpdateWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
