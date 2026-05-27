#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IMapSharingConfirmationPanelView
{
    IMapSharingConfirmationPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
