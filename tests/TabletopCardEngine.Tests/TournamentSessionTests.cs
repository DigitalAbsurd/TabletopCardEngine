using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;
using TabletopCardEngine.Tests.Persistence;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Plays whole tournaments to the end, with real shuffles and a seeded action policy, across many tables and blind
/// levels: the tournament counterpart of <see cref="MultiHandSessionTests"/>. PHH holds hands, not tournaments, so
/// there is no recorded oracle and the invariants are the oracle. The driver reads state the way a host would, from
/// <c>GameData</c> and <c>TournamentData</c> only, and checks after every call that no chip is lost or made, that every
/// player is in exactly one place, that the tables stay balanced, and that every hand is dealt at the level the clock
/// gives at its deal. At the end the places, the prizes and the winner's stack are checked.
/// </summary>
[TestClass]
public sealed class TournamentSessionTests
{
    private const int Seed = 20261112;
    private const int SeatDrawSeed = 725;
    private const decimal StartingStack = 1500;
    private const decimal BuyIn = 100;
    private const int MaxDecisionsPerHand = 300;

    /// <summary>A backstop, so a tournament that never ends fails the test instead of looping forever.</summary>
    private const int MaxHands = 4000;

    /// <summary>One hand in this many shoves, so that stacks change hands fast enough for the tournaments to end.</summary>
    private const int ShoveOdds = 6;

    private const decimal Epsilon = 0.0001M;

    private static readonly TimeSpan _levelDuration = TimeSpan.FromMinutes(10);

    /// <summary>What the level clock moves by for every hand played: a level lasts ten hands, wherever they are played.</summary>
    private static readonly TimeSpan _timePerHand = TimeSpan.FromMinutes(1);

    private static readonly decimal[] _smallBlinds = [10, 25, 50, 75, 100, 150, 200, 300, 400, 600, 800, 1000, 1500, 2000, 3000, 5000];

    [TestMethod]
    [DataRow(6, 6, AnteType.Classic, TestDatabaseKind.Sqlite, DisplayName = "SNG of 6 at 6 per table, classic ante, Sqlite")]
    [DataRow(2, 2, AnteType.BigBlind, TestDatabaseKind.Sqlite, DisplayName = "heads-up of 2, big blind ante, Sqlite")]
    [DataRow(10, 10, AnteType.Classic, TestDatabaseKind.Sqlite, DisplayName = "full ring of 10, classic ante, Sqlite")]
    [DataRow(27, 9, AnteType.BigBlind, TestDatabaseKind.Sqlite, DisplayName = "27 at 9 per table, big blind ante, Sqlite")]
    [DataRow(60, 6, AnteType.Classic, TestDatabaseKind.Sqlite, DisplayName = "60 at 6 per table, classic ante, Sqlite")]
    [DataRow(6, 6, AnteType.BigBlind, TestDatabaseKind.InMemory, DisplayName = "SNG of 6 at 6 per table, big blind ante, InMemory")]
    [DataRow(2, 2, AnteType.Classic, TestDatabaseKind.InMemory, DisplayName = "heads-up of 2, classic ante, InMemory")]
    [DataRow(10, 10, AnteType.BigBlind, TestDatabaseKind.InMemory, DisplayName = "full ring of 10, big blind ante, InMemory")]
    [DataRow(27, 9, AnteType.Classic, TestDatabaseKind.InMemory, DisplayName = "27 at 9 per table, classic ante, InMemory")]
    [DataRow(60, 6, AnteType.BigBlind, TestDatabaseKind.InMemory, DisplayName = "60 at 6 per table, big blind ante, InMemory")]
    public async Task TournamentSession_PlayedToTheEnd_KeepsEveryInvariant(int entryCount, int maxPlayersPerTable, AnteType anteType, TestDatabaseKind database)
    {
        var clock = new TestTimeProvider();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, database, clock, new SeededShuffler(Seed + entryCount * 100 + maxPlayersPerTable));
        var session = new Session(host.GetRequiredService<IHoldemTable>(), tournament, clock, entryCount, maxPlayersPerTable, anteType,
            new Random(Seed + entryCount * 100 + maxPlayersPerTable));

