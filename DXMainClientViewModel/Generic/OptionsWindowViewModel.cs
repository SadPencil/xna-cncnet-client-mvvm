using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;
using ClientCore.Enums;
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
        private readonly IDisplayOptionsPanelViewModel displayOptionsPanel;
        private readonly IAudioOptionsPanelViewModel audioOptionsPanel;
        private readonly IGameOptionsPanelViewModel gameOptionsPanel;
        private readonly ICnCNetOptionsPanelViewModel cncnetOptionsPanel;
        private readonly IUpdaterOptionsPanelViewModel updaterOptionsPanel;
        private readonly IComponentsPanelViewModel componentsPanel;

        private Action<bool>? yesNoDialogCallback;

        [ObservableProperty]
        private int selectedPanelIndex;

        [ObservableProperty]
        private bool isComponentsPanelVisible;

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

        public OptionsWindowViewModel(
            IDisplayOptionsPanelViewModel displayOptionsPanel,
            IAudioOptionsPanelViewModel audioOptionsPanel,
            IGameOptionsPanelViewModel gameOptionsPanel,
            ICnCNetOptionsPanelViewModel cncnetOptionsPanel,
            IUpdaterOptionsPanelViewModel updaterOptionsPanel,
            IComponentsPanelViewModel componentsPanel)
        {
            this.displayOptionsPanel = displayOptionsPanel;
            this.audioOptionsPanel = audioOptionsPanel;
            this.gameOptionsPanel = gameOptionsPanel;
            this.cncnetOptionsPanel = cncnetOptionsPanel;
            this.updaterOptionsPanel = updaterOptionsPanel;
            this.componentsPanel = componentsPanel;

            IsComponentsPanelVisible = !ClientConfiguration.Instance.ModMode;
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
                            SaveSettings();
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
                            IsVisible = false;
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
            // MainMenu subscribes to updateService.ForceUpdate via its own command
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
            // Each panel handles this via its own logic
            // The panels don't currently have ToggleMainMenuOnlyOptions
            // This is a View-level concern (enabling/disabling controls)
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

        private void SaveSettings()
        {
            bool restartRequired = false;

            try
            {
                displayOptionsPanel.SaveSettingsCommand.Execute(null);
                restartRequired = displayOptionsPanel.IsRestartRequired;

                audioOptionsPanel.SaveSettingsCommand.Execute(null);
                gameOptionsPanel.SaveSettingsCommand.Execute(null);
                cncnetOptionsPanel.SaveSettingsCommand.Execute(null);
                updaterOptionsPanel.SaveSettingsCommand.Execute(null);

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
