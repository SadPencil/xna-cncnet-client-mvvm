using System;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface IPasswordRequestWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; }
    string HostName { get; }
    string Password { get; set; }
    bool IsWindowVisible { get; set; }

    IRelayCommand SubmitPasswordCommand { get; }
    IRelayCommand CancelCommand { get; }

    event EventHandler<PasswordEventArgs>? PasswordEntered;
}
