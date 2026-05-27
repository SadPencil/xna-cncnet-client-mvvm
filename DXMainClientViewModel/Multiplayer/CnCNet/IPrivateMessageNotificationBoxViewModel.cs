#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IPrivateMessageNotificationBoxViewModel
{
    string SenderName { get; }
    string MessagePreview { get; }
    bool IsVisible { get; }

    IRelayCommand OpenConversationCommand { get; }
    IRelayCommand DismissCommand { get; }
}
