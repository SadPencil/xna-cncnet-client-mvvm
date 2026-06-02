using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface IChoiceNotificationBoxView
{
    IChoiceNotificationBoxViewModel? ViewModel { get; set; }
}
