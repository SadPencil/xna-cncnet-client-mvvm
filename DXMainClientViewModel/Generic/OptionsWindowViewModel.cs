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

        public void Open()
        {
            // View handles panel loading
        }

        public void RefreshSettings()
        {
            // View handles panel refresh
        }

        public void SwitchToCustomComponentsPanel()
        {
            SelectedPanelIndex = 5;
        }

        public void ToggleMainMenuOnlyOptions(bool enable)
        {
            // View handles panel toggling
        }

        public void OnClosed()
        {
            // View handles post-close logic
        }

        private void SaveSettings()
        {
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

            CloseRequested?.Invoke();
        }

        /// <summary>
        /// Called when the force update button is clicked.
        /// </summary>
        public void OnForceUpdate()
        {
            ForceUpdateRequested?.Invoke();
        }
    }
}
