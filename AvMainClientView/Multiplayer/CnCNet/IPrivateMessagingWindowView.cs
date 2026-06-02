using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface IPrivateMessagingWindowView : ISwitchableView
{
    IPrivateMessagingWindowViewModel? ViewModel { get; set; }
}
