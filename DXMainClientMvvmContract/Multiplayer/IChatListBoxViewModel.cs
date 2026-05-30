using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer;

public interface IChatListBoxViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> Messages { get; }
    string DraftMessage { get; set; }
    bool IsAutoScrollEnabled { get; set; }

    string? PendingUntrustedUrl { get; }
    bool IsUntrustedUrlDialogVisible { get; set; }

    IRelayCommand SendMessageCommand { get; }
    IRelayCommand ClearMessagesCommand { get; }
    IRelayCommand<string> OpenLinkCommand { get; }
    IRelayCommand ConfirmOpenUrlCommand { get; }
    IRelayCommand CancelOpenUrlCommand { get; }
}
