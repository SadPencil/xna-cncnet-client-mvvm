using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IPrivacyNotificationViewModel : INotifyPropertyChanged
{
    string TitleText { get; }
    string DescriptionText { get; }
    string TermsAndConditionsUrl { get; }
    string PrivacyPolicyUrl { get; }
    bool IsVisible { get; }

    IRelayCommand AcceptCommand { get; }
    IRelayCommand OpenTermsAndConditionsCommand { get; }
    IRelayCommand OpenPrivacyPolicyCommand { get; }
}
