using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Multiplayer;

public interface IChatListBoxViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> Messages { get; }
    string DraftMessage { get; set; }
    bool IsAutoScrollEnabled { get; set; }

    string? PendingUntrustedUrl { get; }
    bool IsUntrustedUrlDialogVisible { get; set; }

    IRelayCommand SendMessageCommand { get; }
    IRelayCommand ClearMessagesCommand { get; }
    // TODO: investigate whether OpenLinkCommand is properly implemented in view model. The view model should call the Url service to open it. Besides, I don't remember IRelayCommand having a generic version.
    IRelayCommand<string> OpenLinkCommand { get; }
    IRelayCommand ConfirmOpenUrlCommand { get; }
    IRelayCommand CancelOpenUrlCommand { get; }
}
