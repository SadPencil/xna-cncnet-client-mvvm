#nullable enable

using System;

namespace DXMainClientView;

public interface IOptionsWindowView
{
    event Action? SaveRequested;
    event Action? CancelRequested;
    event Action<int>? TabChanged;
    event Action? ForceUpdateRequested;

    void Show();
    void Hide();
    void SetSelectedTab(int tabIndex);
    void SetTabEnabled(int tabIndex, bool enabled);
    void RefreshAllPanels();
    void ShowRestartRequiredMessage(string message);
    void ShowSettingsAdjustedMessage(string message);
    void ShowDownloadCancellationPrompt(string message);
}
