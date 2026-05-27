#nullable enable

namespace DXMainClientView;

/// <summary>
/// Base interface for views that can be switched on/off by the top bar.
/// </summary>
public interface ISwitchableView
{
    void Show();
    void Hide();
    string GetDisplayName();
}
