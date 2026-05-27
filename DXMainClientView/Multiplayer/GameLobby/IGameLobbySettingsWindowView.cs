#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IGameLobbySettingsWindowView
{
    event Action? SaveRequested;
    event Action? CancelRequested;

    void Show();
    void Hide();
    void SetRoomName(string roomName);
    void SetMaxPlayerOptions(IEnumerable<string> options);
    void SetSelectedMaxPlayers(string selectedOption);
    void SetSkillLevelOptions(IEnumerable<string> options);
    void SetSelectedSkillLevel(string selectedOption);
    void SetPassword(string password);
    void SetSaveEnabled(bool enabled);
}
