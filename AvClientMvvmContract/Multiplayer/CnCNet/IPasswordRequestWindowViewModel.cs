using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

public interface IPasswordRequestWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; }
    string HostName { get; }
    string Password { get; set; }
    bool IsVisible { get; set; }

    IRelayCommand SubmitPasswordCommand { get; }
    IRelayCommand CancelCommand { get; }
}
