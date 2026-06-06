using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

public interface IMapSharingConfirmationPanelViewModel : INotifyPropertyChanged
{
    string MapName { get; }
    string HostName { get; }
    string StatusText { get; }
    bool IsDownloadAllowed { get; }
    bool IsVisible { get; set; }

    IAsyncRelayCommand DownloadMapCommand { get; }
    IRelayCommand CancelCommand { get; }
}
