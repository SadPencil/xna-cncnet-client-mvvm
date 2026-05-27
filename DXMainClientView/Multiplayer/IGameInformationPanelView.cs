#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameInformationPanelView
{
    IGameInformationPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
