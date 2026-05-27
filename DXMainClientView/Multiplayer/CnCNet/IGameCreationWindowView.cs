#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameCreationWindowView
{
    IGameCreationWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
