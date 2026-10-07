using FluentAssertions;
using TabletopCardEngine.Tests.Phh;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Replays recorded hand histories through the engine. The corpus is the Pluribus subset of the PHH
/// dataset — see <c>Resources/phh/README.md</c> for what it is and why it suits this engine.
/// Every hand records all six players' hole cards and its finishing stacks, which makes it an oracle
/// for the whole pipeline: betting legality, action order, street progression, board dealing, pot
/// splitting and showdown.
/// </summary>
[TestClass]
public sealed class PhhReplayTests
{
    private const int PluribusHandCount = 10_000;
    private const int HeadsUpHandCount = 2_351;

    /// <summary>
    /// How many of the heads-up hands the engine can replay. The rest are showdowns where the history
    /// only recorded one player's cards — real play, unlike the Pluribus bots. Pinned so that a reader
    /// or filter regression that quietly shrinks the corpus fails here.
    /// </summary>
    private const int HeadsUpSupportedHandCount = 2_145;

    /// <summary>
    /// Guards the reader and the fixture itself. In particular it asserts that *no* hand is skipped
    /// as unsupported: without this, a reader regression could silently shrink the corpus the replay
    /// covers and everything would still look green.
    /// </summary>
    [TestMethod]
    public void PhhCorpus_ParsesAndIsFullySupported()
    {
        var corpus = PhhFixture.Pluribus;

        corpus.Should().HaveCount(PluribusHandCount, "the vendored fixture holds every Pluribus hand");

        var unsupported = corpus
            .Select(hand =>
            {
                PhhReplayRunner.IsSupported(hand, out var reason);
                return $"{hand}: {reason}";
            })
            .Where(entry => !entry.EndsWith(": ", StringComparison.Ordinal))
            .ToArray();

        unsupported.Should().BeEmpty("every hand of this corpus should be replayable by the engine");
        corpus.Should().OnlyContain(hand => hand.FinishingStacks != null,
            "the finishing stacks are this corpus's oracle, and IsSupported no longer insists on them");

        // A concrete hand, spelled out, so a reader regression fails here rather than mid-replay.
        var firstHand = corpus[0];
        firstHand.Source.Should().Be("pluribus/30/0.phh");
        firstHand.Variant.Should().Be("NT");
        firstHand.BlindsOrStraddles.Should().Equal([50M, 100M, 0M, 0M, 0M, 0M]);
        firstHand.MinBet.Should().Be(100);
        firstHand.Antes.Should().Equal([0M, 0M, 0M, 0M, 0M, 0M]);
        firstHand.StartingStacks.Should().Equal([10_000M, 10_000M, 10_000M, 10_000M, 10_000M, 10_000M]);
        firstHand.FinishingStacks.Should().Equal([9950M, 9900M, 10_000M, 10_000M, 10_150M, 10_000M]);
        firstHand.Players.Should().Equal(["MrWhite", "Gogo", "Budd", "Eddie", "Bill", "Pluribus"]);
        firstHand.Actions.Should().HaveCount(12);
        firstHand.Actions[0].Should().BeEquivalentTo(
            new PhhDealHoleAction("d dh p1 3c9s", 0, [Card.Parse("3c"), Card.Parse("9s")]));
        firstHand.Actions[8].Should().BeEquivalentTo(new PhhCompleteBetRaiseAction("p5 cbr 225", 4, 225M));
        firstHand.Actions[11].Should().BeEquivalentTo(new PhhFoldAction("p2 f", 1));
        firstHand.Board.Should().BeEmpty("this hand never saw a flop");
    }

    /// <summary>
    /// The corpus-wide replay. Capped by <c>TCE_TEST_PHH_HAND_LIMIT</c> so a normal
    /// <c>dotnet test</c> stays quick; set it to <c>0</c> to replay all
    /// <see cref="PluribusHandCount"/> hands.
    /// </summary>
    [TestMethod]
    public async Task PhhPluribusReplay_Test()
    {
        var hands = PhhFixture.PluribusUpToLimit;

        hands.Should().NotBeEmpty();

        using var runner = new PhhReplayRunner();
        foreach (var hand in hands)
        {
            await runner.ReplayAsync(hand);
        }
    }

