using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMVVMContract.Multiplayer.CnCNet;

public interface ICnCNetLoginWindowViewModel : INotifyPropertyChanged
{
    string UserName { get; set; }
    bool RememberPassword { get; set; }
    bool PersistentMode { get; set; }
    bool AutoConnect { get; set; }
    bool IsAutoConnectAllowed { get; }
    bool IsWindowVisible { get; set; }

    IAsyncRelayCommand ConnectCommand { get; }
    IRelayCommand CancelCommand { get; }
}
