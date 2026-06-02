namespace AvClientMvvmContract.Domain.Multiplayer;

/// <summary>
/// Represents a game session setting that can be broadcast to the lobby.
/// </summary>
public interface IGameSessionSetting
{
    /// <summary>Gets the name of this setting.</summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the value of this setting.
    /// For checkboxes: 0 = unchecked/off, 1 = checked/on.
    /// For dropdowns: the selected index.
    /// </summary>
    int Value { get; set; }
}
