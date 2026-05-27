#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface ICnCNetLobbyView : ISwitchableView
{
    event Action? CreateGameRequested;
    event Action? JoinGameRequested;
    event Action? LogoutRequested;

    void SetCurrentChannel(string channelName);
    void SetChannelOptions(IEnumerable<string> channels);
    void SetSelectedChannel(string channelName);
    void SetPlayersOnlineText(string onlineText);
    void SetChatColorOptions(IEnumerable<string> colors);
    void SetSelectedChatColor(string colorName);
    void SetGameSearchText(string searchText);
    void SetCreateGameEnabled(bool enabled);
    void SetJoinGameEnabled(bool enabled);
    void ShowGameFilters(bool visible);
    void ShowGameCreationPanel();
    void ShowPasswordRequestWindow();
}
