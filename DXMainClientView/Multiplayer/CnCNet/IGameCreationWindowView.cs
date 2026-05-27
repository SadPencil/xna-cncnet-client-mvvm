#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IGameCreationWindowView
{
    event Action? CreateRequested;
    event Action? CancelRequested;
    event Action? TunnelSelectionRequested;

    void Show();
    void Hide();
    void SetGameName(string gameName);
    void SetMaxPlayerOptions(IEnumerable<string> options);
    void SetSelectedMaxPlayers(string selectedOption);
    void SetSkillLevelOptions(IEnumerable<string> options);
    void SetSelectedSkillLevel(string selectedOption);
    void SetPasswordRequired(bool required);
    void SetTunnelName(string tunnelName);
    void SetCreateEnabled(bool enabled);
}
