#nullable enable
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IPasswordRequestWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; }
    string HostName { get; }
    string Password { get; set; }

    IRelayCommand SubmitPasswordCommand { get; }
    IRelayCommand CancelCommand { get; }
}
