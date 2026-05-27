#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IChoiceNotificationBoxViewModel
{
    string TitleText { get; }
    string SenderName { get; }
    string MessageText { get; }
    string AcceptButtonText { get; }
    string DeclineButtonText { get; }
    bool IsVisible { get; }

    IRelayCommand AcceptCommand { get; }
    IRelayCommand DeclineCommand { get; }
}
