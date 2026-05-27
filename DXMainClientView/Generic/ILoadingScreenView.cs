#nullable enable

namespace DXMainClientView;

public interface ILoadingScreenView
{
    void Show();
    void Hide();
    void SetVisible(bool visible);
    void SetStatusText(string statusText);
    void SetBackgroundImage(string imagePath);
}
