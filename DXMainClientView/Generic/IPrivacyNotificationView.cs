#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IPrivacyNotificationView
{
    IPrivacyNotificationViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
