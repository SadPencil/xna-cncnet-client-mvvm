#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IGameLobbyView : ISwitchableView
{
    event Action? LeaveRequested;
    event Action? LaunchRequested;
    event Action<string>? MapSelectionChanged;

    void SetMapName(string mapName);
    void SetMapAuthor(string author);
    void SetMapSize(string size);
    void SetGameModeName(string gameModeName);
    void SetAvailableMaps(IEnumerable<string> mapNames);
    void SetSelectedMap(string mapName);
    void SetMapSearchText(string searchText);
    void SetPlayerName(int playerIndex, string playerName);
    void SetPlayerSide(int playerIndex, string sideName);
    void SetPlayerColor(int playerIndex, string colorName);
    void SetPlayerTeam(int playerIndex, string teamName);
    void SetPlayerStartPosition(int playerIndex, string startPosition);
    void SetLaunchButtonText(string text);
    void SetLaunchEnabled(bool enabled);
}
