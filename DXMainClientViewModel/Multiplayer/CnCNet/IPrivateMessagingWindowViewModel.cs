using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface IPrivateMessagingWindowViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> ConversationNames { get; }
    int SelectedConversationIndex { get; set; }
    string? SelectedConversationName { get; }
    IReadOnlyList<string> MessageHistory { get; }
    string DraftMessage { get; set; }

    IRelayCommand SendMessageCommand { get; }
    IRelayCommand CloseCommand { get; }
    IRelayCommand RefreshConversationsCommand { get; }

    void Initialize();
}
