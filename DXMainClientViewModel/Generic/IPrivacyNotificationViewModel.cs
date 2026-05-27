#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IPrivacyNotificationViewModel
{
    string TitleText { get; }
    string DescriptionText { get; }
    string TermsAndConditionsUrl { get; }
    string PrivacyPolicyUrl { get; }

    IRelayCommand AcceptCommand { get; }
    IRelayCommand OpenTermsAndConditionsCommand { get; }
    IRelayCommand OpenPrivacyPolicyCommand { get; }
}
