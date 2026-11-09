namespace TabletopCardEngine.Tests.Phh;

/// <summary>
/// One hand history in the PHH format. Field names mirror the specification, so
/// <see href="https://phh.readthedocs.io/en/stable/spec.html">the spec</see> reads as documentation
/// for this record. Only the fields the engine replay needs are modelled.
/// </summary>
internal sealed record PhhHand
{
    /// <summary>Upstream file this hand came from, e.g. <c>pluribus/30/0.phh</c>. Used in failure messages.</summary>
    internal required string Source { get; init; }

    /// <summary>One-based position within the packed <c>.phhs</c> file.</summary>
    internal required int Index { get; init; }

    /// <summary>Variant code; <c>NT</c> is no-limit and <c>FT</c> fixed-limit Texas hold'em.</summary>
    internal required string Variant { get; init; }

    internal required decimal[] Antes { get; init; }

    /// <summary>
    /// Forced bets per player. Per the specification the first player is the small blind and the
    /// last player is on the button, so index 0 is the small blind and index 1 the big blind.
    /// </summary>
    internal required decimal[] BlindsOrStraddles { get; init; }

    /// <summary><c>null</c> in a fixed-limit hand, where the specification forbids <c>min_bet</c>.</summary>
    internal decimal? MinBet { get; init; }

    /// <summary>The fixed-limit bet size of the first two streets (<c>small_bet</c>); <c>null</c> outside fixed-limit.</summary>
    internal decimal? SmallBet { get; init; }

    /// <summary>The fixed-limit bet size of the last two streets (<c>big_bet</c>); <c>null</c> outside fixed-limit.</summary>
    internal decimal? BigBet { get; init; }

    /// <summary>
    /// Each player's net result, from the custom field <c>_results</c> some corpora carry in place of
    /// <c>finishing_stacks</c>. <c>null</c> when the hand has none.
    /// </summary>
    internal decimal[]? Results { get; init; }

    /// <summary>
    /// The custom field <c>_tce_divergence</c>: the comma-separated names of the rules on which pokerkit
    /// and the engine differ in this hand. <c>null</c> when the hand has none.
    /// </summary>
    internal string? Divergence { get; init; }

    /// <summary><c>null</c> for a player whose stack the history does not record.</summary>
    internal required decimal?[] StartingStacks { get; init; }

    internal required string[] Players { get; init; }

    internal decimal[]? FinishingStacks { get; init; }

    internal required PhhAction[] Actions { get; init; }

    internal int PlayerCount => StartingStacks.Length;

    internal decimal SmallBlind => BlindsOrStraddles[0];

    internal decimal BigBlind => BlindsOrStraddles[1];

    /// <summary>Every board card the history records, flop first, in dealing order.</summary>
    internal Card?[] Board => [.. Actions.OfType<PhhDealBoardAction>().SelectMany(deal => deal.Cards)];

    public override string ToString() => $"{Source} (hand #{Index})";
}
