using FluentAssertions;
using OpenHoldemEngine.Demo.Services;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
public sealed class HandCombination_Tests
{
    // The board from the finished hand that prompted the feature: T♦ 9♦ 9♥ 7♠ T♠.
    private const string PairedBoard = "td 9d 9h 7s ts";

    // Each case: hole, board, the rank the engine reports, and the cards that must light up.
    [TestMethod]
    [DataRow("th 6s", PairedBoard, HandRankType.FullHouse, "th td ts 9d 9h", DisplayName = "FullHouse_TensOverNines")]
    [DataRow("ks qs", PairedBoard, HandRankType.TwoPair, "td ts 9d 9h", DisplayName = "TwoPair_KickerStaysDown")]
    [DataRow("2s 2d", PairedBoard, HandRankType.TwoPair, "td ts 9d 9h", DisplayName = "TwoPair_BoardPlaysOverTheLowerPocketPair")]
    [DataRow("ah 4c", "ad 9h 7s 3c 2d", HandRankType.Pair, "ah ad", DisplayName = "Pair")]
    [DataRow("ah kc", "qd 9h 7s 3c 2d", HandRankType.HighCard, "ah", DisplayName = "HighCard_AceRanksHigh")]
    [DataRow("7h 7c", "7d kh 2s 3c 9d", HandRankType.Trips, "7h 7c 7d", DisplayName = "Trips")]
    [DataRow("8h 8c", "8d 8s ah 3c 2d", HandRankType.FourOfAKind, "8h 8c 8d 8s", DisplayName = "FourOfAKind")]
    [DataRow("5h 6c", "7d 8s 9h kc kd", HandRankType.Straight, "5h 6c 7d 8s 9h", DisplayName = "Straight")]
    [DataRow("ah 2c", "3d 4s 5h kc kd", HandRankType.Straight, "ah 2c 3d 4s 5h", DisplayName = "Straight_Wheel")]
    [DataRow("ah 2h", "9h 4h jh kc 5d", HandRankType.Flush, "ah 2h 9h 4h jh", DisplayName = "Flush")]
    [DataRow("9s ts", "js qs ks 2h 2d", HandRankType.StraightFlush, "9s ts js qs ks", DisplayName = "StraightFlush")]
    [DataRow("qh qc", "", HandRankType.Pair, "qh qc", DisplayName = "Pair_PreFlopWithNoBoard")]
    [DataRow("qh 3c", "ad kd 5s", HandRankType.HighCard, "ad", DisplayName = "HighCard_ShortBoard")]
    [DataRow("qh 3c", "ad kd 5s", HandRankType.None, "", DisplayName = "None_LightsNothing")]
    public void CombinationCards_LightsOnlyTheCombination(string hole, string board, HandRankType rankType, string expected)
    {
        // Act
        var cards = HandCombination.CombinationCards(Parse(hole), Parse(board), rankType);

        // Assert
        cards.Select(card => card.ToString()).Should().BeEquivalentTo(Parse(expected).Select(card => card.ToString()));
    }

    private static Card[] Parse(string cards)
        => cards.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Card.Parse).ToArray();
}
