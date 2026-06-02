using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface IChoiceNotificationBoxView
{
    IChoiceNotificationBoxViewModel? ViewModel { get; set; }
}
