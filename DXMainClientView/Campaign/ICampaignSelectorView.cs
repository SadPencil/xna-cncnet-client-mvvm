#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface ICampaignSelectorView
{
    event Action? LaunchRequested;
    event Action? CloseRequested;
    event Action? ReturnRequested;
    event Action<int>? MissionSelectionChanged;
    event Action<int>? DifficultyChanged;

    void Show();
    void Hide();
    void SetMissionNames(IEnumerable<string> missionNames);
    void SetSelectedMissionIndex(int selectedIndex);
    void SetMissionDescription(string description);
    void SetMissionPreviewPath(string previewPath);
    void ClearMissionPreview();
    void SetDifficultyNames(IEnumerable<string> difficultyNames);
    void SetSelectedDifficulty(int difficultyIndex);
    void SetLaunchEnabled(bool enabled);
    void SetReturnVisible(bool visible);
}
