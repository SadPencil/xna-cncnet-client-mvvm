#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IChatListBoxViewModel
{
    IReadOnlyList<string> Messages { get; }
    string DraftMessage { get; set; }
    bool IsAutoScrollEnabled { get; set; }

    IRelayCommand SendMessageCommand { get; }
    IRelayCommand ClearMessagesCommand { get; }
    IRelayCommand ScrollToLatestCommand { get; }
}
