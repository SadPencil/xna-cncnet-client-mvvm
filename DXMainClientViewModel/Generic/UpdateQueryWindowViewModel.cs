using DXMainClientMvvmContract.Generic;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ClientCore;
using ClientCore.Extensions;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the update query window.
    /// Self-sufficient: subscribes to IUpdateService to detect updates,
    /// calls StartUpdate when user accepts.
    /// </summary>
    public partial class UpdateQueryWindowViewModel : ObservableObject, IUpdateQueryWindowViewModel
    {
        private readonly IUpdateService updateService;

        [ObservableProperty]
        private string descriptionText = string.Empty;

        [ObservableProperty]
        private string updateSizeText = string.Empty;

        [ObservableProperty]
        private bool isVisible;

        public UpdateQueryWindowViewModel(IUpdateService updateService)
        {
            this.updateService = updateService;
            updateService.FileIdentifiersUpdated += OnFileIdentifiersUpdated;
        }

        private void OnFileIdentifiersUpdated()
        {
            if (updateService.VersionState == VersionState.OUTDATED)
            {
                SetInfo(updateService.ServerGameVersion, updateService.UpdateSizeInKb);
                IsVisible = true;
            }
        }

        private void SetInfo(string version, int updateSize)
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
            updateService.StartUpdate();
        }

        [RelayCommand]
        private void Decline()
        {
            IsVisible = false;
        }

        [RelayCommand]
        private void ViewChangelog()
        {
            ProcessLauncher.StartShellProcess(ClientConfiguration.Instance.ChangelogURL);
        }
    }
}


