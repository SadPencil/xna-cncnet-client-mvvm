using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer.CnCNet;

public interface IMapSharingConfirmationPanelViewModel : INotifyPropertyChanged
{
    string MapName { get; }
    string HostName { get; }
    string StatusText { get; }
    bool IsDownloadAvailable { get; }
    bool IsPanelVisible { get; set; }

    IAsyncRelayCommand DownloadMapCommand { get; }
    IRelayCommand CancelCommand { get; }
}
