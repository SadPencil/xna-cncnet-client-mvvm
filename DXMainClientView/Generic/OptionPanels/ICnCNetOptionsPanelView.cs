#nullable enable

using System.Collections.Generic;

namespace DXMainClientView;

public interface ICnCNetOptionsPanelView
{
    void SetPingUnofficialTunnels(bool enabled);
    void SetWritePathToRegistry(bool enabled);
    void SetUserListNotifications(bool enabled);
    void SetPrivateMessagePopupsEnabled(bool enabled);
    void SetMainMenuHotkeysEnabled(bool enabled);
    void SetSkipLoginWindow(bool enabled);
    void SetPersistentMode(bool enabled);
    void SetConnectOnStartup(bool enabled);
    void SetDiscordIntegration(bool enabled);
    void SetSteamIntegration(bool enabled);
    void SetPrivateMessageRuleOptions(IEnumerable<string> options);
    void SetSelectedPrivateMessageRule(string option);
    void SetFollowedGames(IEnumerable<string> followedGames);
}
