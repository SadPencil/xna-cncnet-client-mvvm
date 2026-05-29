
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the privacy notification.
    /// Handles privacy policy acceptance.
    /// </summary>
    public partial class PrivacyNotificationViewModel : ObservableObject, IPrivacyNotificationViewModel
    {
        [ObservableProperty]
        private string titleText = "Privacy Policy";

        [ObservableProperty]
        private string descriptionText = "This application makes use of CnCNet web & tunnel server services and is subject to collection of technical & other necessary information through them.";

        [ObservableProperty]
        private string termsAndConditionsUrl = "https://cncnet.org/terms-and-conditions";

        [ObservableProperty]
        private string privacyPolicyUrl = "https://cncnet.org/privacy-policy";

        [ObservableProperty]
        private bool isVisible;

        public PrivacyNotificationViewModel()
        {
            DescriptionText = "This application makes use of CnCNet web & tunnel server services and is subject to collection of technical & other necessary information through them.".L10N("Client:Main:TOSText");
        }

        [RelayCommand]
        private void Accept()
        {
            UserINISettings.Instance.PrivacyPolicyAccepted.Value = true;
            UserINISettings.Instance.SaveSettings();
            IsVisible = false;
        }

        [RelayCommand]
        private void OpenTermsAndConditions()
        {
            ProcessLauncher.StartShellProcess(TermsAndConditionsUrl);
        }

        [RelayCommand]
        private void OpenPrivacyPolicy()
        {
            ProcessLauncher.StartShellProcess(PrivacyPolicyUrl);
        }
    }
}

