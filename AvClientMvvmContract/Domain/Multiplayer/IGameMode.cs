namespace AvClientMvvmContract.Domain.Multiplayer;

/// <summary>
/// Read-only view of a game mode.
/// </summary>
public interface IGameMode
{
    string Name { get; }
    string UIName { get; }
}
