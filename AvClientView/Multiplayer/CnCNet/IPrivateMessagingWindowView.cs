using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface IPrivateMessagingWindowView : ISwitchableView
{
    IPrivateMessagingWindowViewModel? ViewModel { get; set; }
}
