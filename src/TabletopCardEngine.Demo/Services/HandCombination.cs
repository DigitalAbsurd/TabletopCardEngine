using TabletopCardEngine.Engine;
using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// Works out which of a seat's cards make its combination, so the table can point at them once a
/// hand is over. The engine returns only the category and a strength value, not the cards.
/// </summary>
public static class HandCombination
{
    private const int HandSize = 5;

    /// <summary>
    /// The cards that make <paramref name="rankType"/> out of <paramref name="hole"/> and
    /// <paramref name="board"/>. Kickers are left out: a pair yields two cards, two pair four, trips
    /// three, quads four, high card the one top card. A straight, flush, full house or straight
    /// flush yields all five.
    /// </summary>
    /// <remarks>
    /// The engine ranks every seat against whatever board was dealt, folded seats and hands that
    /// ended before the river included, so anything from two to seven cards has to work. The
    /// category comes from the engine rather than being ranked again here, and the best five are
    /// scored with the engine's own evaluator, so the lit cards cannot disagree with the result.
    /// </remarks>
    /// <param name="hole">The seat's hole cards.</param>
    /// <param name="board">The community cards.</param>
    /// <param name="rankType">The category the engine reported for this seat.</param>
    /// <returns>The combination's cards; empty for <see cref="HandRankType.None"/>.</returns>
    public static IReadOnlySet<Card> CombinationCards(IReadOnlyList<Card> hole, IReadOnlyList<Card> board, HandRankType rankType)
    {
        ArgumentNullException.ThrowIfNull(hole);
        ArgumentNullException.ThrowIfNull(board);

        var best = BestFive(hole.Concat(board).Distinct().ToArray());

        IEnumerable<Card> combination = rankType switch
        {
            HandRankType.Pair or HandRankType.TwoPair or HandRankType.Trips or HandRankType.FourOfAKind
                => best.GroupBy(card => card.CardType).Where(group => group.Count() >= 2).SelectMany(group => group),
            HandRankType.Straight or HandRankType.Flush or HandRankType.FullHouse or HandRankType.StraightFlush
                => best,
            HandRankType.HighCard
                => best.OrderByDescending(card => HighRank(card.CardType)).Take(1),
            _ => [],
        };

        return combination.ToHashSet();
    }

    /// <summary>The strongest five of <paramref name="cards"/>, or all of them when there are no more than five.</summary>
    private static Card[] BestFive(Card[] cards)
    {
        if (cards.Length <= HandSize)
        {
            return cards;
        }

        Card[] best = [];
        var bestStrength = 0u;
        foreach (var subset in Subsets(cards, HandSize))
        {
            // Only the combined card mask matters to the evaluator, so any hole/board split will do.
            var strength = HandEvaluator.EvaluateStrength(subset[..2], subset[2..]);
            if (best.Length == 0 || strength > bestStrength)
            {
                best = subset;
                bestStrength = strength;
            }
        }

        return best;
    }

    private static IEnumerable<Card[]> Subsets(Card[] cards, int size)
    {
        var indexes = Enumerable.Range(0, size).ToArray();
        while (true)
        {
            yield return indexes.Select(index => cards[index]).ToArray();

            var position = size - 1;
            while (position >= 0 && indexes[position] == cards.Length - size + position)
            {
                position--;
            }

            if (position < 0)
            {
                yield break;
            }

            indexes[position]++;
            for (var next = position + 1; next < size; next++)
            {
                indexes[next] = indexes[next - 1] + 1;
            }
        }
    }

    /// <summary><see cref="CardType.Ace"/> is 1 in the enum but ranks above the king.</summary>
    private static int HighRank(CardType cardType) => cardType == CardType.Ace ? 14 : (int)cardType;
}
