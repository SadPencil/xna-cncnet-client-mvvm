#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IExtrasWindowView
{
    IExtrasWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
