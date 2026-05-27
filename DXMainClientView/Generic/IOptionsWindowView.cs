#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IOptionsWindowView
{
    IOptionsWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
