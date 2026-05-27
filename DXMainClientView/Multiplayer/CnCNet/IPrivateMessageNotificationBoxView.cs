#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IPrivateMessageNotificationBoxView
{
    IPrivateMessageNotificationBoxViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
