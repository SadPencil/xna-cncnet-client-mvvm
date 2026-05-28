using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;
using System;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the update query window.
    /// Handles update acceptance/decline and version info display.
    /// </summary>
    public partial class UpdateQueryWindowViewModel : ObservableObject, IUpdateQueryWindowViewModel
    {
        [ObservableProperty]
        private string descriptionText = string.Empty;

        [ObservableProperty]
        private string updateSizeText = string.Empty;

        [ObservableProperty]
        private bool isVisible;

        /// <summary>
        /// Raised when the user accepts the update.
        /// </summary>
        public event Action? UpdateAccepted;

        /// <summary>
        /// Raised when the user declines the update.
        /// </summary>
        public event Action? UpdateDeclined;

        /// <summary>
        /// Sets the update info to display.
        /// </summary>
        public void SetInfo(string version, int updateSize)
        {
            DescriptionText = string.Format(
                "Version {0} is available for download.\nDo you wish to install it?".L10N("Client:Main:VersionAvailable"),
                version);

            if (updateSize >= 1000)
                UpdateSizeText = string.Format("The size of the update is {0} MB.".L10N("Client:Main:UpdateSizeMB"), updateSize / 1000);
            else
                UpdateSizeText = string.Format("The size of the update is {0} KB.".L10N("Client:Main:UpdateSizeKB"), updateSize);
        }

        [RelayCommand]
        private void Accept()
        {
            IsVisible = false;
            UpdateAccepted?.Invoke();
        }

        [RelayCommand]
        private void Decline()
        {
            IsVisible = false;
            UpdateDeclined?.Invoke();
        }

        [RelayCommand]
        private void ViewChangelog()
        {
            ProcessLauncher.StartShellProcess(ClientConfiguration.Instance.ChangelogURL);
        }
    }
}
