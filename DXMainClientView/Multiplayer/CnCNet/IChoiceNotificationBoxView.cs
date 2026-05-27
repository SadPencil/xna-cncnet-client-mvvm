using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IChoiceNotificationBoxView
{
    IChoiceNotificationBoxViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
