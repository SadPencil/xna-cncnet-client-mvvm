namespace DXMainClientViewModel.Domain.Multiplayer;

/// <summary>
/// Represents the rank/star level for a game lobby.
/// </summary>
public record Rank
{
    private readonly int rank;

    public static readonly Rank None = 0;
    public static readonly Rank Easy = 1;
    public static readonly Rank Medium = 2;
    public static readonly Rank Hard = 3;

    private Rank(int rank) => this.rank = rank;

    public static implicit operator int(Rank value) => value.rank;

    public static implicit operator Rank(int value) => new Rank(value);
}
