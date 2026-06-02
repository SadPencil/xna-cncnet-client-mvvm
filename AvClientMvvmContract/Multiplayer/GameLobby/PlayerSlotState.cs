namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Represents the visual state of a player slot indicator.
/// The View maps these to appropriate textures/icons.
/// </summary>
public enum PlayerSlotState
{
    Empty,
    Unavailable,
    AI,
    NotReady,
    Ready,
    InGame,
    Warning,
    Error
}
