namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// Read-only view of a game option filter value.
/// </summary>
public interface IGameOptionFilterValue
{
    IGameOptionFilterDefinition Definition { get; }
    int SelectedIndex { get; }
}
