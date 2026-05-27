#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IGameLoadingLobbyView
{
    event Action? StartRequested;
    event Action? LeaveRequested;

    void Show();
    void Hide();
    void SetStatusText(string statusText);
    void SetMapName(string mapName);
    void SetGameModeName(string gameModeName);
    void SetPlayerNames(IEnumerable<string> playerNames);
    void SetPlayerReadyState(string playerName, bool ready);
    void AddChatMessage(string senderName, string message, string colorHex);
    void SetStartButtonText(string text);
    void SetStartButtonEnabled(bool enabled);
}
