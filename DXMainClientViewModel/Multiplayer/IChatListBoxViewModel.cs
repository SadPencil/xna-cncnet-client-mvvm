#nullable enable
using System.ComponentModel;
using System.Collections.Generic;

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
