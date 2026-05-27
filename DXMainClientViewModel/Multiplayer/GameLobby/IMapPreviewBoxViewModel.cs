#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IMapPreviewBoxViewModel
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
