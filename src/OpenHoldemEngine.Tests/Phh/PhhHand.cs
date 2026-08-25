using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Tests.Phh;

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

    /// <summary>Variant code; <c>NT</c> is no-limit Texas hold'em.</summary>
    internal required string Variant { get; init; }

    internal required decimal[] Antes { get; init; }

    /// <summary>
    /// Forced bets per player. Per the specification the first player is the small blind and the
    /// last player is on the button, so index 0 is the small blind and index 1 the big blind.
    /// </summary>
    internal required decimal[] BlindsOrStraddles { get; init; }

    internal required decimal MinBet { get; init; }

    /// <summary><c>null</c> for a player whose stack the history does not record.</summary>
    internal required decimal?[] StartingStacks { get; init; }

    internal required string[] Players { get; init; }

    internal decimal[]? FinishingStacks { get; init; }

    internal required PhhAction[] Actions { get; init; }

    internal int PlayerCount => StartingStacks.Length;

    internal decimal SmallBlind => BlindsOrStraddles[0];

    internal decimal BigBlind => BlindsOrStraddles[1];

    /// <summary>Every board card the history records, flop first, in dealing order.</summary>
    internal Card?[] Board => Actions.OfType<PhhDealBoardAction>().SelectMany(deal => deal.Cards).ToArray();

    /// <summary>
    /// Only the board cards dealt while there was still betting to come. When the last player is
    /// all in, the rest of the board is run out afterwards, and a history records those cards after
    /// the final decision.
    /// </summary>
    internal Card?[] BoardDealtDuringBetting
    {
        get
        {
            var lastDecisionIx = Array.FindLastIndex(Actions, action => action is IPhhPlayerDecision);

            return Actions.Take(lastDecisionIx + 1).OfType<PhhDealBoardAction>()
                .SelectMany(deal => deal.Cards)
                .ToArray();
        }
    }

    public override string ToString() => $"{Source} (hand #{Index})";
}