        await session.RunAsync();
    }

    /// <summary>
    /// A shuffle that is random but reproducible: the deck of a hand is drawn from a generator seeded by the game's
    /// order of appearance and the hand's number, so a failing row repeats exactly, whatever the order the calls
    /// arrive in. The engine asks again before every street, and every answer is the same.
    /// </summary>
    private sealed class SeededShuffler(int seed) : ICardShuffler
    {
        private readonly Dictionary<Guid, int> _gameOrder = [];
        private readonly object _lock = new();

        public ValueTask<IReadOnlyList<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo, CancellationToken cancellationToken)
            => ValueTask.FromResult<IReadOnlyList<Card>>([.. Deck(gameId, handNo).Skip((seatNo - 1) * 2).Take(2)]);

        public ValueTask<IReadOnlyList<Card>> GetDeckCardsAsync(Guid gameId, int handNo, CancellationToken cancellationToken)
            => ValueTask.FromResult<IReadOnlyList<Card>>([.. Deck(gameId, handNo).Skip(20).Take(5)]);

        private Card[] Deck(Guid gameId, int handNo)
        {
            int order;
            lock (_lock)
            {
                if (!_gameOrder.TryGetValue(gameId, out order))
                {
                    order = _gameOrder[gameId] = _gameOrder.Count;
                }
            }

            var random = new Random(HashCode.Combine(seed, order, handNo));
            return [.. Phh.PhhCards.FullDeck.OrderBy(_ => random.Next())];
        }
    }

    /// <summary>One tournament, driven to its end, with the checks every call is followed by.</summary>
    private sealed class Session(IHoldemTable table, IHoldemTournament tournament, TestTimeProvider clock, int entryCount, int maxPlayersPerTable,
        AnteType anteType, Random random)
    {
        private readonly Guid _tournamentId = Guid.NewGuid();
        private readonly Dictionary<int, GameData> _games = [];
        private readonly Dictionary<string, decimal> _lastStacks = [];
        private readonly string _row = $"{entryCount} entries at {maxPlayersPerTable} per table, {anteType} ante";
        private TournamentData _data = null!;
        private int _hands;

        private IReadOnlyList<TournamentLevelData> Levels => _data.Options.Levels;

        internal async Task RunAsync()
        {
            await StartAsync();

            while (_data.State != TournamentState.Finished)
            {
                var progressed = false;
                foreach (var tableNo in _data.Tables.Where(candidate => !candidate.IsBroken).Select(candidate => candidate.TableNo).ToList())
                {
                    if (_data.State == TournamentState.Finished || _data.Tables.Single(candidate => candidate.TableNo == tableNo).IsBroken)
                    {
                        continue;
                    }

                    if (InProgress(_games[tableNo]))
                    {
                        (++_hands).Should().BeLessThan(MaxHands, Describe("the tournament should end within the step cap"));
                        await PlayOutHandAsync(tableNo);
                        clock.Advance(_timePerHand);
                        await DealAsync(tableNo);
                        progressed = true;
                    }
                    else if (_data.Entries.Any(entry => entry.State == TournamentEntryState.Moving && entry.TableNo == tableNo))
                    {
                        await DealAsync(tableNo);
                        progressed = true;
                    }
                }

                progressed.Should().BeTrue(Describe("a table with a hand in progress or players waiting to be seated must exist while the tournament runs"));
            }

            await AssertEndAsync();
        }

        private string Describe(string message) => $"[{_row}, seed {Seed}, seat draw seed {SeatDrawSeed}, hand {_hands}] {message}";

        private static bool InProgress(GameData game)
            => !game.IsFinished && game.HandData is { } hand && !hand.Seats.Any(seat => seat.HandResult is not null);

        private static decimal StackOf(SeatData seat) => seat.HandResult is { } result ? seat.StartHandStack + result.Net : seat.Stack;

        private async Task StartAsync()
        {
            var levels = _smallBlinds.Select((smallBlind, index) => new TournamentLevelData
            {
                SmallBlind = smallBlind,
                Ante = index == 0 ? 0 : anteType == AnteType.Classic ? smallBlind / 5 : smallBlind * 2,
                Duration = _levelDuration,
            }).ToList();
            var created = await tournament.ApplyAsync(TournamentAction.CreateTournament(new TournamentOptionData
            {
                TableFormat = maxPlayersPerTable switch
                {
                    2 => HoldemTableFormat.HeadsUp,
                    <= 6 => HoldemTableFormat.SixMax,
                    _ => HoldemTableFormat.FullRing,
                },
                MaxPlayersPerTable = maxPlayersPerTable,
                AnteType = anteType,
                StartingStack = StartingStack,
                BuyIn = BuyIn,
                MinEntries = entryCount,
                Payouts = entryCount == 2 ? [60, 40] : [50, 30, 20],
                Levels = levels,
            }, _tournamentId));
            created.IsSuccess.Should().BeTrue(string.Join(", ", created.Errors));

            for (var entry = 1; entry <= entryCount; entry++)
            {
                var registered = await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, $"Player {entry}"));
                registered.IsSuccess.Should().BeTrue(string.Join(", ", registered.Errors));
            }

            var started = await tournament.ApplyAsync(TournamentAction.StartTournament(_tournamentId, SeatDrawSeed));
            started.IsSuccess.Should().BeTrue(string.Join(", ", started.Errors));
            _data = started.Value;
            await RefreshGamesAsync();
            foreach (var game in _games.Values.Where(game => game.HandData is not null))
            {
                AssertDealtAtTheClockLevel(game.HandData!);
            }

            AssertInvariants("the start");
        }

        private async Task RefreshGamesAsync()
        {
            foreach (var tableData in _data.Tables.Where(candidate => !candidate.IsBroken))
            {
                var result = await table.ApplyAsync(HoldemAction.GetGame(tableData.GameId));
                result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
                _games[tableData.TableNo] = result.Value;
            }
        }

        /// <summary>The seeded policy of <see cref="MultiHandSessionTests"/>, with more shoves so that tournaments end.</summary>
        private async Task PlayOutHandAsync(int tableNo)
        {
            var gameId = _data.Tables.Single(candidate => candidate.TableNo == tableNo).GameId;
            for (var decisions = 0; InProgress(_games[tableNo]); decisions++)
            {
                decisions.Should().BeLessThan(MaxDecisionsPerHand, Describe($"table {tableNo}'s hand should end"));
                var hand = _games[tableNo].HandData!;
                var check = hand.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Check);
                var call = hand.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Call);
                var raise = hand.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);
                var streetRaised = hand.Actions.Any(action => action.Street == hand.Street
                    && action.ActionType is HandActionType.Raise or HandActionType.Bet or HandActionType.AllIn);
                var actor = hand.Seats.Single(seat => seat.SeatNo == hand.NextPlayerSeatNo);
                var canShove = raise != null || (call != null && actor.Stack <= call.MaxValue);

                HoldemActionPlay action;
                if (random.Next(ShoveOdds) == 0 && canShove)
                {
                    action = new HoldemActionPlay(gameId, PlayerActionType.AllIn, 0);
                }
                else if (raise != null && !streetRaised && random.Next(4) == 0)
                {
                    action = new HoldemActionPlay(gameId, PlayerActionType.Raise, raise.MinValue);
                }
                else if (check != null)
                {
                    action = new HoldemActionPlay(gameId, PlayerActionType.Check, 0);
                }
                else if (call != null && random.Next(5) > 0)
                {
                    action = new HoldemActionPlay(gameId, PlayerActionType.Call, 0);
                }
                else
                {
                    action = new HoldemActionPlay(gameId, PlayerActionType.Fold, 0);
                }

                var result = await table.ApplyAsync(action);
                result.IsSuccess.Should().BeTrue(Describe($"{action.ActionType} of {action.Size} by seat {hand.NextPlayerSeatNo} at table {tableNo} should be legal: {string.Join("; ", result.Errors)}"));

                // A play changes this table's game only, so the other tables' last reads are still current.
                _games[tableNo] = result.Value;
                AssertInvariants($"a play at table {tableNo}");
            }
        }

        private async Task DealAsync(int tableNo)
        {
            var before = _data;
            var gameBefore = _games[tableNo];
            var handNoBefore = gameBefore.HandData?.No ?? 0;
            var stacksBefore = new Dictionary<string, decimal>(_lastStacks);
            var unbrokenBefore = before.Tables.Count(candidate => !candidate.IsBroken);

            var result = await tournament.ApplyAsync(TournamentAction.DealTable(_tournamentId, tableNo));
            result.IsSuccess.Should().BeTrue(Describe($"DealTable({tableNo}) should succeed: {string.Join("; ", result.Errors)}"));
            _data = result.Value;
            await RefreshGamesAsync();

            AssertInvariants($"DealTable({tableNo})");
            AssertEliminations(tableNo, before, gameBefore);
            if (_data.State == TournamentState.Finished)
            {
                return;
            }

            var remaining = _data.Entries.Count(entry => entry.Place is null);
            var tablesNeeded = (remaining + maxPlayersPerTable - 1) / maxPlayersPerTable;
            var dealt = _data.Tables.Single(candidate => candidate.TableNo == tableNo);
            if (tablesNeeded < unbrokenBefore)
            {
                dealt.IsBroken.Should().BeTrue(Describe($"{remaining} players fit at {tablesNeeded} tables and {unbrokenBefore} were unbroken, so the table dealt breaks"));
                _data.Tables.Count(candidate => !candidate.IsBroken).Should().Be(unbrokenBefore - 1, Describe("one table breaks at a time"));
            }

            var unbroken = _data.Tables.Where(candidate => !candidate.IsBroken).ToList();
            unbroken.Count.Should().BeGreaterOrEqualTo(tablesNeeded, Describe("no more tables break than the players left allow"));
            if (!dealt.IsBroken)
            {
                var counts = unbroken.ToDictionary(candidate => candidate.TableNo, candidate => EffectiveCount(candidate.TableNo));
                counts[tableNo].Should().BeLessOrEqualTo(counts.Values.Min() + 1,
                    Describe($"table {tableNo} just dealt and differs from the smallest table by at most one, counting the players moving in: {string.Join(", ", counts)}"));
            }

            var newHand = _games[tableNo].HandData;
            if (newHand is not null && newHand.No > handNoBefore)
            {
                AssertDealtAtTheClockLevel(newHand);
            }

            // A player who moved is dealt in with exactly the stack it left its old table with.
            foreach (var entry in _data.Entries.Where(entry => entry.State == TournamentEntryState.Seated
                && before.Entries.Single(previous => previous.EntryNo == entry.EntryNo).State == TournamentEntryState.Moving))
            {
                var seat = _games[entry.TableNo!.Value].HandData!.Seats.Single(candidate => candidate.SeatNo == entry.SeatNo && !candidate.HasLeft);
                seat.StartHandStack.Should().Be(stacksBefore[entry.Name], Describe($"{entry.Name} moved to table {entry.TableNo} and is dealt in with the stack it carried"));
            }
        }

        private int EffectiveCount(int tableNo)
            => _data.Entries.Count(entry => entry.TableNo == tableNo && entry.Place is null && entry.State is TournamentEntryState.Seated or TournamentEntryState.Moving);

        /// <summary>Every hand is dealt at the level the clock shows when it is dealt, with that level's blinds and ante.</summary>
        private void AssertDealtAtTheClockLevel(HandData hand)
        {
            var end = _data.StartedAt!.Value;
            var expected = Levels.Count - 1;
            for (var index = 0; index < Levels.Count - 1; index++)
            {
                end += Levels[index].Duration;
                if (end > clock.UtcNow)
                {
                    expected = index;
                    break;
                }
            }

            _data.LevelIndex.Should().Be(expected, Describe("the tournament reads the level the clock gives"));
            hand.BlindLevel.Should().Be(expected, Describe($"hand {hand.No} is dealt at the clock's level"));
            hand.SmallBlind.Should().Be(Levels[expected].SmallBlind, Describe($"hand {hand.No} small blind"));
            hand.Ante.Should().Be(Levels[expected].Ante, Describe($"hand {hand.No} ante"));
        }

        /// <summary>
        /// Chips, places and seats, from what the tournament and its games report: the stacks of the seated players
        /// plus the pots in progress plus the stacks the moving players carry make every entry's starting stack, every
        /// player is in exactly one place, and a table's players are the tournament's players seated there.
        /// </summary>
        private void AssertInvariants(string after)
        {
            var what = Describe($"after {after}:");
            var tableNos = _data.Tables.Select(candidate => candidate.TableNo).ToHashSet();
            var remaining = _data.Entries.Where(entry => entry.Place is null).ToList();
            var placed = _data.Entries.Where(entry => entry.Place is not null).ToList();

            remaining.Where(entry => entry.State != TournamentEntryState.Seated && entry.State != TournamentEntryState.Moving).Should().BeEmpty(
                $"{what} a player without a place is seated or moving");
            placed.Where(entry => entry.State != TournamentEntryState.Eliminated && entry.State != TournamentEntryState.Winner).Should().BeEmpty(
                $"{what} a player with a place is out or the winner");
            (remaining.Count + placed.Count).Should().Be(entryCount, $"{what} every entry is in one place");

            foreach (var entry in remaining)
            {
                entry.TableNo.Should().NotBeNull($"{what} {entry.Name} holds a table");
                entry.SeatNo.Should().NotBeNull($"{what} {entry.Name} holds a seat");
                tableNos.Should().Contain(entry.TableNo!.Value);
                _data.Tables.Single(candidate => candidate.TableNo == entry.TableNo).IsBroken.Should().BeFalse($"{what} {entry.Name} is not at a broken table");
            }

            remaining.Select(entry => (entry.TableNo, entry.SeatNo)).Should().OnlyHaveUniqueItems($"{what} a seat is held or reserved by one player at most");

            if (_data.State == TournamentState.Finished)
            {
                // Nobody is left to play: the winner's stack is checked at the end, from the last read before the finish.
                remaining.Should().BeEmpty($"{what} a finished tournament has no player without a place");
                return;
            }

            var total = 0M;
            var seatedNames = new List<string>();
            foreach (var tableData in _data.Tables.Where(candidate => !candidate.IsBroken))
            {
                var seated = remaining.Where(entry => entry.TableNo == tableData.TableNo && entry.State == TournamentEntryState.Seated).ToList();
                tableData.PlayerCount.Should().Be(seated.Count, $"{what} table {tableData.TableNo}'s count is the players seated there");
                tableData.PlayerCount.Should().BeLessOrEqualTo(maxPlayersPerTable, $"{what} table {tableData.TableNo} has seats for {maxPlayersPerTable}");

                var game = _games[tableData.TableNo];
                if (game.HandData is not { } hand)
                {
                    foreach (var entry in seated)
                    {
                        _lastStacks[entry.Name] = StartingStack;
                        total += StartingStack;
                    }

                    continue;
                }

                var inGame = hand.Seats.Where(seat => !seat.HasLeft).ToList();
                inGame.Select(seat => (seat.SeatNo, seat.Name)).Should().BeEquivalentTo(seated.Select(entry => (entry.SeatNo!.Value, entry.Name)),
                    $"{what} the players in table {tableData.TableNo}'s game are the tournament's players seated there");
                seatedNames.AddRange(inGame.Select(seat => seat.Name));
                foreach (var seat in inGame)
                {
                    _lastStacks[seat.Name] = StackOf(seat);
                    total += StackOf(seat);
                }

                if (InProgress(game))
                {
                    total += hand.Pot + hand.StreetPot;
                }
            }

            seatedNames.Should().OnlyHaveUniqueItems($"{what} a player sits in one game at most");
            total += remaining.Where(entry => entry.State == TournamentEntryState.Moving).Sum(entry => _lastStacks[entry.Name]);
            total.Should().Be(entryCount * StartingStack, $"{what} no chip is created or lost, and there is no rake");
        }

        /// <summary>
        /// The players a table's hand busted are exactly the ones placed, in order of their stacks at the start of that
        /// hand, equal stacks tied at the best place of the group, and a tied group shares the prizes of its places.
        /// </summary>
        private void AssertEliminations(int tableNo, TournamentData before, GameData gameBefore)
        {
            var what = Describe($"DealTable({tableNo}) eliminations:");
            var previousPlace = before.Entries.ToDictionary(entry => entry.EntryNo, entry => entry.Place);
            var newlyPlaced = _data.Entries.Where(entry => entry.Place is not null && previousPlace[entry.EntryNo] is null).ToList();
            var busted = newlyPlaced.Where(entry => entry.State == TournamentEntryState.Eliminated).ToList();
            var seatsBefore = gameBefore.HandData?.Seats.ToDictionary(seat => seat.Name) ?? [];

            var shouldBeOut = before.Entries.Where(entry => entry.Place is null && entry.TableNo == tableNo && entry.State == TournamentEntryState.Seated
                && seatsBefore.TryGetValue(entry.Name, out var seat) && StackOf(seat) < Epsilon).Select(entry => entry.EntryNo);
            busted.Select(entry => entry.EntryNo).Should().BeEquivalentTo(shouldBeOut, $"{what} the players left without chips at the table are out, and only they");

            var remainingBefore = before.Entries.Count(entry => entry.Place is null);
            var firstPlace = remainingBefore - busted.Count + 1;
            foreach (var entry in busted)
            {
                var start = seatsBefore[entry.Name].StartHandStack;
                var expectedPlace = firstPlace + busted.Count(other => seatsBefore[other.Name].StartHandStack > start);
                entry.Place.Should().Be(expectedPlace, $"{what} {entry.Name} started the hand with {start}");
            }

            if (newlyPlaced.Any(entry => entry.State == TournamentEntryState.Winner))
            {
                (remainingBefore - busted.Count).Should().Be(1, $"{what} the winner is the last one left");
                _data.State.Should().Be(TournamentState.Finished);
            }
            else
            {
                (remainingBefore - busted.Count).Should().BeGreaterThan(1, $"{what} the tournament goes on with two or more left");
                _data.State.Should().NotBe(TournamentState.Finished);
            }

            foreach (var group in busted.GroupBy(entry => entry.Place!.Value))
            {
                var places = Enumerable.Range(group.Key, group.Count()).ToList();
                var pool = _data.PrizePool;
                var covered = places.Sum(place => place <= _data.Options.Payouts.Count ? Math.Round(pool * _data.Options.Payouts[place - 1] / 100, 2, MidpointRounding.AwayFromZero) : 0);
                group.Sum(entry => entry.Prize).Should().Be(covered, $"{what} the group at place {group.Key} shares the prizes of the places it covers");
                var share = Math.Floor(covered * 100 / group.Count()) / 100;
                group.Where(entry => entry.Prize != share && entry.Prize != share + 0.01M).Should().BeEmpty($"{what} an equal share to the cent");
            }
        }

        private async Task AssertEndAsync()
        {
            var what = Describe("at the end:");
            _data.State.Should().Be(TournamentState.Finished);
            _data.FinishedAt.Should().NotBeNull();

            var placesSorted = _data.Entries.Select(entry => entry.Place).ToList();
            placesSorted.Should().NotContainNulls($"{what} every player has a place");
            var places = placesSorted.Select(place => place!.Value).Order().ToList();
            for (var index = 0; index < places.Count; index++)
            {
                // Each place is given once, except that a tied group shares the best place of the places it covers.
                (places[index] == index + 1 || (index > 0 && places[index] == places[index - 1])).Should().BeTrue(
                    $"{what} places are 1..{entryCount} each once except declared ties, got {string.Join(",", places)}");
                places[index].Should().BeLessOrEqualTo(index + 1);
            }

            places[0].Should().Be(1);
            _data.Entries.Where(entry => entry.Place == 1).Should().ContainSingle().Which.State.Should().Be(TournamentEntryState.Winner);
            _data.Entries.Sum(entry => entry.Prize).Should().Be(_data.PrizePool, $"{what} the prizes add up to the pool");
            _data.PrizePool.Should().Be(entryCount * BuyIn);

            var winner = _data.Entries.Single(entry => entry.State == TournamentEntryState.Winner);
            _lastStacks[winner.Name].Should().Be(entryCount * StartingStack, $"{what} the winner holds every chip");

            foreach (var tableData in _data.Tables)
            {
                var result = await table.ApplyAsync(HoldemAction.GetGame(tableData.GameId));
                result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
                result.Value.IsFinished.Should().BeTrue($"{what} table {tableData.TableNo}'s game is finished");
            }
        }
    }
}
