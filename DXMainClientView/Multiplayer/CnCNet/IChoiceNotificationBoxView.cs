#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IChoiceNotificationBoxView
{
    IChoiceNotificationBoxViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
