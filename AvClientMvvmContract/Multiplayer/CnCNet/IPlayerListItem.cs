namespace AvClientMvvmContract.Multiplayer.CnCNet;

/// <summary>
/// Read-only view of a player in the CnCNet lobby player list.
/// Provides enough information for the View to render admin/friend/ignore/voice icons.
/// </summary>
public interface IPlayerListItem
{
    string Name { get; }
    bool IsAdmin { get; }
    bool IsFriend { get; }
    bool IsIgnored { get; }
    bool HasVoice { get; }
    int GameId { get; }
}
