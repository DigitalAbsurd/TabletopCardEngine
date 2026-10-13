using System.Numerics;
using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Engine;

/// <summary>
/// Ranks hole cards against a board. A process-wide singleton, because there is no per-hand state
/// to hold.
/// </summary>
/// <remarks>
/// Written in-repo on purpose. It replaces the <c>HandEvaluator</c> NuGet package, which declares no
/// license and derives from Keith Rule's GPLv3 port of poker-eval, so it could not ship under a
/// permissive license. There are no lookup tables: a hand is at most seven cards, so building a
/// rank mask per suit and a count per rank is cheap enough, with nothing to initialise at start-up.
/// <para>
/// A strength packs the <see cref="HandRankType"/> into bits 20 and up, then the five deciding
/// ranks four bits apiece, most significant first. So comparing two values as plain integers
/// compares the hands, and hands whose best five cards are equal get equal values. Ranks are
/// stored as 1 (two) to 13 (ace), which leaves 0 for "no card": with fewer than five cards the
/// missing kickers are 0 and cannot be mistaken for a two.
/// </para>
/// </remarks>
internal sealed class HandEvaluator
{
    internal static readonly HandEvaluator Instance = new();

    private const int RankCount = 13;
    private const int CategoryShift = 20;
    private const int KickerBits = 4;
    private const int HandSize = 5;

    // Rank indices are 0 (two) to 12 (ace), so the wheel is the ace plus the two to the five.
    private const int WheelMask = (1 << 12) | 0b1111;
    private const int FiveHighIndex = 3;

    private HandEvaluator() { }

    /// <summary>A single comparable strength value — higher wins. Used to rank hands against each other.</summary>
    internal static uint EvaluateStrength(IReadOnlyList<Card> hole, IReadOnlyList<Card> board)
    {
        Span<int> suitMasks = stackalloc int[4];
        Span<int> rankCounts = stackalloc int[RankCount];
        AddCards(hole, suitMasks, rankCounts);
        AddCards(board, suitMasks, rankCounts);

        var flushMask = 0;
        foreach (var suitMask in suitMasks)
        {
            // Seven cards hold at most one five-card suit, so the first one found is the only one.
            if (BitOperations.PopCount((uint)suitMask) >= HandSize)
            {
                flushMask = suitMask;
                break;
            }
        }

        if (flushMask != 0 && HighestStraight(flushMask) is var straightFlushHigh and >= 0)
        {
            return Encode(HandRankType.StraightFlush, straightFlushHigh);
        }

        var quads = HighestWithCount(rankCounts, 4, exclude: -1);
        if (quads >= 0)
        {
            return EncodeWithKickers(HandRankType.FourOfAKind, rankCounts, 1, quads);
        }

        var trips = HighestWithCount(rankCounts, 3, exclude: -1);
        // A second set counts as the pair of a full house: it uses two of its three cards.
        var fullHousePair = trips >= 0 ? HighestWithCount(rankCounts, 2, exclude: trips) : -1;
        if (fullHousePair >= 0)
        {
            return Encode(HandRankType.FullHouse, trips, fullHousePair);
        }

        if (flushMask != 0)
        {
            return EncodeFlush(flushMask);
        }

        var allRanks = suitMasks[0] | suitMasks[1] | suitMasks[2] | suitMasks[3];
        if (HighestStraight(allRanks) is var straightHigh and >= 0)
        {
            return Encode(HandRankType.Straight, straightHigh);
        }

        if (trips >= 0)
        {
            return EncodeWithKickers(HandRankType.Trips, rankCounts, 2, trips);
        }

        var highPair = HighestWithCount(rankCounts, 2, exclude: -1);
        var lowPair = highPair >= 0 ? HighestWithCount(rankCounts, 2, exclude: highPair) : -1;
        if (lowPair >= 0)
        {
            // A third pair is not lost: it plays as the kicker when it outranks the singles.
            return EncodeWithKickers(HandRankType.TwoPair, rankCounts, 1, highPair, lowPair);
        }

        if (highPair >= 0)
        {
            return EncodeWithKickers(HandRankType.Pair, rankCounts, 3, highPair);
        }

        return EncodeWithKickers(HandRankType.HighCard, rankCounts, HandSize);
    }

