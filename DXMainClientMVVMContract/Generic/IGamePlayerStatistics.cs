namespace DXMainClientViewModel.Generic;

/// <summary>
/// Read-only view of a player's statistics in a game.
/// </summary>
public interface IGamePlayerStatistics
{
    string Name { get; }
    int Kills { get; }
    int Losses { get; }
    int Economy { get; }
    int Score { get; }
    bool Won { get; }
    bool WasSpectator { get; }
    bool IsAI { get; }
    int Side { get; }
    int Team { get; }
    string SideName { get; }
    int ColorR { get; }
    int ColorG { get; }
    int ColorB { get; }
    bool SawCompletion { get; }
}
