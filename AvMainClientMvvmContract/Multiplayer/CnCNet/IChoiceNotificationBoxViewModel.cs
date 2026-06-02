using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Multiplayer.CnCNet;

public interface IChoiceNotificationBoxViewModel : INotifyPropertyChanged
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
