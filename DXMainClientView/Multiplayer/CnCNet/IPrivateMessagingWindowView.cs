using DXMainClientMvvmContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IPrivateMessagingWindowView : ISwitchableView
{
    IPrivateMessagingWindowViewModel? ViewModel { get; set; }
}
