using FluentAssertions;
using OpenHoldemEngine.Engine;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// The in-repo <see cref="HandEvaluator"/>, checked against oracles that need no other evaluator.
/// The exhaustive test counts every five-card hand by category against the textbook distribution,
/// and counts the distinct strength values, which must come to 7 462 — the number of
/// equivalence classes of five-card poker hands. A wrong category or a kicker in the wrong place
/// would change one number or the other. The scripted cases below cover what five cards cannot
/// show: picking the best five of seven, and hands with fewer than five cards.
/// </summary>
[TestClass]
public sealed class HandEvaluatorTests
{
    private static readonly Card[] _deck =
    [
        .. from suit in Enum.GetValues<CardSuit>()
           from type in Enum.GetValues<CardType>()
           select new Card(type, suit),
    ];

    [TestMethod]
    public void HandEvaluator_AllFiveCardHands_MatchTheKnownDistribution()
    {
        var countByRankType = new Dictionary<HandRankType, int>();
        var distinctStrengths = new HashSet<uint>();
        var hole = new Card[2];
        var board = new Card[3];

        for (var a = 0; a < _deck.Length; a++)
        {
            for (var b = a + 1; b < _deck.Length; b++)
            {
                for (var c = b + 1; c < _deck.Length; c++)
                {
                    for (var d = c + 1; d < _deck.Length; d++)
                    {
                        for (var e = d + 1; e < _deck.Length; e++)
                        {
                            (hole[0], hole[1]) = (_deck[a], _deck[b]);
                            (board[0], board[1], board[2]) = (_deck[c], _deck[d], _deck[e]);

                            var strength = HandEvaluator.EvaluateStrength(hole, board);
                            var rankType = HandEvaluator.EvaluateRankType(hole, board);
                            countByRankType[rankType] = countByRankType.GetValueOrDefault(rankType) + 1;
                            distinctStrengths.Add(strength);
                        }
                    }
                }
            }
        }

        countByRankType.Should().BeEquivalentTo(new Dictionary<HandRankType, int>
        {
            [HandRankType.StraightFlush] = 40,
            [HandRankType.FourOfAKind] = 624,
            [HandRankType.FullHouse] = 3_744,
            [HandRankType.Flush] = 5_108,
            [HandRankType.Straight] = 10_200,
            [HandRankType.Trips] = 54_912,
            [HandRankType.TwoPair] = 123_552,
            [HandRankType.Pair] = 1_098_240,
            [HandRankType.HighCard] = 1_302_540,
        });
        distinctStrengths.Should().HaveCount(7_462);
    }

    [TestMethod]
    [DataRow("as 2d", "3c 4h 5s", HandRankType.Straight)]
    [DataRow("as 2s", "3s 4s 5s", HandRankType.StraightFlush)]
    [DataRow("as ks", "qs js ts", HandRankType.StraightFlush)]
    [DataRow("7c 7d", "7h 2c 2d 2h 9s", HandRankType.FullHouse)]
    [DataRow("9c 8c", "7c 6d 5c 2c kh", HandRankType.Flush)]
    [DataRow("ah kd", "qc jd 2s 3h 5c", HandRankType.HighCard)]
    [DataRow("ah ad", "", HandRankType.Pair)]
    [DataRow("ah kd", "", HandRankType.HighCard)]
    [DataRow("8h 8d", "8c 8s 2d", HandRankType.FourOfAKind)]
    public void HandEvaluator_EvaluateRankType_FindsTheBestCategory(string hole, string board, HandRankType expected)
        => HandEvaluator.EvaluateRankType(Cards(hole), Cards(board)).Should().Be(expected);

    [TestMethod]
    [DataRow("6c 2d", "as 2h", "3c 4h 5s kd qd", "a six-high straight beats the wheel")]
    [DataRow("ks 3s", "qh jh", "ts 9s 8d 2s 7c", "a flush beats a straight on the same board")]
    [DataRow("ac 2d", "kc 2h", "qc qd 9h 9s 4c", "two pair is split by the kicker")]
    [DataRow("kd 3c", "8s 2c", "ks kh 8c 8d 4s", "a full house compares the trips first")]
    [DataRow("ah 4c", "jh 3c", "8h 8d 8c 8s 2d", "quads on the board play the best kicker")]
    [DataRow("ts 2d", "9s 3d", "tc th 9c 9h 2c", "two sets make the full house with the higher set")]
    [DataRow("5c 5d", "4c 3d", "kd kh 7s 7c 2d", "a third pair plays as the kicker")]
    public void HandEvaluator_EvaluateStrength_RanksTheWinnerHigher(string winnerHole, string loserHole, string board, string because)
        => HandEvaluator.EvaluateStrength(Cards(winnerHole), Cards(board))
            .Should().BeGreaterThan(HandEvaluator.EvaluateStrength(Cards(loserHole), Cards(board)), because);

    [TestMethod]
    [DataRow("2c 3d", "4c 5d", "ah kh qh jh th", "the board plays for both")]
    [DataRow("2c 3d", "2h 3s", "ac kd 9h 8s 7c", "the same ranks in different suits tie")]
    [DataRow("ac 2d", "ad 3c", "kh kd 9s 9c 5h", "a kicker outside the best five does not count")]
    public void HandEvaluator_EvaluateStrength_TiesEqualBestFive(string firstHole, string secondHole, string board, string because)
        => HandEvaluator.EvaluateStrength(Cards(firstHole), Cards(board))
            .Should().Be(HandEvaluator.EvaluateStrength(Cards(secondHole), Cards(board)), because);

    private static Card[] Cards(string cards)
        => [.. cards.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Card.Parse)];
}
