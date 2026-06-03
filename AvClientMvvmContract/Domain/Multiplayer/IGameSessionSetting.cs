using System.Collections.Generic;

using Rampastring.Tools;

namespace AvClientMvvmContract.Domain.Multiplayer;

/// <summary>
/// Represents a game session setting that can be broadcast to the lobby
/// and applied to spawn.ini and map code.
/// </summary>
public interface IGameSessionSetting
{
    /// <summary>Gets the name of this setting.</summary>
    string Name { get; }

    /// <summary>Indicates whether this setting can affect spawn.ini.</summary>
    bool AffectsSpawnIni { get; }

    /// <summary>Indicates whether this setting can affect map code.</summary>
    bool AffectsMapCode { get; }

    /// <summary>Indicates whether this setting in its current state allows the game to be scored.</summary>
    bool AllowScoring { get; }

    /// <summary>Indicates whether this setting should be broadcast to the lobby.</summary>
    bool BroadcastToLobby { get; }

    /// <summary>
    /// Gets or sets the value of this setting.
    /// For checkboxes: 0 = unchecked/off, 1 = checked/on.
    /// For dropdowns: the selected index.
    /// </summary>
    int Value { get; set; }

    /// <summary>Applies the associated code to the spawn.ini file.</summary>
    void ApplySpawnIniCode(IniFile spawnIni);

    /// <summary>Applies the associated code to the map INI file.</summary>
    void ApplyMapCode(IniFile mapIni, IGameMode gameMode);

    /// <summary>Applies disallowed side indices to the given array (checkboxes only).</summary>
    void ApplyDisallowedSideIndex(bool[] disallowedArray);

    /// <summary>Gets the raw value data for dropdowns (used for spawn INI STRING mode).</summary>
    IReadOnlyList<string>? DropDownItems { get; }
}
