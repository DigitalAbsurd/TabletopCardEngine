using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Demo.Phh;

/// <summary>
/// One hand history in the PHH format. Field names mirror the specification, so
/// <see href="https://phh.readthedocs.io/en/stable/spec.html">the spec</see> reads as documentation
/// for this record. Only the fields the engine replay needs are modelled.
///
/// This is a port of <c>OpenHoldemEngine.Tests.Phh.PhhHand</c> into the demo project.
/// See the related test project for the original definition and the possible future consolidation
/// into a shared <c>OpenHoldemEngine.Phh</c> project (see task 11 notes in docs/demo-tasks.md).
/// </summary>
public sealed record PhhHand
{
    /// <summary>Upstream file this hand came from, e.g. <c>pluribus/30/0.phh</c>. Used in failure messages.</summary>
    public required string Source { get; init; }

    /// <summary>One-based position within the packed <c>.phhs</c> file.</summary>
    public required int Index { get; init; }

    /// <summary>Variant code; <c>NT</c> is no-limit Texas hold'em.</summary>
    public required string Variant { get; init; }

    public required decimal[] Antes { get; init; }

    /// <summary>
    /// Forced bets per player. Per the specification the first player is the small blind and the
    /// last player is on the button, so index 0 is the small blind and index 1 the big blind.
    /// </summary>
    public required decimal[] BlindsOrStraddles { get; init; }

    public required decimal MinBet { get; init; }

    /// <summary><c>null</c> for a player whose stack the history does not record.</summary>
    public required decimal?[] StartingStacks { get; init; }

    public required string[] Players { get; init; }

    public decimal[]? FinishingStacks { get; init; }

    public required PhhAction[] Actions { get; init; }

    public int PlayerCount => StartingStacks.Length;

    public decimal SmallBlind => BlindsOrStraddles[0];

    public decimal BigBlind => BlindsOrStraddles[1];

    /// <summary>Every board card the history records, flop first, in dealing order.</summary>
    public Card?[] Board => [.. Actions.OfType<PhhDealBoardAction>().SelectMany(deal => deal.Cards)];

    public override string ToString() => $"{Source} (hand #{Index})";
}
