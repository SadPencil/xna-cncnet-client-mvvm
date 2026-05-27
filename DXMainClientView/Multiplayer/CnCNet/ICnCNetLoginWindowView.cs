#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ICnCNetLoginWindowView
{
    ICnCNetLoginWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
