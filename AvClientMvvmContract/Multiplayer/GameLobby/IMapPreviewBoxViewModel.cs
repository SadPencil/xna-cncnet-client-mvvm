using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using SixLabors.ImageSharp;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

public interface IMapPreviewBoxViewModel : INotifyPropertyChanged
{
    // Observable properties
    string SelectedMapName { get; }
    string SelectedGameModeName { get; }
    string MapAuthorName { get; }
    string MapSizeText { get; }
    Image? MapPreviewImage { get; }
    int SelectedStartingLocationIndex { get; set; }
    int SelectedPlayerIndex { get; set; }
    bool IsFavorite { get; }
    bool ShowExtraTextures { get; }
    bool EnableContextMenu { get; set; }
    bool EnableStartLocationSelection { get; set; }

    // Observable collections
    IReadOnlyList<string> StartingLocationSummaries { get; }
    IReadOnlyList<IStartingLocationIndicatorData> StartingLocationIndicators { get; }

    // Commands
    IRelayCommand SelectStartingLocationCommand { get; }
    IRelayCommand AssignStartingLocationCommand { get; }
    IRelayCommand ClearStartingLocationCommand { get; }
    IRelayCommand ToggleFavoriteCommand { get; }
    IRelayCommand ToggleExtraTexturesCommand { get; }
    IRelayCommand ShowInFolderCommand { get; }
    IRelayCommand RefreshPreviewCommand { get; }
}
