#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IMapSharingConfirmationPanelViewModel
{
    string MapName { get; }
    string HostName { get; }
    string StatusText { get; }
    bool IsDownloadAvailable { get; }

    IAsyncRelayCommand DownloadMapCommand { get; }
    IRelayCommand CancelCommand { get; }
}
