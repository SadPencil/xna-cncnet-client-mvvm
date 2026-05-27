using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

public interface IMapPreviewBoxViewModel : INotifyPropertyChanged
{
    string SelectedMapName { get; }
    string SelectedGameModeName { get; }
    string MapAuthorName { get; }
    string MapSizeText { get; }
    IReadOnlyList<string> StartingLocationSummaries { get; }
    int SelectedStartingLocationIndex { get; set; }

    IRelayCommand SelectStartingLocationCommand { get; }
    IRelayCommand RefreshPreviewCommand { get; }
}
