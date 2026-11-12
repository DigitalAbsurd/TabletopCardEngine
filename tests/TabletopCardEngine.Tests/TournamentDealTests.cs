using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.Tests;

/// <summary>
/// <c>DealTable</c>: the eliminations a table's ended hand caused, their places and prizes, the finish of the
/// tournament, and the next hand dealt at the clock's level. The decks are scripted: every seat holds a pair, and the
/// seat a test names as the favourite holds aces, so who wins an all-in is decided before the hand is dealt.
/// </summary>
[TestClass]
public sealed class TournamentDealTests
{
    private static readonly Guid _tournamentId = Guid.Parse("{72372372-3723-7237-2372-372372372372}");

    private static readonly TimeSpan _levelDuration = TimeSpan.FromMinutes(10);

    /// <summary>No straight and no flush can be made with it and the pairs below, so the bigger pair always wins.</summary>
    private static readonly Card[] _board = CardsFrom("2c", "3d", "4h", "6s", "7c");

    /// <summary>The pair of seat <c>n</c> is the <c>n</c>-th, so without a favourite the lowest seat wins.</summary>
    private static readonly string[] _pairRanks = ["k", "q", "j", "t", "9", "8"];

    /// <summary>The stacks three seats start hand 2 with after hand 1 was folded to the big blind at 10/20.</summary>
    private static readonly decimal[] _carriedStacks = [1000m, 990m, 1010m];

    private static TournamentOptionData Options(decimal buyIn, decimal[] payouts, int minEntries = 3) => new()
    {
        MaxPlayersPerTable = 6,
        MinEntries = minEntries,
        StartingStack = 1000,
        BuyIn = buyIn,
        Payouts = payouts,
        Levels =
        [
            new TournamentLevelData { SmallBlind = 10, Duration = _levelDuration },
            new TournamentLevelData { SmallBlind = 20, Ante = 5, Duration = _levelDuration },
        ],
    };

