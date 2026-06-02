using System.Collections.Generic;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Data for a single starting location indicator on the map preview.
/// </summary>
public interface IStartingLocationIndicatorData
{
    int WaypointNumber { get; }
    double X { get; }
    double Y { get; }
    bool IsVisible { get; }
    bool IsOccupied { get; }
    IRgb24Color TintColor { get; }
    IReadOnlyList<IIndicatorPlayerInfo> Players { get; }
}

/// <summary>
/// Player info displayed on a starting location indicator.
/// </summary>
public interface IIndicatorPlayerInfo
{
    string Name { get; }
    int TeamId { get; }
    IRgb24Color Color { get; }
}
