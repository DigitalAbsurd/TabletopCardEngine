namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>
/// One blind level of a tournament. The levels run on a clock from the start of the tournament, and a hand keeps the
/// level it was dealt at. The big blind is twice <see cref="SmallBlind"/>. The last level lasts until the tournament
/// ends, whatever its <see cref="Duration"/>.
/// </summary>
public sealed record TournamentLevelData
{
    /// <summary>The small blind of the level. Must be positive, have at most two decimal places, and not be smaller than the previous level's.</summary>
    public decimal SmallBlind { get; init; }

    /// <summary>The ante of the level, posted as <see cref="TournamentOptionData.AnteType"/> says; <c>0</c> (the default) means none. Must not be negative and must have at most two decimal places.</summary>
    public decimal Ante { get; init; }

    /// <summary>How long the level lasts. Must be at least one minute and a whole number of seconds.</summary>
    public TimeSpan Duration { get; init; }
}
