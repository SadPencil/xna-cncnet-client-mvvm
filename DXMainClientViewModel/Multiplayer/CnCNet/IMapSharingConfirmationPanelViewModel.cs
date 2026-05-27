using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface IMapSharingConfirmationPanelViewModel : INotifyPropertyChanged
{
    string MapName { get; }
    string HostName { get; }
    string StatusText { get; }
    bool IsDownloadAvailable { get; }

    IAsyncRelayCommand DownloadMapCommand { get; }
    IRelayCommand CancelCommand { get; }
}
