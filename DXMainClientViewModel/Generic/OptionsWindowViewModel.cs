using DXMainClientMvvmContract.Generic;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.Enums;
using ClientUpdater;

using Rampastring.Tools;

using System;

using DXMainClientViewModel.Generic.OptionPanels;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the options window.
    /// Coordinates child panel ViewModels directly (no Should* signals).
    /// Self-sufficient: calls child ViewModel commands directly.
    /// </summary>
    public partial class OptionsWindowViewModel : ObservableObject, IOptionsWindowViewModel
    {
        private readonly DisplayOptionsPanelViewModel displayOptionsPanel;
        private readonly AudioOptionsPanelViewModel audioOptionsPanel;
        private readonly GameOptionsPanelViewModel gameOptionsPanel;
        private readonly CnCNetOptionsPanelViewModel cncnetOptionsPanel;
        private readonly UpdaterOptionsPanelViewModel updaterOptionsPanel;
        private readonly ComponentsPanelViewModel componentsPanel;

        private Action<bool>? yesNoDialogCallback;

        [ObservableProperty]
        private int selectedPanelIndex;

        [ObservableProperty]
        private bool isComponentsPanelVisible;

        [ObservableProperty]
        private bool isUpdaterPanelVisible;

        [ObservableProperty]
        private bool isComponentDownloadInProgress;

        [ObservableProperty]
        private bool isVisible;

        // Dialog state
        [ObservableProperty]
        private bool isMessageBoxVisible;

        [ObservableProperty]
        private string messageBoxTitle = string.Empty;

        [ObservableProperty]
        private string messageBoxMessage = string.Empty;

        [ObservableProperty]
        private bool isYesNoDialogVisible;

        [ObservableProperty]
        private string yesNoDialogTitle = string.Empty;

        [ObservableProperty]
        private string yesNoDialogMessage = string.Empty;

        // Domain events (for MainMenu to subscribe, not on interface)
        public event EventHandler? RestartRequested;
        public event EventHandler? ForceUpdateRequested;

        public OptionsWindowViewModel(
            DisplayOptionsPanelViewModel displayOptionsPanel,
            AudioOptionsPanelViewModel audioOptionsPanel,
            GameOptionsPanelViewModel gameOptionsPanel,
            CnCNetOptionsPanelViewModel cncnetOptionsPanel,
            UpdaterOptionsPanelViewModel updaterOptionsPanel,
            ComponentsPanelViewModel componentsPanel)
        {
            this.displayOptionsPanel = displayOptionsPanel;
            this.audioOptionsPanel = audioOptionsPanel;
            this.gameOptionsPanel = gameOptionsPanel;
            this.cncnetOptionsPanel = cncnetOptionsPanel;
            this.updaterOptionsPanel = updaterOptionsPanel;
            this.componentsPanel = componentsPanel;

            // Original: if ModMode || no update mirrors -> hide both updater and components tabs
            // else if no custom components -> hide only components tab
            if (ClientConfiguration.Instance.ModMode || Updater.UpdateMirrors == null || Updater.UpdateMirrors.Count < 1)
            {
                IsUpdaterPanelVisible = false;
                IsComponentsPanelVisible = false;
            }
            else if (Updater.CustomComponents == null || Updater.CustomComponents.Count < 1)
            {
                IsUpdaterPanelVisible = true;
                IsComponentsPanelVisible = false;
            }
            else
            {
                IsUpdaterPanelVisible = true;
                IsComponentsPanelVisible = true;
            }
        }

        partial void OnIsMessageBoxVisibleChanged(bool value)
        {
            if (!value)
            {
                MessageBoxTitle = string.Empty;
                MessageBoxMessage = string.Empty;
            }
        }

        partial void OnIsYesNoDialogVisibleChanged(bool value)
        {
            if (!value)
            {
                YesNoDialogTitle = string.Empty;
                YesNoDialogMessage = string.Empty;
                yesNoDialogCallback = null;
            }
        }

        #region Commands

        [RelayCommand]
        private void Save()
        {
            if (IsComponentDownloadInProgress)
            {
                ShowYesNoDialog(
                    "Downloads in progress".L10N("Client:DTAConfig:DownloadingTitle"),
                    "Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu.\n\nAre you sure you want to continue?".L10N("Client:DTAConfig:DownloadingText"),
                    yes =>
                    {
                        if (yes)
                        {
                            componentsPanel.CancelDownloadsCommand.Execute(null);
                            SaveSettings();
                        }
                    });
                return;
            }

            SaveSettings();
        }

        [RelayCommand]
        private void Cancel()
        {
            if (IsComponentDownloadInProgress)
            {
                ShowYesNoDialog(
                    "Downloads in progress".L10N("Client:DTAConfig:DownloadingTitle"),
                    "Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu.\n\nAre you sure you want to continue?".L10N("Client:DTAConfig:DownloadingText"),
                    yes =>
                    {
                        if (yes)
                        {
                            componentsPanel.CancelDownloadsCommand.Execute(null);
                            IsVisible = false;
                        }
                    });
                return;
            }

            IsVisible = false;
        }

        [RelayCommand]
        private void OpenComponentsPanel()
        {
            SelectedPanelIndex = 5;
        }

        [RelayCommand]
        private void ForceUpdate()
        {
            IsVisible = false;
            ForceUpdateRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void DismissMessageBox()
        {
            IsMessageBoxVisible = false;
        }

        [RelayCommand]
        private void YesNoDialogYes()
        {
            IsYesNoDialogVisible = false;
            yesNoDialogCallback?.Invoke(true);
        }

        [RelayCommand]
        private void YesNoDialogNo()
        {
            IsYesNoDialogVisible = false;
            yesNoDialogCallback?.Invoke(false);
        }

        #endregion

        #region Lifecycle (called on concrete class by MainMenu)

        public void Open()
        {
            // Directly load all panels
            displayOptionsPanel.LoadSettingsCommand.Execute(null);
            audioOptionsPanel.LoadSettingsCommand.Execute(null);
            gameOptionsPanel.LoadSettingsCommand.Execute(null);
            cncnetOptionsPanel.LoadSettingsCommand.Execute(null);
            updaterOptionsPanel.LoadSettingsCommand.Execute(null);

            RefreshOptionPanels();

            componentsPanel.RefreshComponentsCommand.Execute(null);

            IsVisible = true;
        }

        public void RefreshSettings()
        {
            // Reload all panels from INI
            displayOptionsPanel.LoadSettingsCommand.Execute(null);
            audioOptionsPanel.LoadSettingsCommand.Execute(null);
            gameOptionsPanel.LoadSettingsCommand.Execute(null);
            cncnetOptionsPanel.LoadSettingsCommand.Execute(null);
            updaterOptionsPanel.LoadSettingsCommand.Execute(null);

            RefreshOptionPanels();

            // Save all panels back
            displayOptionsPanel.SaveSettingsCommand.Execute(null);
            audioOptionsPanel.SaveSettingsCommand.Execute(null);
            gameOptionsPanel.SaveSettingsCommand.Execute(null);
            cncnetOptionsPanel.SaveSettingsCommand.Execute(null);
            updaterOptionsPanel.SaveSettingsCommand.Execute(null);

            UserINISettings.Instance.SaveSettings();
        }

        public void SwitchToCustomComponentsPanel()
        {
            SelectedPanelIndex = 5;
        }

        public void ToggleMainMenuOnlyOptions(bool enable)
        {
            updaterOptionsPanel.IsForceUpdateEnabled = enable;
        }

        public void InstallCustomComponent(int id)
        {
            componentsPanel.SelectedComponentIndex = id;
            componentsPanel.InstallSelectedComponentCommand.Execute(null);
        }

        public void PostInit()
        {
            // Compatibility fix check is now done in DisplayOptionsPanelViewModel constructor
        }

        public void OnClosed()
        {
            IsVisible = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Refreshes the option panels to account for possible
        /// changes that could affect their functionality.
        /// Shows the popup to inform the user if needed.
        /// Corresponds to RefreshOptionPanels() in the original OptionsWindow.
        /// </summary>
        /// <returns>A bool that determines whether the
        /// settings values were changed.</returns>
        private bool RefreshOptionPanels()
        {
            // Re-load all panels to pick up any INI changes.
            // In the original, RefreshPanel() checks IFileSetting entries and returns
            // true if any setting value was forced to change (e.g., a resolution no longer available).
            // Currently the ViewModel panels don't have this concept, so we just reload.
            displayOptionsPanel.LoadSettingsCommand.Execute(null);
            audioOptionsPanel.LoadSettingsCommand.Execute(null);
            gameOptionsPanel.LoadSettingsCommand.Execute(null);
            cncnetOptionsPanel.LoadSettingsCommand.Execute(null);
            updaterOptionsPanel.LoadSettingsCommand.Execute(null);

            return false;
        }

        private void SaveSettings()
        {
            if (RefreshOptionPanels())
                return;

            bool restartRequired = false;

            try
            {
                displayOptionsPanel.SaveSettingsCommand.Execute(null);
                restartRequired = displayOptionsPanel.IsRestartRequired || restartRequired;

                audioOptionsPanel.SaveSettingsCommand.Execute(null);
                restartRequired = audioOptionsPanel.IsRestartRequired || restartRequired;

                gameOptionsPanel.SaveSettingsCommand.Execute(null);
                restartRequired = gameOptionsPanel.IsRestartRequired || restartRequired;

                cncnetOptionsPanel.SaveSettingsCommand.Execute(null);
                restartRequired = cncnetOptionsPanel.IsRestartRequired || restartRequired;

                updaterOptionsPanel.SaveSettingsCommand.Execute(null);
                restartRequired = updaterOptionsPanel.IsRestartRequired || restartRequired;

                UserINISettings.Instance.SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Log("Saving settings failed! Error message: " + ex.ToString());
                ShowMessageBox(
                    "Saving Settings Failed".L10N("Client:DTAConfig:SaveSettingFailTitle"),
                    "Saving settings failed! Error message:".L10N("Client:DTAConfig:SaveSettingFailText") + " " + ex.Message);
            }

            IsVisible = false;

            if (restartRequired)
            {
                ShowYesNoDialog(
                    "Restart Required".L10N("Client:DTAConfig:RestartClientTitle"),
                    ("The client needs to be restarted for some of the changes to take effect.\n\n" +
                    "Do you want to restart now?").L10N("Client:DTAConfig:RestartClientText"),
                    yes =>
                    {
                        if (yes)
                            RestartRequested?.Invoke(this, EventArgs.Empty);
                    });
            }
        }

        private void ShowMessageBox(string title, string message)
        {
            MessageBoxTitle = title;
            MessageBoxMessage = message;
            IsMessageBoxVisible = true;
        }

        private void ShowYesNoDialog(string title, string message, Action<bool> callback)
        {
            yesNoDialogCallback = callback;
            YesNoDialogTitle = title;
            YesNoDialogMessage = message;
            IsYesNoDialogVisible = true;
        }

        #endregion
    }
}


