#nullable enable

using System;

namespace DXMainClientView;

public interface ITopBarView
{
    event Action? MainViewRequested;
    event Action? SecondaryViewRequested;
    event Action? PrivateMessagesRequested;
    event Action? OptionsRequested;
    event Action? LogoutRequested;

    void SetMainButtonText(string text);
    void SetSecondaryButtonText(string text);
    void SetPrivateMessagesButtonText(string text);
    void SetConnectionStatusText(string text);
    void SetPlayerCountText(string text);
    void SetDateText(string text);
    void SetTimeText(string text);
    void SetSwitchButtonsEnabled(bool enabled);
    void SetOptionsButtonEnabled(bool enabled);
    void SetLogoutButtonEnabled(bool enabled);
}
