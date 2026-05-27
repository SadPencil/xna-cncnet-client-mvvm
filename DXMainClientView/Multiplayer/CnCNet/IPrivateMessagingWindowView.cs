#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IPrivateMessagingWindowView : ISwitchableView
{
    event Action? MessageSubmitted;
    event Action<int>? UserSelected;
    event Action<int>? TabChanged;

    void SetSelectedTab(int tabIndex);
    void SetConversationTitle(string title);
    void SetUserListTitle(string title);
    void SetUsers(IEnumerable<string> users);
    void SetRecentPlayers(IEnumerable<IReadOnlyList<string>> recentPlayers);
    void AddMessage(string senderName, string message, string colorHex);
    void ClearConversation();
    void SetMessageInput(string message);
    void SetMessageInputEnabled(bool enabled);
    void ShowNotificationPreview(string senderName, string previewText);
}
