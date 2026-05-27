#nullable enable
using System.ComponentModel;
using System.Collections.Generic;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic.OptionPanels;

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
    IReadOnlyList<string> FollowedGameNames { get; }

    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
}
