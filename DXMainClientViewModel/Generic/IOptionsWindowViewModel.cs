using System;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IOptionsWindowViewModel : INotifyPropertyChanged
{
    int SelectedPanelIndex { get; set; }
    bool IsComponentsPanelVisible { get; }
    bool IsComponentDownloadInProgress { get; }
    bool IsVisible { get; set; }

    IRelayCommand SaveCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand OpenComponentsPanelCommand { get; }
    IRelayCommand ForceUpdateCommand { get; }

    // Events for View-specific panel operations
    event Action? LoadPanelsRequested;
    event Action<Action<bool>>? RefreshPanelsRequested;
    event Action<Action<bool>>? SavePanelsRequested;
    event Action<bool>? ToggleMainMenuOnlyOptionsRequested;
    event Action? DisablePanelsRequested;
    event Action? OpenComponentsPanelRequested;
    event Action<int>? InstallComponentRequested;
    event Action? PostInitRequested;

    // Events for dialog/close operations
    event Action? ForceUpdateRequested;
    event Action<string, string>? MessageBoxRequested;
    event Action<string, string, Action<bool>>? YesNoDialogRequested;
    event Action? RestartRequested;
    event Action? CloseRequested;

    void Initialize();
    void Open();
    void RefreshSettings();
    void SwitchToCustomComponentsPanel();
    void ToggleMainMenuOnlyOptions(bool enable);
    void OnClosed();
    void InstallCustomComponent(int id);
    void PostInit();
}
