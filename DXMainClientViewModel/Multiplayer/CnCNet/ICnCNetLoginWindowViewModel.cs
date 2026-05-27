#nullable enable
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface ICnCNetLoginWindowViewModel : INotifyPropertyChanged
{
    string UserName { get; set; }
    string Password { get; set; }
    bool RememberPassword { get; set; }
    bool PersistentMode { get; set; }
    bool AutoConnect { get; set; }

    IAsyncRelayCommand ConnectCommand { get; }
    IRelayCommand CancelCommand { get; }
}
