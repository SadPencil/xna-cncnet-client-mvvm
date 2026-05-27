#nullable enable

using System;

namespace DXMainClientView;

public interface IGlobalContextMenuView
{
    event Action? PrivateMessageRequested;
    event Action? FriendToggleRequested;
    event Action? IgnoreToggleRequested;
    event Action? InviteRequested;
    event Action? JoinRequested;

    void ShowAt(int x, int y);
    void Hide();
    void SetTargetUserName(string userName);
    void SetFriendActionText(string text);
    void SetIgnoreActionText(string text);
    void SetInviteEnabled(bool enabled);
    void SetJoinEnabled(bool enabled);
}
