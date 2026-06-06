using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientViewModel.Domain.Multiplayer;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Represents a single item in the map list for display.
/// </summary>
public partial class MapListItem : ObservableObject, IMapListItem
{
    /// <summary>
    /// The source GameModeMap this item represents.
    /// </summary>
    public GameModeMap Source { get; init; }

    [ObservableProperty]
    public partial int RankIndex { get; set; }

    [ObservableProperty]
    public partial string MapName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GameModeName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDisabled { get; set; }

    [ObservableProperty]
    public partial string? RankTexturePath { get; set; }
}
