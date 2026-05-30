using DXMainClientMVVMContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IMapSharingConfirmationPanelView
{
    IMapSharingConfirmationPanelViewModel? ViewModel { get; set; }
}
