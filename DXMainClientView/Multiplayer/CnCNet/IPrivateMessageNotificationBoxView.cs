#nullable enable

using System;

namespace DXMainClientView;

public interface IPrivateMessageNotificationBoxView
{
    event Action? NotificationClicked;

    void ShowNotification(string senderName, string previewText);
    void Hide();
    void SetUnreadCount(int unreadCount);
    void SetFlashEnabled(bool enabled);
    void ClearNotification();
}
