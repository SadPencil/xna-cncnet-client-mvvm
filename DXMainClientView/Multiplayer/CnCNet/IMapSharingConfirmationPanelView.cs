using DXMainClientMvvmContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IMapSharingConfirmationPanelView
{
    IMapSharingConfirmationPanelViewModel? ViewModel { get; set; }
}
