namespace DXMainClientMvvmContract.Multiplayer;

/// <summary>
/// Read-only view of a game option filter definition.
/// </summary>
public interface IGameOptionFilterDefinition
{
    string OptionName { get; }
    bool IsCheckbox { get; }
    string DisplayText { get; }
    string? EnabledIconPath { get; }
    string? DisabledIconPath { get; }
    int DropdownItemCount { get; }
}
