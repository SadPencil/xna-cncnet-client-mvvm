using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

public interface IPrivateMessagingWindowViewModel : INotifyPropertyChanged
{
    int SelectedTabIndex { get; set; }
    IReadOnlyList<string> UserNames { get; }
    int SelectedUserIndex { get; set; }
    bool IsMessageInputEnabled { get; }
    IReadOnlyList<string> MessageHistory { get; }
    string DraftMessage { get; set; }
    bool IsNotificationVisible { get; set; }
    string NotificationSender { get; }
    string NotificationMessage { get; }
    string PlayersLabelText { get; }
    bool IsRecentPlayersVisible { get; }
    bool IsMessagesPanelEnabled { get; }
    IReadOnlyList<string> RecentPlayerNames { get; }
    bool IsVisible { get; set; }

    IRelayCommand SendMessageCommand { get; }
    IRelayCommand CloseCommand { get; }
    IRelayCommand SwitchOnCommand { get; }
    IRelayCommand RefreshConversationsCommand { get; }
    IRelayCommand ToggleSelectedUserFriendCommand { get; }
    IRelayCommand ToggleSelectedUserIgnoreCommand { get; }
    IRelayCommand JoinSelectedUserGameCommand { get; }
    IRelayCommand InviteSelectedUserToGameCommand { get; }
}
