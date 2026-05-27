#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ICheaterWindowView
{
    ICheaterWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
