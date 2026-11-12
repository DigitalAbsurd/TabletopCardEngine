using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.Tests;

/// <summary>
/// <c>DealTable</c> across several tables: a table breaks when the players left fit at fewer tables, a table two
/// players larger than the shortest gives it its next big blind, and a player moving between tables is seated, with its
/// exact stack, by the target's next deal. The decks are scripted: every seat holds a pair, the lower the seat the
/// bigger the pair, so the lowest seat of an all-in wins it. After every action the chips are
/// counted, and every player is checked to be in exactly one place.
/// </summary>
[TestClass]
public sealed class TournamentTableBalanceTests
{
    private static readonly Guid _tournamentId = Guid.Parse("{72472472-4724-7247-2472-472472472472}");

    private const decimal StartingStack = 1000;

    private const int MaxPlayersPerTable = 6;

    /// <summary>No straight and no flush can be made with it and the pairs below, so the bigger pair always wins.</summary>
    private static readonly Card[] _board = CardsFrom("2c", "3d", "4h", "6s", "7c");

    /// <summary>The pair of seat <c>n</c> is the <c>n</c>-th, so without a favourite the lowest seat wins.</summary>
    private static readonly string[] _pairRanks = ["k", "q", "j", "t", "9", "8"];

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public async Task DealTable_PlayersFitAtFewerTables_BreaksTheTableDealtAndSpreadsItsPlayers(int bustTableNo)
    {
        using var session = Session.Start(entryCount: 18);
        var started = await session.StartAsync();
        started.Tables.Select(table => table.PlayerCount).Should().Equal(6, 6, 6);
        var otherTableNo = bustTableNo == 2 ? 3 : 2;

        // Five players bust at table 1, which is then left waiting with one player: 13 players still need three tables.
        await session.PlayHandAsync(1, AllInAmong(1, 2, 3, 4, 5, 6));
        var afterTableOne = await session.DealAsync(1);
        afterTableOne.Entries.Count(entry => entry.State == TournamentEntryState.Eliminated).Should().Be(5);
        afterTableOne.Tables.Should().OnlyContain(table => !table.IsBroken);
        afterTableOne.Tables[0].PlayerCount.Should().Be(1);

        // One more bust leaves 12, who fit at two tables: the next table to deal is the one that breaks.
        await session.PlayHandAsync(bustTableNo, AllInAmong(1, 2));
        await session.PlayHandAsync(otherTableNo, FoldToTheBigBlind);
        var broken = await session.DealAsync(bustTableNo);

        broken.Entries.Count(entry => entry.Place is null).Should().Be(12);
        broken.Tables.Single(table => table.TableNo == bustTableNo).IsBroken.Should().BeTrue();
        broken.Tables.Where(table => table.TableNo != bustTableNo).Should().OnlyContain(table => !table.IsBroken);
        var moving = broken.Entries.Where(entry => entry.State == TournamentEntryState.Moving).ToList();
        moving.Should().HaveCount(5);
        moving.Should().OnlyContain(entry => entry.TableNo == 1, "table 1 has the lowest count until it holds as many as the other table");
        moving.Select(entry => entry.SeatNo).Should().BeEquivalentTo(new byte?[] { 2, 3, 4, 5, 6 }, "seat 1 holds table 1's survivor");
        var bustGame = await session.GetGameAsync(bustTableNo);
        bustGame.IsFinished.Should().BeTrue();
        bustGame.HandData!.No.Should().Be(1, "nothing is dealt at a table that breaks");
        AssertFailed(await session.Tournament.ApplyAsync(TournamentAction.DealTable(_tournamentId, bustTableNo)), HoldemErrorCode.InvalidTable);

        var carried = await session.LoadCarriedStacksAsync();
        var gathered = await session.DealAsync(1);
        gathered.Tables[0].PlayerCount.Should().Be(6);
        await session.AssertJoinedWithTheirStacksAsync(1, carried);

        var final = await session.DealAsync(otherTableNo);
        final.Tables.Where(table => !table.IsBroken).Select(table => table.PlayerCount).Should().Equal(6, 6);
        final.Entries.Where(entry => entry.Place is null).Should().OnlyContain(entry => entry.State == TournamentEntryState.Seated);
    }

