using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

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
    bool IsWindowVisible { get; set; }

    IRelayCommand SendMessageCommand { get; }
    IRelayCommand CloseCommand { get; }
    IRelayCommand SwitchOnCommand { get; }
    IRelayCommand RefreshConversationsCommand { get; }

    void Initialize();
    void InitPM(string name);
    void SetInviteChannelInfo(string channelName, string gameName, string channelPassword);
    void ClearInviteChannelInfo();
}
