#nullable enable

using System;

namespace DXMainClientView;

public interface IChoiceNotificationBoxView
{
    event Action? AffirmativeRequested;
    event Action? NegativeRequested;

    void Show();
    void Hide();
    void SetHeaderText(string headerText);
    void SetSenderName(string senderName);
    void SetMessageText(string messageText);
    void SetAffirmativeText(string text);
    void SetNegativeText(string text);
    void SetTimeoutSeconds(int timeoutSeconds);
}
