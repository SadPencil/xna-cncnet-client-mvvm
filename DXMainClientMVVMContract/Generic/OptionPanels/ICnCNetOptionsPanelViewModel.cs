using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMVVMContract.Generic.OptionPanels;

public interface ICnCNetOptionsPanelViewModel : INotifyPropertyChanged
{
    bool PingUnofficialTunnels { get; set; }
    bool WriteInstallationPathToRegistry { get; set; }
    bool DisableMainMenuHotkeys { get; set; }
    bool NotifyOnUserListChanges { get; set; }
    bool DisablePrivateMessagePopups { get; set; }
    int AllowPrivateMessagesMode { get; set; }
    bool SkipLoginDialog { get; set; }
    bool PersistentMode { get; set; }
    bool AutoConnectOnStartup { get; set; }
    bool IsDiscordIntegrationEnabled { get; set; }
    bool IsSteamIntegrationEnabled { get; set; }
    bool AllowGameInvitesOnlyFromFriends { get; set; }
    bool IsAutoConnectOnStartupAllowed { get; }
    bool IsDiscordIntegrationGloballyDisabled { get; }
    IReadOnlyList<string> FollowedGameNames { get; }
    IReadOnlyList<IGameListItemData> GameListItems { get; }

    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
    IRelayCommand<string> ToggleGameFollowedCommand { get; }
}
