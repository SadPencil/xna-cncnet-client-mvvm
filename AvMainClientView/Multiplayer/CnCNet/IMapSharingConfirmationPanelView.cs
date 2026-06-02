using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface IMapSharingConfirmationPanelView
{
    IMapSharingConfirmationPanelViewModel? ViewModel { get; set; }
}
