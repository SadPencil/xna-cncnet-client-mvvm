using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

public interface ICnCNetLoginWindowViewModel : INotifyPropertyChanged
{
    string UserName { get; set; }
    bool RememberPassword { get; set; }
    bool IsVisible { get; set; }

    IRelayCommand ConnectCommand { get; }
    IRelayCommand CancelCommand { get; }

    void LoadSettings();
}
