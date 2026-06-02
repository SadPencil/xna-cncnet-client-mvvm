using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface IPrivateMessageNotificationBoxView
{
    IPrivateMessageNotificationBoxViewModel? ViewModel { get; set; }
}
