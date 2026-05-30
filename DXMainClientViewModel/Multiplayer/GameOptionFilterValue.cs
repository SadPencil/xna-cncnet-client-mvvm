namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// Represents the current value of a game option filter.
/// </summary>
public sealed class GameOptionFilterValue : IGameOptionFilterValue
{
    /// <summary>
    /// The filter definition this value corresponds to.
    /// </summary>
    public required IGameOptionFilterDefinition Definition { get; init; }

    /// <summary>
    /// The selected index in the UI dropdown.
    /// 0 = All, 1 = On (for checkbox) or first item (for dropdown), etc.
    /// </summary>
    public int SelectedIndex { get; set; }
}
