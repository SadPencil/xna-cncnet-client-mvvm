using DXMainClientMVVMContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IPrivateMessageNotificationBoxView
{
    IPrivateMessageNotificationBoxViewModel? ViewModel { get; set; }
}