    /// <summary>The category of hand (pair, flush, ...), for display.</summary>
    /// <remarks>Read back out of the strength, so the category and the ranking can never disagree.</remarks>
    internal static HandRankType EvaluateRankType(IReadOnlyList<Card> hole, IReadOnlyList<Card> board)
        => (HandRankType)(EvaluateStrength(hole, board) >> CategoryShift);

    /// <summary>The display text of a made hand's category; <see langword="null"/> for <see cref="HandRankType.None"/>.</summary>
    internal static string? Describe(HandRankType rankType) => rankType switch
    {
        HandRankType.HighCard => "High card",
        HandRankType.Pair => "Pair",
        HandRankType.TwoPair => "Two pair",
        HandRankType.Trips => "Three of a kind",
        HandRankType.Straight => "Straight",
        HandRankType.Flush => "Flush",
        HandRankType.FullHouse => "Full house",
        HandRankType.FourOfAKind => "Four of a kind",
        HandRankType.StraightFlush => "Straight flush",
        _ => null
    };

    private static void AddCards(IReadOnlyList<Card> cards, Span<int> suitMasks, Span<int> rankCounts)
    {
        foreach (var card in cards)
        {
            var rank = RankIndex(card.CardType);
            suitMasks[(int)card.CardSuit] |= 1 << rank;
            rankCounts[rank]++;
        }
    }

    /// <summary><see cref="CardType.Ace"/> is 1 in the enum but ranks above the king.</summary>
    private static int RankIndex(CardType cardType) => cardType == CardType.Ace ? 12 : (int)cardType - 2;

    /// <summary>The rank index of the top card of the highest five-card run in <paramref name="mask"/>, or -1.</summary>
    private static int HighestStraight(int mask)
    {
        for (var high = RankCount - 1; high >= FiveHighIndex + 1; high--)
        {
            var run = 0b11111 << (high - 4);
            if ((mask & run) == run)
            {
                return high;
            }
        }

        return (mask & WheelMask) == WheelMask ? FiveHighIndex : -1;
    }

    private static int HighestWithCount(ReadOnlySpan<int> rankCounts, int count, int exclude)
    {
        for (var rank = RankCount - 1; rank >= 0; rank--)
        {
            if (rank != exclude && rankCounts[rank] >= count)
            {
                return rank;
            }
        }

        return -1;
    }

    private static uint EncodeFlush(int flushMask)
    {
        Span<int> ranks = stackalloc int[HandSize];
        var taken = 0;
        for (var rank = RankCount - 1; rank >= 0 && taken < HandSize; rank--)
        {
            if ((flushMask & (1 << rank)) != 0)
            {
                ranks[taken++] = rank;
            }
        }

        return Encode(HandRankType.Flush, ranks);
    }

    /// <summary>
    /// <paramref name="made"/> followed by the <paramref name="kickerCount"/> highest ranks not
    /// already used by it.
    /// </summary>
    private static uint EncodeWithKickers(HandRankType category, ReadOnlySpan<int> rankCounts, int kickerCount, params int[] made)
    {
        Span<int> ranks = stackalloc int[made.Length + kickerCount];
        made.CopyTo(ranks);
        var taken = made.Length;
        for (var rank = RankCount - 1; rank >= 0 && taken < ranks.Length; rank--)
        {
            if (rankCounts[rank] > 0 && Array.IndexOf(made, rank) < 0)
            {
                ranks[taken++] = rank;
            }
        }

        return Encode(category, ranks[..taken]);
    }

    private static uint Encode(HandRankType category, params int[] ranks) => Encode(category, ranks.AsSpan());

    private static uint Encode(HandRankType category, ReadOnlySpan<int> ranks)
    {
        var value = (uint)category;
        for (var i = 0; i < HandSize; i++)
        {
            value = (value << KickerBits) | (i < ranks.Length ? (uint)ranks[i] + 1 : 0);
        }

        return value;
    }
}