    [TestMethod]
    public async Task DealTable_TwoPlayersLargerThanTheShortestTable_MovesTheNextBigBlindWithItsStack()
    {
        using var session = Session.Start(entryCount: 12);
        await session.StartAsync();

        // Table 2 loses two players and deals on with four.
        await session.PlayHandAsync(2, AllInAmong(1, 2, 3));
        var shortened = await session.DealAsync(2);
        shortened.Tables.Select(table => table.PlayerCount).Should().Equal(6, 4);
        shortened.Entries.Should().NotContain(entry => entry.State == TournamentEntryState.Moving, "the shorter table never gives a player away");

        // Every seat of table 1 still has chips, so its next big blind is the seat after this hand's.
        var ended = await session.PlayHandAsync(1, FoldToTheBigBlind);
        var dueSeatNo = (byte)((ended.BigBlindSeatNo % MaxPlayersPerTable) + 1);
        dueSeatNo.Should().NotBe(1, "the probe that moves the lowest seat instead must fail");
        var due = shortened.Entries.Single(entry => entry.TableNo == 1 && entry.SeatNo == dueSeatNo);
        var dueStack = (await session.GetGameAsync(1)).HandData!.Seats.Single(seat => seat.SeatNo == dueSeatNo) is var seat
            ? seat.StartHandStack + seat.HandResult!.Net
            : 0;

        var balanced = await session.DealAsync(1);

        var moving = balanced.Entries.Where(entry => entry.State == TournamentEntryState.Moving).ToList();
        moving.Should().ContainSingle().Which.EntryNo.Should().Be(due.EntryNo);
        moving[0].TableNo.Should().Be(2);
        moving[0].SeatNo.Should().Be(2, "seats 2 and 3 of table 2 were freed by the busts, and the lowest is reserved");
        balanced.Tables.Select(table => table.PlayerCount).Should().Equal(5, 4);
        (await session.LoadCarriedStacksAsync()).Should().Equal(new Dictionary<int, decimal> { [due.EntryNo] = dueStack });
        var tableOne = (await session.GetGameAsync(1)).HandData!;
        tableOne.No.Should().Be(2);
        tableOne.Seats.Select(seat => seat.Name).Should().NotContain(due.Name);

        // The player is a tournament row until table 2 deals: its hand in progress is not touched.
        (await session.GetGameAsync(2)).HandData!.Seats.Select(seat => seat.Name).Should().NotContain(due.Name);
        await session.PlayHandAsync(2, FoldToTheBigBlind);
        var carried = await session.LoadCarriedStacksAsync();
        var joined = await session.DealAsync(2);

        joined.Entries.Single(entry => entry.EntryNo == due.EntryNo).Should().Match<TournamentEntryData>(entry =>
            entry.State == TournamentEntryState.Seated && entry.TableNo == 2 && entry.SeatNo == 2);
        joined.Tables.Select(table => table.PlayerCount).Should().Equal(5, 5);
        await session.AssertJoinedWithTheirStacksAsync(2, carried);
    }