    [TestMethod]
    public async Task DealTable_SixPlayerSitAndGo_PlaysToTheEnd()
    {
        var deck = new ScriptedDeck();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, timeProvider: new TestTimeProvider(), shuffler: deck.Shuffler);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(10, [50, 30, 20], minEntries: 6), 6);
        var data = await StartAsync(tournament);
        var gameId = data.Tables.Single().GameId;

        for (var hands = 0; data.State != TournamentState.Finished; hands++)
        {
            hands.Should().BeLessThan(100, "every hand seat 1 does not open busts the player who does");
            await PlayHandAsync(table, gameId, ShoveAndCall(1));
            data = await DealAsync(tournament, 1);

            if (data.State != TournamentState.Finished)
            {
                var hand = (await GetGameAsync(table, gameId)).HandData!;
                hand.Seats.Should().HaveCount(data.Tables[0].PlayerCount, "an eliminated player's seat is freed before the next hand");
                hand.Seats.Select(seat => seat.Name).Should().BeEquivalentTo(
                    data.Entries.Where(entry => entry.State == TournamentEntryState.Seated).Select(entry => entry.Name));
            }
        }

        data.Entries.Select(entry => entry.Place).Should().BeEquivalentTo(new int?[] { 1, 2, 3, 4, 5, 6 });
        data.Entries.OrderBy(entry => entry.Place).Select(entry => entry.Prize).Should().Equal(30m, 18m, 12m, 0m, 0m, 0m);
        data.Entries.Sum(entry => entry.Prize).Should().Be(60).And.Be(data.PrizePool);
        data.Entries.Single(entry => entry.Place == 1).State.Should().Be(TournamentEntryState.Winner);
        data.Entries.Where(entry => entry.Place != 1).Should().OnlyContain(entry => entry.State == TournamentEntryState.Eliminated);
        data.Entries.Should().OnlyContain(entry => entry.TableNo == null && entry.SeatNo == null);
        data.State.Should().Be(TournamentState.Finished);
        data.FinishedAt.Should().NotBeNull();
        data.Tables.Single().PlayerCount.Should().Be(0);
        (await GetGameAsync(table, gameId)).IsFinished.Should().BeTrue();
    }

    [TestMethod]
    public async Task DealTable_TwoBustedWithDifferentStartStacks_BiggerStackTakesTheBetterPlace()
    {
        var deck = new ScriptedDeck();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, shuffler: deck.Shuffler);
        var table = host.GetRequiredService<IHoldemTable>();

        var (data, hand) = await PlayDifferentStacksAsync(tournament, table, deck, buyIn: 10);

        var losers = hand.Seats.Where(seat => seat.SeatNo != deck.Favourite).OrderByDescending(seat => seat.StartHandStack).ToList();
        losers.Select(seat => seat.StartHandStack).Should().Equal(1000m, 990m);
        var bigger = data.Entries.Single(entry => entry.Name == losers[0].Name);
        var smaller = data.Entries.Single(entry => entry.Name == losers[1].Name);
        bigger.EntryNo.Should().BeGreaterThan(smaller.EntryNo, "the draw is chosen so that ordering by entry number would place them the other way");

        bigger.Place.Should().Be(2);
        bigger.Prize.Should().Be(9);
        smaller.Place.Should().Be(3);
        smaller.Prize.Should().Be(6);
        var winner = data.Entries.Single(entry => entry.Place == 1);
        winner.Name.Should().Be(hand.Seats.Single(seat => seat.SeatNo == deck.Favourite).Name);
        winner.Prize.Should().Be(15);
        data.State.Should().Be(TournamentState.Finished);
    }

    [TestMethod]
    public async Task DealTable_TwoBustedWithEqualStartStacks_TieForTheBetterPlaceAndShareThePrizes()
    {
        var deck = new ScriptedDeck();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, shuffler: deck.Shuffler);
        var table = host.GetRequiredService<IHoldemTable>();

        var data = await PlayEqualStacksAsync(tournament, table, deck, buyIn: 25, [50, 30, 20]);

        data.PrizePool.Should().Be(100);
        data.Entries.OrderBy(entry => entry.Place).Select(entry => (entry.Place, entry.Prize))
            .Should().Equal((1, 50m), (2, 25m), (2, 25m), (4, 0m));
    }

    [TestMethod]
    public async Task DealTable_PoolThatDoesNotDivide_RoundsEveryPlaceToTheCent()
    {
        var deck = new ScriptedDeck();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, shuffler: deck.Shuffler);
        var table = host.GetRequiredService<IHoldemTable>();

        var (data, _) = await PlayDifferentStacksAsync(tournament, table, deck, buyIn: 1);

        data.PrizePool.Should().Be(3);
        data.Entries.OrderBy(entry => entry.Place).Select(entry => entry.Prize).Should().Equal(1.50m, 0.90m, 0.60m);
    }

    [TestMethod]
    public async Task DealTable_RoundingLeavesACent_FirstPlaceGetsIt()
    {
        var deck = new ScriptedDeck();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, shuffler: deck.Shuffler);
        var table = host.GetRequiredService<IHoldemTable>();

        // 30% of 0.08 is 0.024, rounded to 0.02 twice; 40% would be 0.032, but first place gets the 0.04 left.
        var data = await PlayEqualStacksAsync(tournament, table, deck, buyIn: 0.02m, [40, 30, 30]);

        data.PrizePool.Should().Be(0.08m);
        data.Entries.OrderBy(entry => entry.Place).Select(entry => (entry.Place, entry.Prize))
            .Should().Equal((1, 0.04m), (2, 0.02m), (2, 0.02m), (4, 0m));
        data.Entries.Sum(entry => entry.Prize).Should().Be(data.PrizePool);
    }

    [TestMethod]
    [DataRow(3, new[] { 50, 30, 20 }, new[] { "1.50", "0.90", "0.60" })]
    [DataRow(0.08, new[] { 40, 30, 30 }, new[] { "0.04", "0.02", "0.02" })]
    [DataRow(1, new[] { 100 }, new[] { "1" })]
    [DataRow(0.10, new[] { 34, 33, 33 }, new[] { "0.04", "0.03", "0.03" })]
    public void PrizeTable_RoundsToTheCent_AndAddsUpToThePool(double pool, int[] payouts, string[] expected)
    {
        var prizePool = (decimal)pool;
        var prizes = TournamentDealer.PrizeTable(prizePool, [.. payouts.Select(payout => (decimal)payout)]);

        prizes.Should().Equal(expected.Select(decimal.Parse));
        prizes.Sum().Should().Be(prizePool);
    }

    [TestMethod]
    [DataRow(0.05, 2, new[] { "0.03", "0.02" })]
    [DataRow(1, 3, new[] { "0.34", "0.33", "0.33" })]
    [DataRow(50, 2, new[] { "25", "25" })]
    public void SplitPrize_LeftoverCents_GoToTheLowerEntryNumbers(double total, int count, string[] expected)
        => TournamentDealer.SplitPrize((decimal)total, count).Should().Equal(expected.Select(decimal.Parse));

    [TestMethod]
    public async Task DealTable_AfterTheClockPassesALevel_DealsTheNextHandAtTheNewLevel()
    {
        var clock = new TestTimeProvider();
        var deck = new ScriptedDeck();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, timeProvider: clock, shuffler: deck.Shuffler);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(10, [100]), 3);
        var gameId = (await StartAsync(tournament)).Tables.Single().GameId;

        var first = (await GetGameAsync(table, gameId)).HandData!;
        (await table.ApplyAsync(HoldemAction.Play(gameId, PlayerActionType.Fold))).IsSuccess.Should().BeTrue();
        clock.Advance(_levelDuration);

        var inPlay = (await GetGameAsync(table, gameId)).HandData!;
        inPlay.No.Should().Be(first.No);
        inPlay.SmallBlind.Should().Be(10, "the hand in progress keeps the level it was dealt at");
        inPlay.Ante.Should().Be(0);
        (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value.LevelIndex.Should().Be(1);

        await PlayHandAsync(table, gameId, FoldToTheBigBlind);
        await DealAsync(tournament, 1);

        var next = (await GetGameAsync(table, gameId)).HandData!;
        next.No.Should().Be(2);
        next.BlindLevel.Should().Be(1);
        next.SmallBlind.Should().Be(20);
        next.BigBlind.Should().Be(40);
        next.Ante.Should().Be(5);
        next.Actions.Where(action => action.ActionType == HandActionType.Ante).Should().HaveCount(3);
    }

    [TestMethod]
    public async Task DealTable_BeforeTheStart_TournamentNotStarted()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var registered = await CreateWithEntriesAsync(tournament, Options(10, [100]), 3);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.DealTable(_tournamentId, 1)), HoldemErrorCode.TournamentNotStarted);
        (await GetAsync(tournament)).Version.Should().Be(registered.Version, "nothing was saved");
    }

    [TestMethod]
    public async Task DealTable_DuringAHand_PreviousHandNotEnded()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(10, [100]), 3);
        var started = await StartAsync(tournament);
        var before = await GetGameAsync(table, started.Tables[0].GameId);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.DealTable(_tournamentId, 1)), HoldemErrorCode.PreviousHandNotEnded);
        (await GetAsync(tournament)).Version.Should().Be(started.Version, "nothing was saved");
        (await GetGameAsync(table, started.Tables[0].GameId)).ShouldAsJToken().BeEquivalentToAsJToken(before);
    }

    [TestMethod]
    [DataRow(9)]
    [DataRow(0)]
    public async Task DealTable_NoSuchTable_InvalidTable(int tableNo)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(10, [100]), 3);
        var gameId = (await StartAsync(tournament)).Tables[0].GameId;
        await PlayHandAsync(table, gameId, FoldToTheBigBlind);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.DealTable(_tournamentId, tableNo)), HoldemErrorCode.InvalidTable);
    }

    [TestMethod]
    public async Task DealTable_AfterTheFinish_TournamentFinished_AndPlayOnTheTable_GameFinished()
    {
        var deck = new ScriptedDeck();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, shuffler: deck.Shuffler);
        var table = host.GetRequiredService<IHoldemTable>();
        var (finished, _) = await PlayDifferentStacksAsync(tournament, table, deck, buyIn: 10);
        var gameId = finished.Tables[0].GameId;

        AssertFailed(await tournament.ApplyAsync(TournamentAction.DealTable(_tournamentId, 1)), HoldemErrorCode.TournamentFinished);
        (await GetAsync(tournament)).Version.Should().Be(finished.Version, "nothing was saved");

        var played = await table.ApplyAsync(HoldemAction.Play(gameId, PlayerActionType.Fold));
        played.IsFailed.Should().BeTrue();
        played.Errors.Should().ContainSingle().Which.Code.Should().Be(HoldemErrorCode.GameFinished);
    }

    [TestMethod]
    public async Task DealTable_UnknownTournament_TournamentNotFound()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.DealTable(_tournamentId, 1)), HoldemErrorCode.TournamentNotFound);
    }

    [TestMethod]
    public async Task DealTable_NobodyBusted_DealsTheNextHandAndRaisesBothVersionsByOne()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(10, [100]), 3);
        var gameId = (await StartAsync(tournament)).Tables[0].GameId;
        await PlayHandAsync(table, gameId, FoldToTheBigBlind);
        var tournamentBefore = await GetAsync(tournament);
        var gameBefore = await GetGameAsync(table, gameId);

        var dealt = await DealAsync(tournament, 1);

        dealt.Version.Should().Be(tournamentBefore.Version + 1);
        dealt.Entries.Should().OnlyContain(entry => entry.State == TournamentEntryState.Seated && entry.Place == null);
        dealt.Tables[0].PlayerCount.Should().Be(3);
        var game = await GetGameAsync(table, gameId);
        game.Version.Should().Be(gameBefore.Version + 1);
        game.HandData!.No.Should().Be(2);
        game.HandData.Seats.Select(seat => seat.StartHandStack).Should().BeEquivalentTo(_carriedStacks,
            "the stacks carry over from the ledger: the big blind won the small blind's");
    }

    /// <summary>
    /// Three entries. Hand 1 is folded to the big blind, so its seats start hand 2 at 1000, 990 and 1010. In hand 2
    /// everyone is all in and the big blind of hand 1 holds aces: the other two bust together, with different start
    /// stacks, and the tournament finishes.
    /// </summary>
    private static async Task<(TournamentData Data, HandData BustHand)> PlayDifferentStacksAsync(IHoldemTournament tournament, IHoldemTable table, ScriptedDeck deck, decimal buyIn)
    {
        await CreateWithEntriesAsync(tournament, Options(buyIn, [50, 30, 20]), 3);
        var gameId = (await StartAsync(tournament)).Tables.Single().GameId;

        var first = await PlayHandAsync(table, gameId, FoldToTheBigBlind);
        deck.Favourite = first.BigBlindSeatNo;
        await DealAsync(tournament, 1);

        var bust = await PlayHandAsync(table, gameId, ShoveAndCall(1, 2, 3));
        return (await DealAsync(tournament, 1), bust);
    }

    /// <summary>
    /// Four entries. In hand 1 everyone folds to the blinds, who go all in against each other: the lower of the two
    /// seats holds the better pair, wins and doubles up, and the other busts in fourth place; the two who folded keep
    /// their 1000. In hand 2 everyone is all in and the winner of hand 1 holds aces: the two with 1000 bust together,
    /// tied for second.
    /// </summary>
    private static async Task<TournamentData> PlayEqualStacksAsync(IHoldemTournament tournament, IHoldemTable table, ScriptedDeck deck, decimal buyIn, decimal[] payouts)
    {
        await CreateWithEntriesAsync(tournament, Options(buyIn, payouts), 4);
        var gameId = (await StartAsync(tournament)).Tables.Single().GameId;

        var first = await PlayHandAsync(table, gameId, (hand, seatNo) =>
            seatNo == hand.SmallBlindSeatNo ? PlayerActionType.AllIn
            : seatNo == hand.BigBlindSeatNo ? PlayerActionType.Call
            : PlayerActionType.Fold);
        deck.Favourite = Math.Min(first.SmallBlindSeatNo, first.BigBlindSeatNo);
        var afterFirst = await DealAsync(tournament, 1);
        afterFirst.Entries.Where(entry => entry.Place is not null).Select(entry => entry.Place).Should().Equal(4);

        var bust = await PlayHandAsync(table, gameId, ShoveAndCall(1, 2, 3, 4));
        bust.Seats.Where(seat => seat.SeatNo != deck.Favourite).Select(seat => seat.StartHandStack).Should().Equal(1000m, 1000m);
        return await DealAsync(tournament, 1);
    }

    /// <summary>The first seat to act pre-flop goes all in, the <paramref name="callers"/> go all in after it, and everyone else folds.</summary>
    private static Func<HandData, byte, PlayerActionType> ShoveAndCall(params byte[] callers)
        => (hand, seatNo) =>
        {
            if (!hand.Actions.Any(action => action.ActionType == HandActionType.AllIn))
            {
                return PlayerActionType.AllIn;
            }

            if (callers.Contains(seatNo))
            {
                // All in while raising is open, so that a caller is all in too and nobody keeps a few chips back;
                // a call once it is closed, which is never more than the stack.
                return Offers(hand, PlayerActionType.Raise) ? PlayerActionType.AllIn
                    : Offers(hand, PlayerActionType.Call) ? PlayerActionType.Call
                    : PlayerActionType.Check;
            }

            return Offers(hand, PlayerActionType.Check) ? PlayerActionType.Check : PlayerActionType.Fold;
        };

    private static PlayerActionType FoldToTheBigBlind(HandData hand, byte seatNo)
        => Offers(hand, PlayerActionType.Fold) ? PlayerActionType.Fold : PlayerActionType.Check;

    private static bool Offers(HandData hand, PlayerActionType actionType)
        => hand.Evaluations.Any(evaluation => evaluation.PlayerActionType == actionType);

    /// <summary>Plays the table's current hand to its end and returns it as it was dealt.</summary>
    private static async Task<HandData> PlayHandAsync(IHoldemTable table, Guid gameId, Func<HandData, byte, PlayerActionType> policy)
    {
        var dealt = (await GetGameAsync(table, gameId)).HandData!;
        var hand = dealt;
        for (var actions = 0; !hand.Seats.Any(seat => seat.HandResult is not null); actions++)
        {
            actions.Should().BeLessThan(100);
            var result = await table.ApplyAsync(HoldemAction.Play(gameId, policy(hand, hand.NextPlayerSeatNo)));
            result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
            hand = result.Value.HandData!;
        }

        return dealt;
    }

    private static async Task<TournamentData> CreateWithEntriesAsync(IHoldemTournament tournament, TournamentOptionData options, int entryCount)
    {
        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(options, _tournamentId));
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        for (var entry = 1; entry <= entryCount; entry++)
        {
            result = await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, $"Player {entry}"));
            result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        }

        return result.Value;
    }

    private static async Task<TournamentData> StartAsync(IHoldemTournament tournament)
    {
        var result = await tournament.ApplyAsync(TournamentAction.StartTournament(_tournamentId, 42));
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        return result.Value;
    }

    private static async Task<TournamentData> DealAsync(IHoldemTournament tournament, int tableNo)
    {
        var result = await tournament.ApplyAsync(TournamentAction.DealTable(_tournamentId, tableNo));
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        return result.Value;
    }

    private static async Task<TournamentData> GetAsync(IHoldemTournament tournament)
        => (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value;

    private static async Task<GameData> GetGameAsync(IHoldemTable table, Guid gameId)
    {
        var result = await table.ApplyAsync(HoldemAction.GetGame(gameId));
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        return result.Value;
    }

    private static void AssertFailed(HoldemResult<TournamentData> result, HoldemErrorCode code)
    {
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
    }

    private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];

    /// <summary>The cards of every hand: a pair per seat, aces for <see cref="Favourite"/>, and the same board.</summary>
    private sealed class ScriptedDeck
    {
        internal ScriptedDeck() => Shuffler = new TestCardShuffler((_, seatNo) => Hole(seatNo), _ => _board);

        /// <summary>The seat dealt aces from the next deal on; <see langword="null"/> leaves seat 1 the best pair.</summary>
        internal byte? Favourite { get; set; }

        internal TestCardShuffler Shuffler { get; }

        private Card[] Hole(byte seatNo)
            => seatNo == Favourite ? CardsFrom("ah", "as") : CardsFrom(_pairRanks[seatNo - 1] + "h", _pairRanks[seatNo - 1] + "s");
    }
}
