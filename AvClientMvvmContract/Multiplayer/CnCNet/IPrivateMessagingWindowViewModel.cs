using System.Collections.Generic;
using System.ComponentModel;

using AvClientMvvmContract.Online;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

public interface IPrivateMessagingWindowViewModel : INotifyPropertyChanged
{
    int SelectedTabIndex { get; set; }
    IReadOnlyList<string> UserNames { get; }
    int SelectedUserIndex { get; set; }
    bool IsMessageInputEnabled { get; }
    IReadOnlyList<IChatMessage> MessageHistory { get; }
    string DraftMessage { get; set; }
    bool IsNotificationVisible { get; set; }
    string NotificationSender { get; }
    string NotificationMessage { get; }
    string PlayersLabelText { get; }
    bool IsRecentPlayersVisible { get; }
    bool IsMessagesPanelEnabled { get; }
    IReadOnlyList<string> RecentPlayerNames { get; }
    bool IsVisible { get; set; }
    int SelectedMessageIndex { get; set; }
    int SelectedRecentPlayerIndex { get; set; }
    string? PendingLink { get; set; }

    IRelayCommand SendMessageCommand { get; }
    IRelayCommand CloseCommand { get; }
    IRelayCommand SwitchOnCommand { get; }
    IRelayCommand RefreshConversationsCommand { get; }
    IRelayCommand ToggleSelectedUserFriendCommand { get; }
    IRelayCommand ToggleSelectedUserIgnoreCommand { get; }
    IRelayCommand JoinSelectedUserGameCommand { get; }
    IRelayCommand InviteSelectedUserToGameCommand { get; }
    IRelayCommand OpenSelectedMessageSenderPrivateMessageCommand { get; }
    IRelayCommand ToggleSelectedMessageSenderFriendCommand { get; }
    IRelayCommand ToggleSelectedMessageSenderIgnoreCommand { get; }
    IRelayCommand JoinSelectedMessageSenderGameCommand { get; }
    IRelayCommand OpenPendingLinkCommand { get; }
    IRelayCommand CopyPendingLinkCommand { get; }
    IRelayCommand OpenSelectedRecentPlayerPrivateMessageCommand { get; }
    IRelayCommand ToggleSelectedRecentPlayerFriendCommand { get; }
    IRelayCommand ToggleSelectedRecentPlayerIgnoreCommand { get; }
    IRelayCommand JoinSelectedRecentPlayerGameCommand { get; }
}
