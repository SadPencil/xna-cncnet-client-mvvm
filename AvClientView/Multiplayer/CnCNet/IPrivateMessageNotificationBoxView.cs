using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface IPrivateMessageNotificationBoxView
{
    IPrivateMessageNotificationBoxViewModel? ViewModel { get; set; }
}
