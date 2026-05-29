
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the manual update query window.
    /// Self-sufficient: subscribes to IUpdateService to detect manual updates.
    /// </summary>
    public partial class ManualUpdateQueryWindowViewModel : ObservableObject, IManualUpdateQueryWindowViewModel
    {
        private readonly IUpdateService updateService;

        private string downloadUrl = string.Empty;
        private string descriptionTemplate = "Version {0} is available.\n\nManual download and installation is\nrequired.".L10N("Client:Main:ManualDownloadAvailable");

        [ObservableProperty]
        private string descriptionText = string.Empty;

        [ObservableProperty]
        private bool isVisible;

        public ManualUpdateQueryWindowViewModel(IUpdateService updateService)
        {
            this.updateService = updateService;
            updateService.FileIdentifiersUpdated += OnFileIdentifiersUpdated;
        }

        private void OnFileIdentifiersUpdated()
        {
            if (updateService.ManualUpdateRequired && !string.IsNullOrEmpty(updateService.ManualDownloadURL))
            {
                downloadUrl = updateService.ManualDownloadURL;
                DescriptionText = string.Format(descriptionTemplate, updateService.ServerGameVersion);
                IsVisible = true;
            }
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
        }
    }
}

