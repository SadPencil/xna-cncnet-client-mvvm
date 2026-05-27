#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IGameFiltersPanelView
{
    event Action? FiltersChanged;

    void Show();
    void Hide();
    void SetFriendsOnly(bool enabled);
    void SetHideLockedGames(bool enabled);
    void SetHidePasswordedGames(bool enabled);
    void SetHideIncompatibleGames(bool enabled);
    void SetGameModeOptions(IEnumerable<string> options);
    void SetSelectedGameMode(string option);
    void SetSearchText(string searchText);
    void ResetFilters();
}
