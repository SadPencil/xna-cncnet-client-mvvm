using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface IGlobalContextMenuView
{
    IGlobalContextMenuViewModel? ViewModel { get; set; }
    void ShowAt(int x, int y);
    void Hide();
}
