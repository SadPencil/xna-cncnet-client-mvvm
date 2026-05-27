#nullable enable
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IPrivateMessageNotificationBoxViewModel : INotifyPropertyChanged
{
    string SenderName { get; }
    string MessagePreview { get; }
    bool IsVisible { get; }

    IRelayCommand OpenConversationCommand { get; }
    IRelayCommand DismissCommand { get; }
}
