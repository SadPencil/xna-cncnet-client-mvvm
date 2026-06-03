namespace AvClientMvvmContract.Domain.Multiplayer;

/// <summary>
/// Read-only view of a game session setting exposed to the View.
/// Only contains what the View needs to display a checkbox or dropdown.
/// Business logic (ApplySpawnIniCode, ApplyMapCode, etc.) lives on the
/// concrete GameSessionSetting class in the ViewModel project.
/// </summary>
public interface IGameSessionSetting
{
    string Name { get; }
    int Value { get; set; }
}