    [TestMethod]
    public async Task DealTable_WaitingTable_ReceivesPlayersFromABiggerTable_ThenDeals()
    {
        using var session = Session.Start(entryCount: 12);
        await session.StartAsync();

        await session.PlayHandAsync(2, AllInAmong(1, 2, 3, 4, 5, 6));
        var waiting = await session.DealAsync(2);
        waiting.Tables.Select(table => table.PlayerCount).Should().Equal(6, 1);
        (await session.GetGameAsync(2)).HandData!.No.Should().Be(1, "a table with one player waits");

        await session.PlayHandAsync(1, FoldToTheBigBlind);
        var balanced = await session.DealAsync(1);

        // Six against one: the big blind moves twice, until table 1 is no longer two players larger.
        var moving = balanced.Entries.Where(entry => entry.State == TournamentEntryState.Moving).ToList();
        moving.Should().HaveCount(2).And.OnlyContain(entry => entry.TableNo == 2);
        moving.Select(entry => entry.SeatNo).Should().BeEquivalentTo(new byte?[] { 2, 3 }, "seat 1 holds the survivor");
        balanced.Tables.Select(table => table.PlayerCount).Should().Equal(4, 1);
        (await session.GetGameAsync(2)).HandData!.No.Should().Be(1, "the waiting table deals only when it is called");

        var carried = await session.LoadCarriedStacksAsync();
        var dealt = await session.DealAsync(2);

        dealt.Tables.Select(table => table.PlayerCount).Should().Equal(4, 3);
        var hand = (await session.GetGameAsync(2)).HandData!;
        hand.No.Should().Be(2);
        hand.Seats.Select(seat => seat.SeatNo).Should().Equal((byte)1, (byte)2, (byte)3);
        await session.AssertJoinedWithTheirStacksAsync(2, carried);
    }

    [TestMethod]
    public async Task DealTable_OneBustFromAFinalTable_BreaksTheOtherTableAndEveryoneEndsAtOne()
    {
        using var session = Session.Start(entryCount: 7);
        var started = await session.StartAsync();
        started.Tables.Select(table => table.PlayerCount).Should().Equal(4, 3);

        await session.PlayHandAsync(1, AllInAmong(1, 2));
        await session.PlayHandAsync(2, FoldToTheBigBlind);
        var broken = await session.DealAsync(1);

        broken.Tables.Select(table => table.IsBroken).Should().Equal(true, false);
        // Assigned in the order of their seats at table 1, into the lowest seats table 2 does not hold.
        var survivors = started.Entries.Where(entry => entry.TableNo == 1 && entry.SeatNo != 2).OrderBy(entry => entry.SeatNo).Select(entry => entry.EntryNo);
        survivors.Select(entryNo => broken.Entries.Single(entry => entry.EntryNo == entryNo)).Select(entry => (entry.State, entry.TableNo, entry.SeatNo))
            .Should().Equal((TournamentEntryState.Moving, 2, (byte?)4), (TournamentEntryState.Moving, 2, (byte?)5), (TournamentEntryState.Moving, 2, (byte?)6));
        (await session.GetGameAsync(1)).IsFinished.Should().BeTrue();

        var carried = await session.LoadCarriedStacksAsync();
        var final = await session.DealAsync(2);

        final.Tables.Select(table => table.PlayerCount).Should().Equal(0, 6);
        final.Entries.Where(entry => entry.Place is null).Should().HaveCount(6)
            .And.OnlyContain(entry => entry.State == TournamentEntryState.Seated && entry.TableNo == 2);
        (await session.GetGameAsync(2)).HandData!.Seats.Should().HaveCount(6);
        await session.AssertJoinedWithTheirStacksAsync(2, carried);
    }

    /// <summary>Every seat in <paramref name="seats"/> goes all in, or calls once raising is closed; everyone else folds.</summary>
    private static Func<HandData, byte, PlayerActionType> AllInAmong(params byte[] seats)
        => (hand, seatNo) => seats.Contains(seatNo)
            ? Offers(hand, PlayerActionType.Raise) ? PlayerActionType.AllIn
                : Offers(hand, PlayerActionType.Call) ? PlayerActionType.Call
                : PlayerActionType.Check
            : Offers(hand, PlayerActionType.Check) ? PlayerActionType.Check : PlayerActionType.Fold;

    private static PlayerActionType FoldToTheBigBlind(HandData hand, byte seatNo)
        => Offers(hand, PlayerActionType.Fold) ? PlayerActionType.Fold : PlayerActionType.Check;

    private static bool Offers(HandData hand, PlayerActionType actionType)
        => hand.Evaluations.Any(evaluation => evaluation.PlayerActionType == actionType);

    private static void AssertFailed(HoldemResult<TournamentData> result, HoldemErrorCode code)
    {
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
    }

