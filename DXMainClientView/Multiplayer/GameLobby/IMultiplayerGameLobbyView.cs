#nullable enable

namespace DXMainClientView;

public interface IMultiplayerGameLobbyView : IGameLobbyView
{
    void AddChatMessage(string senderName, string message, string colorHex);
    void ClearChat();
    void SetReadyChecked(bool ready);
    void SetReadyEnabled(bool enabled);
    void SetLockButtonText(string text);
    void SetLockEnabled(bool enabled);
    void SetBroadcastStatusText(string statusText);
}
