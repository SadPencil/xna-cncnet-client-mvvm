using Rampastring.Tools;

namespace DXMainClientViewModel.Domain.Multiplayer;

/// <summary>
/// Represents a game session setting that can be broadcast to the lobby.
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
    /// <param name="spawnIni">The spawn.ini file.</param>
    void ApplySpawnIniCode(IniFile spawnIni);

    /// <summary>Applies the associated code to the map INI file.</summary>
    /// <param name="mapIni">The map INI file.</param>
    /// <param name="gameMode">Currently selected gamemode, if applicable.</param>
    void ApplyMapCode(IniFile mapIni, IGameMode gameMode);

    /// <summary>Applies disallowed side indexes based on this setting's state.</summary>
    /// <param name="disallowedArray">Array of booleans indicating which sides are disallowed.</param>
    void ApplyDisallowedSideIndex(bool[] disallowedArray);
}
