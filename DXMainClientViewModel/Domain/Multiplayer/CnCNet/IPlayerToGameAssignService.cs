#nullable enable
using System.Collections.Generic;

namespace DXMainClientViewModel.Domain.Multiplayer.CnCNet;

public interface IPlayerToGameAssignService
{
    IReadOnlyDictionary<string, string> PlayerGameAssignments { get; }

    void AssignPlayer(string playerName, string gameIdentifier);
    void UnassignPlayer(string playerName);
    IReadOnlyList<string> GetPlayersInGame(string gameIdentifier);
    void ClearAssignments();
}
