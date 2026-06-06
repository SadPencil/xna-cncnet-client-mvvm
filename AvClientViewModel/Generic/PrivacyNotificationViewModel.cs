using AvClientMvvmContract.Generic;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the privacy notification.
    /// Handles privacy policy acceptance.
    /// </summary>
    public partial class PrivacyNotificationViewModel : ObservableObject, IPrivacyNotificationViewModel
    {
        [ObservableProperty]
        public partial string TitleText { get; set; } = "Privacy Policy";

        [ObservableProperty]
        public partial string DescriptionText { get; set; } = "This application makes use of CnCNet web & tunnel server services and is subject to collection of technical & other necessary information through them.";

        [ObservableProperty]
        public partial string ExplanationText { get; set; } = "By using this application you agree to the CnCNet Terms & Conditions as well as the CnCNet Privacy Policy. Privacy-related options can be configured in the client settings.";

        [ObservableProperty]
        public partial string AcceptButtonText { get; set; } = "Got it";

        [ObservableProperty]
        public partial string TermsAndConditionsUrl { get; set; } = "https://cncnet.org/terms-and-conditions";

        [ObservableProperty]
        public partial string PrivacyPolicyUrl { get; set; } = "https://cncnet.org/privacy-policy";

        [ObservableProperty]
        public partial bool IsVisible { get; set; }

        public PrivacyNotificationViewModel()
        {
            DescriptionText = "This application makes use of CnCNet web & tunnel server services and is subject to collection of technical & other necessary information through them.".L10N("Client:Main:TOSText");
            ExplanationText = "By using this application you agree to the CnCNet Terms & Conditions as well as the CnCNet Privacy Policy. Privacy-related options can be configured in the client settings.".L10N("Client:Main:TOSExplanation");
            AcceptButtonText = "Got it".L10N("Client:Main:TOSButtonOK");
            IsVisible = !UserINISettings.Instance.PrivacyPolicyAccepted;
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


