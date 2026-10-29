namespace TabletopCardEngine.Holdem.Dto;

/// <summary>
/// The fixed rules a game is created with; they never change once the game exists.
/// <para>
/// This is where other games and betting structures will plug in: a new option is a new
/// property with a default, so existing callers keep compiling.
/// </para>
/// </summary>
public sealed record GameOptionData
{
    /// <summary>The poker variant. Only <see cref="TabletopCardEngine.Holdem.GameType.Holdem"/> exists in v1.</summary>
    public GameType GameType { get; init; } = GameType.Holdem;

    /// <summary>How bets are sized. Only <see cref="TabletopCardEngine.Holdem.BettingStructure.NoLimit"/> exists in v1.</summary>
    public BettingStructure BettingStructure { get; init; } = BettingStructure.NoLimit;

    /// <summary>
    /// The table format, which caps the seats: <see cref="HoldemTableFormat.HeadsUp"/> is exactly 2,
    /// <see cref="HoldemTableFormat.SixMax"/> is 2 to 6 and <see cref="HoldemTableFormat.FullRing"/> is 7 to 10.
    /// Defaults to <see cref="HoldemTableFormat.SixMax"/>, so a caller that never sets it gets the 1.0 table.
    /// An undefined value is rejected when the game is created.
    /// </summary>
    public HoldemTableFormat TableFormat { get; init; } = HoldemTableFormat.SixMax;

    /// <summary>
    /// How many seats the table has; must lie in the range of <see cref="TableFormat"/> and within the
    /// engine's configured seat limits, which are 2 to 10 by default.
    /// </summary>
    public int MaxPlayersCount { get; init; } = 6;

    /// <summary>The small blind of every hand in this release; the big blind is twice this. Read a hand's actual blinds from <see cref="HandData.SmallBlind"/> and <see cref="HandData.BigBlind"/>. Must be positive and have at most two decimal places.</summary>
    public decimal InitialSmallBlind { get; init; } = 1;

    /// <summary>Where the button sits for the first hand. Seats are 1-based.</summary>
    public byte InitialDealerSeatNo { get; init; } = 1;
}

/// <summary>A player taking a seat, together with the stack they bring to it.</summary>
/// <param name="Name">Display name of the player; must be 1 to 64 characters and not null or whitespace-only.</param>
/// <param name="SeatNo">The seat to occupy. Seats are 1-based; <c>0</c> means "no seat".</param>
/// <param name="BuyInAmount">The stack the player sits down with; must have at most two decimal places.</param>
public record PlayerJoinData(string Name, byte SeatNo, decimal BuyInAmount);

/// <summary>
/// One action the seat to act may legally take, with its sizing window.
/// <para>
/// The two bounds are on different bases by design: for <see cref="PlayerActionType.Call"/> they are
/// an increment ("how much more to put in"), while for <see cref="PlayerActionType.Raise"/> they are
/// both absolute street totals ("raise the street wager up to this") — the same basis
/// <see cref="HoldemActionPlay.Size"/> expects. A "call" button wants a top-up amount; a
/// "raise to" slider wants an absolute.
/// </para>
/// </summary>
/// <param name="PlayerActionType">The action this window describes.</param>
/// <param name="MinValue">Smallest legal amount, on the basis described above.</param>
/// <param name="MaxValue">Largest legal amount, on the basis described above.</param>
public record PlayerActionEvaluationData(PlayerActionType PlayerActionType, decimal MinValue = 0, decimal MaxValue = 0);

/// <summary>What a seat walked away from a finished hand with.</summary>
/// <param name="Net">Chips won minus chips wagered over the whole hand; negative for a loser.</param>
/// <param name="Strength">A comparable hand-strength value — only meaningful against another seat's value in the same hand.</param>
/// <param name="RankType">The display category of the made hand.</param>
/// <param name="Description">Human-readable name of the made hand's category, such as "Two pair"; <see langword="null"/> when the seat did not reach showdown (it folded, or the hand ended because everyone else folded).</param>
public record HandResultData(decimal Net, uint Strength, HandRankType RankType, string? Description = null);
