#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IGameLoadingWindowView
{
    event Action<int>? SaveSelectionChanged;
    event Action? LoadRequested;
    event Action? DeleteRequested;
    event Action? CloseRequested;

    void Show();
    void Hide();
    void SetSavedGames(IEnumerable<IReadOnlyList<string>> savedGames);
    void SetSelectedSaveIndex(int selectedIndex);
    void SetLoadEnabled(bool enabled);
    void SetDeleteEnabled(bool enabled);
    void RefreshSavedGameList();
}
