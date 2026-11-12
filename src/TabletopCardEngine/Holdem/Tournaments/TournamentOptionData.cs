using TabletopCardEngine.Holdem.Mappers;

namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>
/// The rules a tournament is created with; they never change afterwards. Build it with an object initializer: every
/// option has a default, so a new option can be added without breaking a caller. A freezeout: no rebuys, add-ons or
/// late registration.
/// <para>
/// Record equality compares <see cref="Levels"/> and <see cref="Payouts"/> element by element for values the engine
/// returns, so the options read back from a tournament equal the ones it was created with.
/// </para>
/// </summary>
public sealed record TournamentOptionData
{
    /// <summary>How bets are sized at every table. Must be a structure the engine plays, as for a cash game.</summary>
    public BettingStructure BettingStructure { get; init; } = BettingStructure.NoLimit;

    /// <summary>The format of every table, which caps <see cref="MaxPlayersPerTable"/>: <see cref="HoldemTableFormat.HeadsUp"/> is exactly 2, <see cref="HoldemTableFormat.SixMax"/> 2 to 6 and <see cref="HoldemTableFormat.FullRing"/> 7 to 10. Must be a defined value.</summary>
    public HoldemTableFormat TableFormat { get; init; } = HoldemTableFormat.SixMax;

    /// <summary>The seats of every table; must lie in the range of <see cref="TableFormat"/>.</summary>
    public int MaxPlayersPerTable { get; init; } = 6;

    /// <summary>
    /// The most bets a street may hold under <see cref="TabletopCardEngine.Holdem.BettingStructure.FixedLimit"/>, as for a cash game:
    /// <see langword="null"/> means no cap, otherwise it must be at least 1. Ignored by the other structures.
    /// </summary>
    public int? RaiseCap { get; init; } = 4;

    /// <summary>Who posts the ante of a level that has one. Must be a defined <see cref="TabletopCardEngine.Holdem.AnteType"/> value.</summary>
    public AnteType AnteType { get; init; } = AnteType.Classic;

    /// <summary>The chips every player starts with. Must be positive and have at most two decimal places.</summary>
    public decimal StartingStack { get; init; } = 10000;

    /// <summary>
    /// What each entry adds to the prize pool, which is this times the number of entries; <c>0</c> (the default) makes
    /// a free tournament. A fee on top of it is the host's business. Must not be negative and must have at most two decimal places.
    /// </summary>
    public decimal BuyIn { get; init; }

    /// <summary>The fewest entries the tournament can start with. Must be at least 2.</summary>
    public int MinEntries { get; init; } = 2;

    /// <summary>The most entries registration takes; <see langword="null"/> (the default) means no limit. When set it must be at least <see cref="MinEntries"/>.</summary>
    public int? MaxEntries { get; init; }

    /// <summary>
    /// The blind levels in the order they are played. Must hold 1 to 100 levels whose small blinds never decrease from
    /// one level to the next; see <see cref="TournamentLevelData"/> for each level's rules. The levels together are
    /// stored in one column, so they must also fit in 4 000 characters of JSON. Empty by default, so a caller has to
    /// set them.
    /// </summary>
    public IReadOnlyList<TournamentLevelData> Levels { get; init; } = EquatableList<TournamentLevelData>.Empty;

    /// <summary>
    /// The share of the prize pool each place wins, in percent: first place first, then second, and so on. Must hold
    /// 1 to <see cref="MinEntries"/> values, each positive with at most two decimal places, none larger than the one
    /// before it, summing to exactly 100. Defaults to <c>[100]</c>: winner takes all.
    /// </summary>
    public IReadOnlyList<decimal> Payouts { get; init; } = new EquatableList<decimal>([100m]);
}
