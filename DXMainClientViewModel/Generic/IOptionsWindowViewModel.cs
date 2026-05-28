using System;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IOptionsWindowViewModel : INotifyPropertyChanged
{
    int SelectedPanelIndex { get; set; }
    bool IsComponentsPanelVisible { get; }
    bool IsComponentDownloadInProgress { get; }

    IRelayCommand SaveCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand OpenComponentsPanelCommand { get; }

    /// <summary>
    /// Raised when a force update should be initiated.
    /// </summary>
    event Action? ForceUpdateRequested;

    /// <summary>
    /// Raised when a message box needs to be shown.
    /// </summary>
    event Action<string, string>? MessageBoxRequested;

    /// <summary>
    /// Raised when a yes/no dialog needs to be shown.
    /// </summary>
    event Action<string, string, Action<bool>>? YesNoDialogRequested;

    /// <summary>
    /// Raised when the client needs to restart.
    /// </summary>
    event Action? RestartRequested;

    /// <summary>
    /// Raised when the window should be closed.
    /// </summary>
    event Action? CloseRequested;

    void Initialize();

    /// <summary>
    /// Opens the options window. Loads settings.
    /// </summary>
    void Open();

    /// <summary>
    /// Refreshes settings from the configuration.
    /// </summary>
    void RefreshSettings();

    /// <summary>
    /// Switches to the custom components panel.
    /// </summary>
    void SwitchToCustomComponentsPanel();

    /// <summary>
    /// Toggles main-menu-only options.
    /// </summary>
    void ToggleMainMenuOnlyOptions(bool enable);

    /// <summary>
    /// Called when the window is closed (disabled).
    /// </summary>
    void OnClosed();
}
