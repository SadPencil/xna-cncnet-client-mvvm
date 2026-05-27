#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IGameListBoxView
{
    event Action<int>? SelectedGameChanged;

    void SetGames(IEnumerable<IReadOnlyList<string>> games);
    void AddGame(IReadOnlyList<string> gameRow);
    void RemoveGame(int gameIndex);
    void ClearGames();
    void SetSelectedGameIndex(int selectedIndex);
    void SetSortDirection(string sortDirection);
    void ShowToolTip(string toolTipText);
    void RefreshList();
}
