using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface IMapSharingConfirmationPanelView
{
    IMapSharingConfirmationPanelViewModel? ViewModel { get; set; }
}
