using DXMainClientMvvmContract.Multiplayer;
namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// Defines a game option filter that can be displayed in the filters panel.
/// </summary>
public sealed class GameOptionFilterDefinition : IGameOptionFilterDefinition
{
    /// <summary>
    /// The name of the game option (matches INI key).
    /// </summary>
    public required string OptionName { get; init; }

    /// <summary>
    /// Whether this is a checkbox filter (true) or dropdown filter (false).
    /// Checkbox filters have 3 states: All, On, Off.
    /// Dropdown filters have N+1 states: All, then the dropdown items.
    /// </summary>
    public bool IsCheckbox { get; init; }

    /// <summary>
    /// The display text for the filter label.
    /// </summary>
    public required string DisplayText { get; init; }

    /// <summary>
    /// Path to the enabled icon (for checkbox filters).
    /// </summary>
    public string? EnabledIconPath { get; init; }

    /// <summary>
    /// Path to the disabled icon (for checkbox filters).
    /// </summary>
    public string? DisabledIconPath { get; init; }

    /// <summary>
    /// The number of items in the dropdown (for dropdown filters).
    /// Checkbox filters always have 3 items (All, On, Off).
    /// </summary>
    public int DropdownItemCount { get; init; }
}
