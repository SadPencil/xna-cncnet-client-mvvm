using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Generic;

public interface IOptionsWindowViewModel : INotifyPropertyChanged
{
    int SelectedPanelIndex { get; set; }
    bool IsComponentsPanelVisible { get; }
    bool IsUpdaterPanelVisible { get; }
    bool IsComponentDownloadInProgress { get; set; }
    bool IsVisible { get; set; }

    // Dialog state
    bool IsMessageBoxVisible { get; }
    string MessageBoxTitle { get; }
    string MessageBoxMessage { get; }
    bool IsYesNoDialogVisible { get; }
    string YesNoDialogTitle { get; }
    string YesNoDialogMessage { get; }

    IRelayCommand SaveCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand OpenComponentsPanelCommand { get; }
    IRelayCommand ForceUpdateCommand { get; }
    IRelayCommand DismissMessageBoxCommand { get; }
    IRelayCommand YesNoDialogYesCommand { get; }
    IRelayCommand YesNoDialogNoCommand { get; }

    // Lifecycle methods called by MainMenuViewModel
    void Open();
    void SwitchToCustomComponentsPanel();
}