    /// <summary>
    /// Guards the heads-up corpus. Unlike the Pluribus hands these are real online play, so some are
    /// filtered out — the counts are pinned rather than the filter being asserted empty.
    /// </summary>
    [TestMethod]
    public void PhhHeadsUpCorpus_ParsesAndIsMostlySupported()
    {
        var corpus = PhhFixture.HeadsUp;

        corpus.Should().HaveCount(HeadsUpHandCount, "the vendored fixture holds this many heads-up hands");
        corpus.Should().OnlyContain(hand => hand.BlindsOrStraddles.Length == 2,
            "the packer keeps heads-up hands only");

        var supported = corpus.Count(hand => PhhReplayRunner.IsSupported(hand, out _));
        supported.Should().Be(HeadsUpSupportedHandCount, "how much of the corpus the engine can replay");

        var unsupportedReasons = corpus
            .Select(hand =>
            {
                PhhReplayRunner.IsSupported(hand, out var reason);
                return reason;
            })
            .Where(reason => reason.Length > 0)
            .Distinct()
            .ToArray();

        unsupportedReasons.Should().Equal(
            ["a showdown is reached without every contender's cards and the whole board recorded"],
            "an unsupported hand should only ever be one the history did not show both hands of");

        // A concrete hand, spelled out, so a reader regression fails here rather than mid-replay.
        var firstHand = corpus[0];
        firstHand.Source.Should()
            .Be("handhq/PS-2009-07-01_2009-07-23_50NLH_OBFU/0.5/ps_NLH_handhq_1-OBFUSCATED.phhs#3");
        firstHand.Variant.Should().Be("NT");
        firstHand.BlindsOrStraddles.Should().Equal([0.25M, 0.50M]);
        firstHand.Antes.Should().Equal([0M, 0M]);
        firstHand.StartingStacks.Should().Equal([50.20M, 75.75M]);
        firstHand.FinishingStacks.Should().BeNull("handhq records no finishing stacks");
        firstHand.Actions.Should().HaveCount(3);
        firstHand.Actions[2].Should().BeEquivalentTo(new PhhFoldAction("p2 f", 1),
            "the button posts the small blind heads-up, and here it folds it away");
    }

    /// <summary>
    /// The heads-up replay. Every seat, blind, actor and board card is checked against the history;
    /// the money is not, because handhq records no finishing stacks and rakes its pots.
    /// </summary>
    [TestMethod]
    public async Task PhhHeadsUpReplay_Test()
    {
        var hands = PhhFixture.HeadsUpUpToLimit;

        hands.Should().NotBeEmpty();

        using var runner = new PhhReplayRunner();
        foreach (var hand in hands)
        {
            await runner.ReplayAsync(hand);
        }
    }

    /// <summary>
    /// One heads-up hand played to a showdown, spelled out: the button opens, the big blind
    /// three-bets, and the board runs to the river.
    /// </summary>
    [TestMethod]
    public async Task PhhSingleHand_HeadsUpShowdown()
    {
        using var runner = new PhhReplayRunner();

        var gameData = await runner.ReplayAsync(PhhFixture.Single(
            "handhq/PS-2009-07-01_2009-07-23_50NLH_OBFU/0.5/ps_NLH_handhq_1-OBFUSCATED.phhs#42"));

        var handData = gameData.HandData!;
        handData.DealerSeatNo.Should().Be(2);
        handData.SmallBlindSeatNo.Should().Be(2, "the button posts the small blind heads-up");
        handData.BigBlindSeatNo.Should().Be(1);
        handData.CommunityCards.Should().Equal(CardsFrom("7s", "4s", "3d", "Qs", "3c"));
        handData.Seats.Sum(seat => seat.HandResult!.Net).Should().Be(0);
    }

