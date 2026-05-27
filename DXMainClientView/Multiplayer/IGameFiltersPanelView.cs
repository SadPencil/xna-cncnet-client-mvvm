#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameFiltersPanelView
{
    IGameFiltersPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
