using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AvClientMvvmContract.Domain.Multiplayer;

using Rampastring.Tools;

namespace AvClientViewModel.Multiplayer.GameLobby
{
    public record GameSessionSetting : IGameSessionSetting
    {
        public string Name { get; set; }

        public int Value { get; set; }

        // TODO: read the old client's code and implement these properly in an MVVM way.
        public bool AffectsSpawnIni { get; } = false;
        public bool AffectsMapCode { get; } = false;
        public bool AllowScoring { get; } = false;
        public bool BroadcastToLobby { get; } = false;
        public void ApplySpawnIniCode(IniFile spawnIni) { }
        public void ApplyMapCode(IniFile mapIni, IGameMode gameMode) { }
        public void ApplyDisallowedSideIndex(bool[] disallowedArray) { }
    }
}
