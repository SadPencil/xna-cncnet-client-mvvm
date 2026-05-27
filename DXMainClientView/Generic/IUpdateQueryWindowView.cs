#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IUpdateQueryWindowView
{
    IUpdateQueryWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
