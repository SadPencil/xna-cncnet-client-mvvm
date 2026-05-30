#nullable enable
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer
{
    public interface IReadOnlyGameModeMapCollection : IReadOnlyList<GameModeMap> // checked
    {
        public IReadOnlyList<GameMode> GameModes { get; }
        public Map? FindMapByHash(string mapHash);
    }
}
