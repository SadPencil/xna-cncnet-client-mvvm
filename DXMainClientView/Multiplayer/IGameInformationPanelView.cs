#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IGameInformationPanelView
{
    IGameInformationPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
