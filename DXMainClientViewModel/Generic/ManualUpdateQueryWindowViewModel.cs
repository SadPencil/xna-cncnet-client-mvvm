
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;
using System;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the manual update query window.
    /// Handles manual download redirect and version info display.
    /// </summary>
    public partial class ManualUpdateQueryWindowViewModel : ObservableObject, IManualUpdateQueryWindowViewModel
    {
        private string downloadUrl = string.Empty;
        private string descriptionTemplate = "Version {0} is available.\n\nManual download and installation is\nrequired.".L10N("Client:Main:ManualDownloadAvailable");

        [ObservableProperty]
        private string descriptionText = string.Empty;

        [ObservableProperty]
        private bool isVisible;

        /// <summary>
        /// Raised when the window is closed.
        /// </summary>
        public event Action? Closed;

        /// <summary>
        /// Sets the update info to display.
        /// </summary>
        public void SetInfo(string version, string downloadUrl)
        {
            this.downloadUrl = downloadUrl;
            DescriptionText = string.Format(descriptionTemplate, version);
        }

        [RelayCommand]
        private void ViewDownloads()
        {
            ProcessLauncher.StartShellProcess(downloadUrl);
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
            Closed?.Invoke();
        }
    }
}
