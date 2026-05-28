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
    /// </summary>
    public partial class OptionsWindowViewModel : ObservableObject, IOptionsWindowViewModel
    {
        [ObservableProperty]
        private int selectedPanelIndex;

        [ObservableProperty]
        private bool isComponentsPanelVisible;

        [ObservableProperty]
        private bool isComponentDownloadInProgress;

        [ObservableProperty]
        private bool isVisible;

        // Events for View-specific panel operations
        public event Action? LoadPanelsRequested;
        public event Action<Action<bool>>? RefreshPanelsRequested;
        public event Action<Action<bool>>? SavePanelsRequested;
        public event Action<bool>? ToggleMainMenuOnlyOptionsRequested;
        public event Action? DisablePanelsRequested;
        public event Action? OpenComponentsPanelRequested;
        public event Action<int>? InstallComponentRequested;
        public event Action? PostInitRequested;

        // Events for dialog/close operations
        public event Action? ForceUpdateRequested;
        public event Action<string, string>? MessageBoxRequested;
        public event Action<string, string, Action<bool>>? YesNoDialogRequested;
        public event Action? RestartRequested;
        public event Action? CloseRequested;

        public OptionsWindowViewModel()
        {
        }

        public void Initialize()
        {
            IsComponentsPanelVisible = !ClientConfiguration.Instance.ModMode;
        }

        [RelayCommand]
        private void Save()
        {
            if (IsComponentDownloadInProgress)
            {
                YesNoDialogRequested?.Invoke(
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
                YesNoDialogRequested?.Invoke(
                    "Downloads in progress".L10N("Client:DTAConfig:DownloadingTitle"),
                    "Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu.\n\nAre you sure you want to continue?".L10N("Client:DTAConfig:DownloadingText"),
                    yes =>
                    {
                        if (yes)
                            CloseRequested?.Invoke();
                    });
                return;
            }

            CloseRequested?.Invoke();
        }

        [RelayCommand]
        private void OpenComponentsPanel()
        {
            SelectedPanelIndex = 5;
        }

        /// <summary>
        /// Opens the options window. Loads panels, refreshes to check for value changes,
        /// opens components panel, and makes window visible.
        /// </summary>
        public void Open()
        {
            LoadPanelsRequested?.Invoke();
            RefreshOptionPanels();
            OpenComponentsPanelRequested?.Invoke();
            IsVisible = true;
        }

        /// <summary>
        /// Refreshes settings by loading panels, checking for changes,
        /// saving panels, and persisting settings.
        /// </summary>
        public void RefreshSettings()
        {
            LoadPanelsRequested?.Invoke();
            RefreshOptionPanels();
            SavePanelsRequested?.Invoke(_ => { });
            UserINISettings.Instance.SaveSettings();
        }

        public void SwitchToCustomComponentsPanel()
        {
            DisablePanelsRequested?.Invoke();
            SelectedPanelIndex = 5;
        }

        public void ToggleMainMenuOnlyOptions(bool enable)
        {
            ToggleMainMenuOnlyOptionsRequested?.Invoke(enable);
        }

        public void OnClosed()
        {
            IsVisible = false;
        }

        /// <summary>
        /// Installs a custom component by ID.
        /// </summary>
        public void InstallCustomComponent(int id)
        {
            InstallComponentRequested?.Invoke(id);
        }

        /// <summary>
        /// Post-initialization for display options panel (TS client only).
        /// </summary>
        public void PostInit()
        {
            if (ClientConfiguration.Instance.ClientGameType == ClientCore.Enums.ClientType.TS)
                PostInitRequested?.Invoke();
        }

        private void SaveSettings()
        {
            if (RefreshOptionPanels())
                return;

            bool restartRequired = false;

            SavePanelsRequested?.Invoke(restart =>
            {
                restartRequired = restartRequired || restart;
            });

            try
            {
                UserINISettings.Instance.SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Log("Saving settings failed! Error message: " + ex.ToString());
                MessageBoxRequested?.Invoke(
                    "Saving Settings Failed".L10N("Client:DTAConfig:SaveSettingFailTitle"),
                    "Saving settings failed! Error message:".L10N("Client:DTAConfig:SaveSettingFailText") + " " + ex.Message);
            }

            IsVisible = false;

            if (restartRequired)
            {
                YesNoDialogRequested?.Invoke(
                    "Restart Required".L10N("Client:DTAConfig:RestartClientTitle"),
                    ("The client needs to be restarted for some of the changes to take effect.\n\n" +
                    "Do you want to restart now?").L10N("Client:DTAConfig:RestartClientText"),
                    yes =>
                    {
                        if (yes)
                            RestartRequested?.Invoke();
                    });
            }
        }

        /// <summary>
        /// Refreshes the option panels to account for possible
        /// changes that could affect their functionality.
        /// Shows the popup to inform the user if needed.
        /// </summary>
        /// <returns>A bool that determines whether the
        /// settings values were changed.</returns>
        private bool RefreshOptionPanels()
        {
            bool optionValuesChanged = false;

            RefreshPanelsRequested?.Invoke(changed =>
            {
                optionValuesChanged = optionValuesChanged || changed;
            });

            if (optionValuesChanged)
            {
                MessageBoxRequested?.Invoke(
                    "Setting Value(s) Changed".L10N("Client:DTAConfig:SettingChangedTitle"),
                    ("One or more setting values are\n" +
                    "no longer available and were changed.\n\n" +
                    "You may want to verify the new setting\n" +
                    "values in client's options window.").L10N("Client:DTAConfig:SettingChangedText"));

                return true;
            }

            return false;
        }

        [RelayCommand]
        private void ForceUpdate()
        {
            ForceUpdateRequested?.Invoke();
        }
    }
}
