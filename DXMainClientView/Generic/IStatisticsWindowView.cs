#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IStatisticsWindowView
{
    event Action<int>? TabChanged;
    event Action<string>? GameModeFilterChanged;
    event Action<string>? GameClassFilterChanged;
    event Action<bool>? IncludeSpectatedGamesChanged;
    event Action<int>? GameSelectionChanged;
    event Action? ClearStatisticsRequested;
    event Action? CloseRequested;

    void Show();
    void Hide();
    void SetSelectedTab(int tabIndex);
    void SetGameModeOptions(IEnumerable<string> gameModes);
    void SetGameClassOptions(IEnumerable<string> gameClasses);
    void SetIncludeSpectatedGames(bool includeSpectatedGames);
    void SetGameRows(IEnumerable<IReadOnlyList<string>> games);
    void SetGameStatisticRows(IEnumerable<IReadOnlyList<string>> statistics);
    void SetTotalStatistics(IReadOnlyDictionary<string, string> totals);
}
