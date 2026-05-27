#nullable enable

using System;

namespace DXMainClientView;

public interface IPrivacyNotificationView
{
    event Action? Accepted;

    void Show();
    void Hide();
    void SetVisible(bool visible);
    void SetMessage(string message);
    void SetAcceptEnabled(bool enabled);
}
