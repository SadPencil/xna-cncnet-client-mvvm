using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer.GameLobby;

public interface IMapPreviewBoxViewModel : INotifyPropertyChanged
{
    // Observable properties
    string SelectedMapName { get; }
    string SelectedGameModeName { get; }
    string MapAuthorName { get; }
    string MapSizeText { get; }
    int SelectedStartingLocationIndex { get; set; }
    int SelectedPlayerIndex { get; set; }
    bool IsFavorite { get; }
    bool ShowExtraTextures { get; }
    bool EnableContextMenu { get; set; }
    bool EnableStartLocationSelection { get; set; }

    // Observable collections
    IReadOnlyList<string> StartingLocationSummaries { get; }

    // Commands
    IRelayCommand SelectStartingLocationCommand { get; }
    IRelayCommand AssignStartingLocationCommand { get; }
    IRelayCommand ClearStartingLocationCommand { get; }
    IRelayCommand ToggleFavoriteCommand { get; }
    IRelayCommand ToggleExtraTexturesCommand { get; }
    IRelayCommand ShowInFolderCommand { get; }
    IRelayCommand RefreshPreviewCommand { get; }
}