    private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];

    /// <summary>One tournament on its own database, with the checks every action is followed by.</summary>
    private sealed class Session : IDisposable
    {
        private readonly TestEngineHost.DisposableTestData _host;
        private readonly IDbContextFactory<HoldemDbContext> _contextFactory;
        private readonly int _entryCount;
        private IReadOnlyList<TournamentTableData> _tables = [];

        private Session(TestEngineHost.DisposableTestData host, IHoldemTournament tournament, IDbContextFactory<HoldemDbContext> contextFactory,
            int entryCount)
        {
            _host = host;
            Tournament = tournament;
            Table = host.GetRequiredService<IHoldemTable>();
            _contextFactory = contextFactory;
            _entryCount = entryCount;
        }

        internal IHoldemTournament Tournament { get; }

        internal IHoldemTable Table { get; }

        internal static Session Start(int entryCount)
        {
            var deck = new TestCardShuffler((_, seatNo) => CardsFrom(_pairRanks[seatNo - 1] + "h", _pairRanks[seatNo - 1] + "s"), _ => _board);
            var host = TestEngineHost.SetupTournament(out var tournament, out var contextFactory, timeProvider: new TestTimeProvider(), shuffler: deck);
            return new Session(host, tournament, contextFactory, entryCount);
        }

        public void Dispose() => _host.Dispose();

        internal async Task<TournamentData> StartAsync()
        {
            var result = await Tournament.ApplyAsync(TournamentAction.CreateTournament(new TournamentOptionData
            {
                MaxPlayersPerTable = MaxPlayersPerTable,
                MinEntries = 2,
                MaxEntries = 100,
                StartingStack = StartingStack,
                BuyIn = 10,
                Payouts = [100],
                Levels = [new TournamentLevelData { SmallBlind = 10, Duration = TimeSpan.FromHours(1) }],
            }, _tournamentId));
            result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
            for (var entry = 1; entry <= _entryCount; entry++)
            {
                result = await Tournament.ApplyAsync(TournamentAction.Register(_tournamentId, $"Player {entry}"));
                result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
            }

            result = await Tournament.ApplyAsync(TournamentAction.StartTournament(_tournamentId, 42));
            result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
            _tables = result.Value.Tables;
            await AssertInvariantsAsync();
            return result.Value;
        }

        internal async Task<TournamentData> DealAsync(int tableNo)
        {
            var result = await Tournament.ApplyAsync(TournamentAction.DealTable(_tournamentId, tableNo));
            result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
            await AssertInvariantsAsync();
            return result.Value;
        }

        /// <summary>Plays the table's current hand to its end and returns it as it ended.</summary>
        internal async Task<HandData> PlayHandAsync(int tableNo, Func<HandData, byte, PlayerActionType> policy)
        {
            var gameId = GameIdOf(tableNo);
            var hand = (await GetGameAsync(tableNo)).HandData!;
            hand.Seats.Should().NotContain(seat => seat.HandResult != null, "the table must be dealing a hand");
            for (var actions = 0; !hand.Seats.Any(seat => seat.HandResult is not null); actions++)
            {
                actions.Should().BeLessThan(100);
                var result = await Table.ApplyAsync(HoldemAction.Play(gameId, policy(hand, hand.NextPlayerSeatNo)));
                result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
                hand = result.Value.HandData!;
                await AssertInvariantsAsync();
            }

            return hand;
        }

        internal async Task<GameData> GetGameAsync(int tableNo)
        {
            var result = await Table.ApplyAsync(HoldemAction.GetGame(GameIdOf(tableNo)));
            result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
            return result.Value;
        }

        /// <summary>The stack every moving player carries, by entry number.</summary>
        internal async Task<Dictionary<int, decimal>> LoadCarriedStacksAsync()
        {
            using var dbContext = await _contextFactory.CreateDbContextAsync();
            return await dbContext.Set<TournamentEntry>().AsNoTracking()
                .Where(entry => entry.TournamentId == _tournamentId && entry.CarriedStack != null)
                .ToDictionaryAsync(entry => entry.EntryNo, entry => entry.CarriedStack!.Value);
        }

        /// <summary>
        /// Every player of <paramref name="carried"/> moving to the table was dealt into its new hand in the seat reserved
        /// for it, with exactly the stack it carried, and the table's ledger records the chips as a transfer.
        /// </summary>
        internal async Task AssertJoinedWithTheirStacksAsync(int tableNo, Dictionary<int, decimal> carried)
        {
            var data = (await Tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value;
            var hand = (await GetGameAsync(tableNo)).HandData!;
            using var dbContext = await _contextFactory.CreateDbContextAsync();
            var gameId = GameIdOf(tableNo);
            var transfers = await dbContext.Set<BuyIn>().AsNoTracking()
                .Where(buyIn => buyIn.GameId == gameId && buyIn.HandNo == hand.No && buyIn.Kind == BuyInKind.Transfer)
                .ToListAsync();

            var arrived = data.Entries.Where(entry => carried.ContainsKey(entry.EntryNo) && entry.TableNo == tableNo).ToList();
            arrived.Should().NotBeEmpty();
            transfers.Should().HaveCount(arrived.Count);
            foreach (var entry in arrived)
            {
                entry.State.Should().Be(TournamentEntryState.Seated);
                var seat = hand.Seats.Single(seat => seat.SeatNo == entry.SeatNo);
                seat.Name.Should().Be(entry.Name);
                seat.StartHandStack.Should().Be(carried[entry.EntryNo]);
                transfers.Single(buyIn => buyIn.SeatNo == entry.SeatNo).Amount.Should().Be(carried[entry.EntryNo]);
            }
        }

        private Guid GameIdOf(int tableNo) => _tables.Single(table => table.TableNo == tableNo).GameId;

        /// <summary>
        /// The chips are all there: the stacks of the seated players, read from their games, plus the stacks the moving
        /// players carry, add up to every entry's starting stack. And every player is in one place only: no two players
        /// hold or reserve one seat, and a game's seated players are exactly the tournament's players seated there.
        /// </summary>
        private async Task AssertInvariantsAsync()
        {
            using var dbContext = await _contextFactory.CreateDbContextAsync();
            var tournament = await dbContext.Set<Tournament>().AsNoTracking()
                .Include(candidate => candidate.Entries)
                .Include(candidate => candidate.Tables)
                .SingleAsync(candidate => candidate.Id == _tournamentId);
            var remaining = tournament.Entries.Where(entry => entry.Place is null).ToList();
            var placed = remaining.Where(entry => entry.TableNo is not null).ToList();
            placed.Select(entry => (entry.TableNo, entry.SeatNo)).Should().OnlyHaveUniqueItems("a seat is held or reserved by one player at most");

            var total = remaining.Sum(entry => entry.CarriedStack ?? 0);
            var seatedNames = new List<string>();
            foreach (var table in tournament.Tables)
            {
                var game = (await Table.ApplyAsync(HoldemAction.GetGame(table.GameId))).Value;
                var seated = placed.Where(entry => entry.TableNo == table.TableNo && entry.CarriedStack is null).ToList();
                if (game.HandData is null)
                {
                    total += seated.Count * StartingStack;
                    continue;
                }

                var inGame = game.HandData.Seats.Where(seat => !seat.HasLeft).ToList();
                inGame.Select(seat => (seat.SeatNo, seat.Name)).Should().BeEquivalentTo(seated.Select(entry => (entry.SeatNo!.Value, entry.Name)),
                    $"the players seated in table {table.TableNo}'s game are the tournament's players seated there");
                seatedNames.AddRange(inGame.Select(seat => seat.Name));
                total += inGame.Sum(seat => seat.StartHandStack + (seat.HandResult?.Net ?? 0));
            }

            seatedNames.Should().OnlyHaveUniqueItems("a player sits in one game at most");
            total.Should().Be(_entryCount * StartingStack, "no chip is created or lost, and there is no rake");
        }
    }
}
