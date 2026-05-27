#nullable enable

namespace DXMainClientView;

public interface IGameInProgressWindowView
{
    void Show();
    void Hide();
    void SetVisible(bool visible);
    void SetMessage(string message);
    void SetCursorHidden(bool hidden);
}
