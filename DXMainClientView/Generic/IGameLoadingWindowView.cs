using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public interface IGameLoadingWindowView
{
    IGameLoadingWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
