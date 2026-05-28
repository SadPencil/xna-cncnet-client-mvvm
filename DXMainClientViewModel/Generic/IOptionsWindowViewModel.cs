using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IOptionsWindowViewModel : INotifyPropertyChanged
{
    int SelectedPanelIndex { get; set; }
    bool IsComponentsPanelVisible { get; }
    bool IsComponentDownloadInProgress { get; set; }
    bool IsVisible { get; set; }

    // Panel orchestration signals (View observes and acts)
    bool ShouldLoadPanels { get; set; }
    bool ShouldRefreshPanels { get; set; }
    bool ShouldSavePanels { get; set; }
    bool ShouldDisableAllPanels { get; set; }
    bool ShouldToggleMainMenuOnlyOptions { get; set; }
    bool ToggleMainMenuOnlyOptionsValue { get; }
    bool ShouldOpenComponentsPanel { get; set; }
    bool ShouldInstallComponent { get; set; }
    int ComponentToInstall { get; }
    bool ShouldPostInitDisplayOptions { get; set; }
    bool ShouldRefreshSettings { get; set; }

    // Panel feedback (View sets after operation)
    bool PanelsChangedValues { get; set; }
    bool RestartRequired { get; set; }

    // Dialog state
    bool IsMessageBoxVisible { get; }
    string MessageBoxTitle { get; }
    string MessageBoxMessage { get; }
    bool IsYesNoDialogVisible { get; }
    string YesNoDialogTitle { get; }
    string YesNoDialogMessage { get; }

    // Navigation signals
    bool ShouldRestart { get; set; }

    IRelayCommand SaveCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand OpenComponentsPanelCommand { get; }
    IRelayCommand ForceUpdateCommand { get; }
    IRelayCommand DismissMessageBoxCommand { get; }
    IRelayCommand YesNoDialogYesCommand { get; }
    IRelayCommand YesNoDialogNoCommand { get; }
}
