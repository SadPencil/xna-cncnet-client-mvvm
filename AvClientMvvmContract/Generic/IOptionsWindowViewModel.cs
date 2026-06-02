using System.ComponentModel;

using AvClientMvvmContract.Generic.OptionPanels;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Generic;

public interface IOptionsWindowViewModel : INotifyPropertyChanged
{
    int SelectedPanelIndex { get; set; }
    bool IsComponentsPanelVisible { get; }
    bool IsUpdaterPanelVisible { get; }

    // Sub-panel ViewModels (exposed so axaml can set DataContext)
    IDisplayOptionsPanelViewModel DisplayOptions { get; }
    IAudioOptionsPanelViewModel AudioOptions { get; }
    IGameOptionsPanelViewModel GameOptions { get; }
    ICnCNetOptionsPanelViewModel CnCNetOptions { get; }
    IUpdaterOptionsPanelViewModel UpdaterOptions { get; }
    IComponentsPanelViewModel ComponentsOptions { get; }

    // Panel visibility (derived from SelectedPanelIndex)
    bool IsDisplayPanelVisible { get; }
    bool IsAudioPanelVisible { get; }
    bool IsGamePanelVisible { get; }
    bool IsCnCNetPanelVisible { get; }
    bool IsUpdaterPanelVisibleInner { get; }
    bool IsComponentsPanelVisibleInner { get; }
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
    IRelayCommand SelectDisplayPanelCommand { get; }
    IRelayCommand SelectAudioPanelCommand { get; }
    IRelayCommand SelectGamePanelCommand { get; }
    IRelayCommand SelectCnCNetPanelCommand { get; }
    IRelayCommand SelectUpdaterPanelCommand { get; }
    IRelayCommand SelectComponentsPanelCommand { get; }
    IRelayCommand OpenComponentsPanelCommand { get; }
    IRelayCommand ForceUpdateCommand { get; }
    IRelayCommand DismissMessageBoxCommand { get; }
    IRelayCommand YesNoDialogYesCommand { get; }
    IRelayCommand YesNoDialogNoCommand { get; }
}
