#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IOptionsWindowViewModel
{
    int SelectedPanelIndex { get; set; }
    string SelectedPanelName { get; }
    bool IsComponentsPanelVisible { get; }
    bool IsComponentDownloadInProgress { get; }

    IRelayCommand SaveCommand { get; }
    IRelayCommand CancelCommand { get; }
    IAsyncRelayCommand CancelDownloadsCommand { get; }
    IRelayCommand OpenComponentsPanelCommand { get; }

    void Initialize();
}
