using System;
using System.Collections.Generic;
using System.Linq;

namespace AvMainClientViewModel.Domain.Multiplayer;

public class GameModeMapFilter
{
    public Func<List<GameModeMap>> GetGameModeMaps { get; }

    public GameModeMapFilter(Func<List<GameModeMap>> filterAction)
    {
        GetGameModeMaps = filterAction;
    }

    public bool Any() => GetGameModeMaps().Any();
}
