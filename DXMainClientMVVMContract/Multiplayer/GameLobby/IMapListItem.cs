using System.ComponentModel;

namespace DXMainClientMVVMContract.Multiplayer.GameLobby;

/// <summary>
/// Read-only view of a map list item for display.
/// </summary>
public interface IMapListItem : INotifyPropertyChanged
{
    int RankIndex { get; }
    string MapName { get; }
    string GameModeName { get; }
    bool IsDisabled { get; }
}
