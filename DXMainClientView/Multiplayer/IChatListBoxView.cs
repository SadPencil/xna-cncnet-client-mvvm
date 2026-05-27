#nullable enable

namespace DXMainClientView;

public interface IChatListBoxView
{
    void AddMessage(string senderName, string message, string colorHex);
    void AddSystemMessage(string message);
    void AddNotice(string message);
    void ClearMessages();
    void ScrollToBottom();
    void SetAutoScroll(bool enabled);
}
