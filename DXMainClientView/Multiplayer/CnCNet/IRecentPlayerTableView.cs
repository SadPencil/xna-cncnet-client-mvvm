#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IRecentPlayerTableView
{
    event Action<int>? PlayerRightClicked;
    event Action<int>? SelectionChanged;

    void SetPlayers(IEnumerable<IReadOnlyList<string>> players);
    void ClearPlayers();
    void SetSelectedPlayerIndex(int selectedIndex);
    void RefreshPlayers();
    void ShowContextMenuForSelection();
}
