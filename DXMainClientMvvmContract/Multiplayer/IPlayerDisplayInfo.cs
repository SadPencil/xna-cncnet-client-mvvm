using System.Drawing;

namespace DXMainClientMVVMContract.Multiplayer;

/// <summary>
/// Read-only view of display info for a player in the loading lobby.
/// </summary>
public interface IPlayerDisplayInfo
{
    string Name { get; }
    bool IsPresent { get; }
    bool IsReady { get; }
    Color Color { get; }
    string DisplayName { get; }
}
