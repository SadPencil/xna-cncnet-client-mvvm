#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameLoadingWindowView
{
    IGameLoadingWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
