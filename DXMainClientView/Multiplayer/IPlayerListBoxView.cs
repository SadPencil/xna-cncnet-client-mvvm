#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IPlayerListBoxView
{
    event Action<int>? SelectedPlayerChanged;

    void SetPlayers(IEnumerable<IReadOnlyList<string>> players);
    void AddPlayer(IReadOnlyList<string> playerRow);
    void UpdatePlayer(int playerIndex, IReadOnlyList<string> playerRow);
    void RemovePlayer(int playerIndex);
    void ClearPlayers();
    void SetSelectedPlayerIndex(int selectedIndex);
    void RefreshPlayers();
}
