using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;
using DXMainClientViewModel.Domain.Multiplayer;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

public interface IGameLobbyViewModel : INotifyPropertyChanged
{
    // --- Map info ---
    string MapName { get; }
    string MapAuthor { get; }
    string GameModeName { get; }
    string MapSize { get; }
    string GameName { get; }

    // --- Player summary (for compatibility) ---
    IReadOnlyList<string> PlayerNames { get; }
    int SelectedPlayerIndex { get; set; }

    // --- Map list ---
    IReadOnlyList<MapListItem> MapListItems { get; }
    int SelectedMapIndex { get; set; }
    IReadOnlyList<string> GameModeFilterOptions { get; }
    int SelectedGameModeFilterIndex { get; set; }
    string MapSearchText { get; set; }
    string MapListTooltipText { get; }
    int SortDirectionState { get; set; }
    bool IsMapSortButtonVisible { get; }
    bool IsMapSortButtonEnabled { get; }

    // --- Player slots (8 slots) ---
    IReadOnlyList<PlayerSlotObservable> PlayerSlots { get; }

    // --- Game options ---
    IReadOnlyList<GameOptionCheckBox> CheckBoxes { get; }
    IReadOnlyList<GameOptionDropDown> DropDowns { get; }

    // --- Launch state ---
    int LaunchButtonRank { get; }
    string LaunchButtonText { get; }
    bool IsHost { get; }
    bool CanLaunchGame { get; }

    // --- Map list hover ---
    void SetHoveredMapIndex(int hoveredIndex);

    // --- Commands ---
    IRelayCommand LeaveGameCommand { get; }
    IRelayCommand LaunchGameCommand { get; }
    IRelayCommand PickRandomMapCommand { get; }
    IRelayCommand SaveGameOptionPresetCommand { get; }
    IRelayCommand LoadGameOptionPresetCommand { get; }
    IRelayCommand ToggleFavoriteCommand { get; }
    IRelayCommand DeleteMapCommand { get; }
    IRelayCommand ShowMapInFolderCommand { get; }
    IRelayCommand CycleSortDirectionCommand { get; }
    IRelayCommand ToggleSearchAllModesCommand { get; }
    IRelayCommand OpenGameSettingsCommand { get; }
    IRelayCommand OpenMapSelectionCommand { get; }

    // --- Lifecycle ---
    void Initialize();
}
