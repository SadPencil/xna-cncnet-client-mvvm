#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ILoadingScreenView
{
    ILoadingScreenViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
