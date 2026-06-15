using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

using AvClientMvvmContract.ViewServices;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

public interface IGameLobbyViewModel : INotifyPropertyChanged
{
    // --- Map info ---
    string MapName { get; }
    string MapRawName { get; }
    string MapOriginalName { get; }
    bool HasMapOriginalName { get; }
    string MapAuthor { get; }
    string GameModeName { get; }
    string MapSize { get; }
    string GameName { get; }
    IMapPreviewBoxViewModel MapPreviewBox { get; }

    // --- Player summary (for compatibility) ---
    IReadOnlyList<string> PlayerNames { get; }
    int SelectedPlayerIndex { get; set; }

    // --- Map list ---
    IReadOnlyList<IMapListItem> MapListItems { get; }
    int SelectedMapIndex { get; set; }
    IReadOnlyList<string> GameModeFilterOptions { get; }
    int SelectedGameModeFilterIndex { get; set; }
    string MapSearchText { get; set; }
    string? MapListTooltipText { get; }
    int SortDirectionState { get; set; }
    bool IsMapSortButtonVisible { get; }
    bool IsMapSortButtonEnabled { get; }

    // --- Player slots (8 slots) ---
    ObservableCollection<IPlayerSlotObservable> PlayerSlots { get; }

    // --- Game options ---
    IReadOnlyList<IGameOptionCheckBox> CheckBoxes { get; }
    IReadOnlyList<IGameOptionDropDown> DropDowns { get; }

    // --- Launch state ---
    int LaunchButtonRank { get; }
    string LaunchButtonText { get; }
    bool IsHost { get; }
    bool CanLaunchGame { get; }

    // --- Map list hover ---
    IRelayCommand<int> SetHoveredMapIndexCommand { get; }

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

    IReadOnlyList<IContextMenuItem> StartingLocationAssignMenuItems { get; }
}
