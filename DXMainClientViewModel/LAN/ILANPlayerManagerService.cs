#nullable enable
using System.Collections.Generic;

namespace DXMainClientViewModel;

public interface ILANPlayerManagerService
{
    IReadOnlyList<string> PlayerNames { get; }

    void AddOrUpdatePlayer(string playerName, string address);
    void RemovePlayer(string playerName);
    bool ContainsPlayer(string playerName);
    void ClearPlayers();
}
