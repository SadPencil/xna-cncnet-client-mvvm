// checked
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;
using Rampastring.Tools;
using System;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the options window.
    /// Handles settings save/load orchestration, tab selection, and download state.
    /// Self-sufficient: uses observable properties for View coordination.
    /// </summary>
    public partial class OptionsWindowViewModel : ObservableObject, IOptionsWindowViewModel
    {
        private Action<bool>? yesNoDialogCallback;

        [ObservableProperty]
        private int selectedPanelIndex;

        [ObservableProperty]
        private bool isComponentsPanelVisible;

        [ObservableProperty]
        private bool isComponentDownloadInProgress;

        [ObservableProperty]
        private bool isVisible;

        // Panel orchestration signals
        [ObservableProperty]
        private bool shouldLoadPanels;

        [ObservableProperty]
        private bool shouldRefreshPanels;

        [ObservableProperty]
        private bool shouldSavePanels;

        [ObservableProperty]
        private bool shouldDisableAllPanels;

        [ObservableProperty]
        private bool shouldToggleMainMenuOnlyOptions;

        [ObservableProperty]
        private bool toggleMainMenuOnlyOptionsValue;

        [ObservableProperty]
        private bool shouldOpenComponentsPanel;

        [ObservableProperty]
        private bool shouldInstallComponent;

        [ObservableProperty]
        private int componentToInstall;

        [ObservableProperty]
        private bool shouldPostInitDisplayOptions;

        [ObservableProperty]
        private bool shouldRefreshSettings;

        // Panel feedback
        [ObservableProperty]
        private bool panelsChangedValues;

        [ObservableProperty]
        private bool restartRequired;

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

        // Navigation signals
        [ObservableProperty]
        private bool shouldRestart;

        public OptionsWindowViewModel()
        {
            IsComponentsPanelVisible = !ClientConfiguration.Instance.ModMode;
        }

        partial void OnPanelsChangedValuesChanged(bool value)
        {
            if (value)
            {
                ShowMessageBox(
                    "Setting Value(s) Changed".L10N("Client:DTAConfig:SettingChangedTitle"),
                    ("One or more setting values are\n" +
                    "no longer available and were changed.\n\n" +
                    "You may want to verify the new setting\n" +
                    "values in client's options window.").L10N("Client:DTAConfig:SettingChangedText"));
                PanelsChangedValues = false;
            }
        }

        partial void OnRestartRequiredChanged(bool value)
        {
            if (value)
            {
                ShowYesNoDialog(
                    "Restart Required".L10N("Client:DTAConfig:RestartClientTitle"),
                    ("The client needs to be restarted for some of the changes to take effect.\n\n" +
                    "Do you want to restart now?").L10N("Client:DTAConfig:RestartClientText"),
                    yes =>
                    {
                        if (yes)
                            ShouldRestart = true;
                    });
                RestartRequired = false;
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
            ShouldLoadPanels = true;
            ShouldRefreshPanels = true;
            ShouldOpenComponentsPanel = true;
            IsVisible = true;
        }

        public void RefreshSettings()
        {
            ShouldRefreshSettings = true;
        }

        public void SwitchToCustomComponentsPanel()
        {
            ShouldDisableAllPanels = true;
            SelectedPanelIndex = 5;
        }

        public void ToggleMainMenuOnlyOptions(bool enable)
        {
            ToggleMainMenuOnlyOptionsValue = enable;
            ShouldToggleMainMenuOnlyOptions = true;
        }

        public void InstallCustomComponent(int id)
        {
            ComponentToInstall = id;
            ShouldInstallComponent = true;
        }

        public void PostInit()
        {
            if (ClientConfiguration.Instance.ClientGameType == ClientCore.Enums.ClientType.TS)
                ShouldPostInitDisplayOptions = true;
        }

        public void OnClosed()
        {
            IsVisible = false;
        }

        #endregion

        #region Private Methods

        private void SaveSettings()
        {
            ShouldRefreshPanels = true;
            // View sets PanelsChangedValues if panels changed

            ShouldSavePanels = true;
            // View saves panels, sets RestartRequired if needed

            try
            {
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
