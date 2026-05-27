#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGlobalContextMenuView
{
    IGlobalContextMenuViewModel? ViewModel { get; set; }
    void ShowAt(int x, int y);
    void Hide();
}
