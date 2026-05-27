#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameInProgressWindowView
{
    IGameInProgressWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
