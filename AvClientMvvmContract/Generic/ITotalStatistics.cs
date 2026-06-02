using System;

namespace AvClientMvvmContract.Generic;

/// <summary>
/// Read-only view of total statistics across all games.
/// </summary>
public interface ITotalStatistics
{
    int GamesStarted { get; }
    int GamesFinished { get; }
    int Wins { get; }
    int Losses { get; }
    double WinLossRatio { get; }
    TimeSpan AverageGameLength { get; }
    TimeSpan TotalTimePlayed { get; }
    double AverageEnemyCount { get; }
    double AverageAllyCount { get; }
    int TotalKills { get; }
    int KillsPerGame { get; }
    int TotalLosses { get; }
    int LossesPerGame { get; }
    double KillLossRatio { get; }
    int TotalScore { get; }
    int AverageEconomy { get; }
    string FavouriteSide { get; }
    string AverageAILevel { get; }
}
