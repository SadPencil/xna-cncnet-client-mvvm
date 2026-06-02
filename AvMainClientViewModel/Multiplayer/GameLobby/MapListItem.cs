using AvMainClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

using AvMainClientViewModel.Domain.Multiplayer;

namespace AvMainClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Represents a single item in the map list for display.
/// </summary>
public class MapListItem : ObservableObject, IMapListItem
{
    /// <summary>
    /// The source GameModeMap this item represents.
    /// </summary>
    public GameModeMap Source { get; init; }

    private int _rankIndex;
    public int RankIndex
    {
        get => _rankIndex;
        set => SetProperty(ref _rankIndex, value);
    }

    private string _mapName = string.Empty;
    public string MapName
    {
        get => _mapName;
        set => SetProperty(ref _mapName, value);
    }

    private string _gameModeName = string.Empty;
    public string GameModeName
    {
        get => _gameModeName;
        set => SetProperty(ref _gameModeName, value);
    }

    private bool _isDisabled;
    public bool IsDisabled
    {
        get => _isDisabled;
        set => SetProperty(ref _isDisabled, value);
    }
}