    /// <summary>Everyone folds to the big blind, who wins the small blind without a board.</summary>
    [TestMethod]
    public async Task PhhSingleHand_WalkToTheBigBlind()
    {
        using var runner = new PhhReplayRunner();

        var gameData = await runner.ReplayAsync(PhhFixture.Single("pluribus/100/26.phh"));

        gameData.HandData!.CommunityCards.Should().BeEmpty();
        gameData.HandData!.Seats.Single(seat => seat.SeatNo == 2).HandResult!.Net.Should()
            .Be(50, "the big blind collects the small blind");
        gameData.HandData!.Seats.Single(seat => seat.SeatNo == 1).HandResult!.Net.Should()
            .Be(-50, "the small blind folded it away");
    }

    /// <summary>
    /// Two aces-up hands chop the river pot, and the pot does not divide evenly — both players end
    /// on a half chip.
    /// </summary>
    [TestMethod]
    public async Task PhhSingleHand_SplitPotWithOddChip()
    {
        using var runner = new PhhReplayRunner();

        var gameData = await runner.ReplayAsync(PhhFixture.Single("pluribus/102/0.phh"));

        gameData.HandData!.Seats
            .Where(seat => seat.HandResult!.Net > 0)
            .Select(seat => seat.SeatNo)
            .Should().Equal([(byte)1, (byte)5], "seats 1 and 5 chop the pot");
        gameData.HandData!.Seats.Single(seat => seat.SeatNo == 1).HandResult!.Net.Should().Be(112.5M);
        gameData.HandData!.Seats.Single(seat => seat.SeatNo == 5).HandResult!.Net.Should().Be(112.5M);
    }

    /// <summary>Four players see the river, one of them mucks rather than showing.</summary>
    [TestMethod]
    public async Task PhhSingleHand_FourWayShowdownWithMuck()
    {
        using var runner = new PhhReplayRunner();

        var gameData = await runner.ReplayAsync(PhhFixture.Single("pluribus/111b/150.phh"));

        gameData.HandData!.CommunityCards.Should()
            .Equal(CardsFrom("8h", "2s", "Td", "3h", "Ah"));
        gameData.HandData!.Seats
            .Where(seat => seat.HandResult!.Net > 0)
            .Select(seat => seat.SeatNo)
            .Should().Equal([(byte)4, (byte)6], "both ace-six hands play the same board");
    }

    /// <summary>
    /// A pre-flop all-in for the whole stack. Betting closes before the river, so the engine has to
    /// run the rest of the board out on its own — the history has it as <c>5d Kd Th Ac 7d</c>, which
    /// gives the winning kings a set.
    /// </summary>
    [TestMethod]
    public async Task PhhSingleHand_AllInBeforeTheRiver_RunsOutTheBoard()
    {
        using var runner = new PhhReplayRunner();

        var gameData = await runner.ReplayAsync(PhhFixture.Single("pluribus/100b/75.phh"));

        var winner = gameData.HandData!.Seats.Single(seat => seat.SeatNo == 2);
        winner.HandResult!.Net.Should().Be(10_050M, "the pot is still awarded correctly");

        var loser = gameData.HandData!.Seats.Single(seat => seat.SeatNo == 6);
        (loser.StartHandStack + loser.HandResult!.Net).Should().Be(0, "the loser was all in");

        gameData.HandData!.CommunityCards.Should()
            .Equal(CardsFrom("5d", "Kd", "Th", "Ac", "7d"), "the board is run out in full even though betting ended on the flop");
        winner.HandResult.RankType.Should()
            .Be(HandRankType.Trips, "the winning kings pair the king on the board for a set");
    }

    private static Card[] CardsFrom(params string[] cards)
        => [.. cards.Select(card => Card.Parse(card.ToLowerInvariant()))];
}
