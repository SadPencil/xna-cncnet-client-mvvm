using DXMainClientMVVMContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IPlayerExtraOptionsPanelView
{
    IPlayerExtraOptionsPanelViewModel? ViewModel { get; set; }
}
