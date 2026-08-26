using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Engine;

/// <summary>
/// Ranks hole cards against a board using the <c>HoldemHand</c> package. A process-wide
/// singleton — there is no per-hand state to hold, just like before.
/// </summary>
internal sealed class HandEvaluator
{
    internal static readonly HandEvaluator Instance = new();

    private static readonly Dictionary<HoldemHand.Hand.HandTypes, HandRankType> _rankTypeByHandType = new()
    {
        [HoldemHand.Hand.HandTypes.HighCard] = HandRankType.HighCard,
        [HoldemHand.Hand.HandTypes.Pair] = HandRankType.Pair,
        [HoldemHand.Hand.HandTypes.TwoPair] = HandRankType.TwoPair,
        [HoldemHand.Hand.HandTypes.Trips] = HandRankType.Trips,
        [HoldemHand.Hand.HandTypes.Straight] = HandRankType.Straight,
        [HoldemHand.Hand.HandTypes.Flush] = HandRankType.Flush,
        [HoldemHand.Hand.HandTypes.FullHouse] = HandRankType.FullHouse,
        [HoldemHand.Hand.HandTypes.FourOfAKind] = HandRankType.FourOfAKind,
        [HoldemHand.Hand.HandTypes.StraightFlush] = HandRankType.StraightFlush,
    };

    private HandEvaluator() { }

    /// <summary>A single comparable strength value — higher wins. Used to rank hands against each other.</summary>
    internal static uint EvaluateStrength(IReadOnlyList<Card> hole, IReadOnlyList<Card> board)
        => GetHand(hole, board).HandValue;

    /// <summary>The category of hand (pair, flush, ...), for display.</summary>
    internal static HandRankType EvaluateRankType(IReadOnlyList<Card> hole, IReadOnlyList<Card> board)
        => _rankTypeByHandType.TryGetValue(GetHand(hole, board).HandTypeValue, out var rankType)
            ? rankType
            : HandRankType.None;

    private static HoldemHand.Hand GetHand(IReadOnlyList<Card> hole, IReadOnlyList<Card> board)
        => new(pocket: ToHoldemHandString(hole), board: ToHoldemHandString(board));

    // HoldemHand wants "Rank+suit" per card (e.g. "Ah"); the domain's Card.ToString() is
    // "rank+suit" already, lowercase (e.g. "ah") — only the rank needs upper-casing.
    private static string ToHoldemHandString(IReadOnlyList<Card> cards)
        => string.Join(" ", cards.Select(card =>
        {
            var s = card.ToString();
            return $"{char.ToUpperInvariant(s[0])}{s[1]}";
        }));
}
