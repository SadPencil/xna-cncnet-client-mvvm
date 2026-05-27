#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface ILANLobbyView : ISwitchableView
{
    event Action? NewGameRequested;
    event Action? JoinGameRequested;
    event Action? ExitRequested;

    void SetGames(IEnumerable<IReadOnlyList<string>> games);
    void SetPlayers(IEnumerable<IReadOnlyList<string>> players);
    void AddChatMessage(string senderName, string message, string colorHex);
    void SetChatInputEnabled(bool enabled);
    void SetNewGameEnabled(bool enabled);
    void SetJoinGameEnabled(bool enabled);
    void SetSelectedGameIndex(int selectedIndex);
    void SetSelectedPlayerIndex(int selectedIndex);
    void SetLocalAddressText(string localAddressText);
    void ShowGameCreationWindow();
    void ShowGameLoadingLobby();
}
