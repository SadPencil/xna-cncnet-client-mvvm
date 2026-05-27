using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer;

public interface IChatListBoxViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> Messages { get; }
    string DraftMessage { get; set; }
    bool IsAutoScrollEnabled { get; set; }

    IRelayCommand SendMessageCommand { get; }
    IRelayCommand ClearMessagesCommand { get; }
    IRelayCommand ScrollToLatestCommand { get; }
}
