#nullable enable

using System;

namespace DXMainClientView;

public interface IExtrasWindowView
{
    event Action? StatisticsRequested;
    event Action? MapEditorRequested;
    event Action? CreditsRequested;
    event Action? CloseRequested;

    void Show();
    void Hide();
    void SetStatisticsEnabled(bool enabled);
    void SetMapEditorEnabled(bool enabled);
    void SetCreditsEnabled(bool enabled);
}
