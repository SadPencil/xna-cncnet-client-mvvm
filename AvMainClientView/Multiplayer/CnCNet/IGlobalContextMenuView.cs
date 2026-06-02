using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface IGlobalContextMenuView
{
    IGlobalContextMenuViewModel? ViewModel { get; set; }
    void ShowAt(int x, int y);
    void Hide();
}
