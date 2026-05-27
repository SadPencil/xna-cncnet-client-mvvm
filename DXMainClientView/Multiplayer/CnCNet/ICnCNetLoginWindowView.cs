#nullable enable

using System;

namespace DXMainClientView;

public interface ICnCNetLoginWindowView
{
    event Action? ConnectRequested;
    event Action? CancelRequested;

    void Show();
    void Hide();
    void SetPlayerName(string playerName);
    void SetRememberMe(bool rememberMe);
    void SetPersistentMode(bool enabled);
    void SetAutoConnect(bool enabled);
    void SetAutoConnectEnabled(bool enabled);
    void SetStatusText(string statusText);
    void ShowError(string errorMessage);
}
