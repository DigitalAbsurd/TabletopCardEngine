using FluentAssertions;
using FluentAssertions.Execution;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Tests.Persistence;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Plays one long game and checks what a single recorded hand cannot: that chips carry from hand to
/// hand, that the button walks round the table, and that nothing leaks in the ledger. The corpus
/// replay in <see cref="PhhReplayTests"/> covers the inside of a hand exhaustively but resets stacks
/// every time, because the histories it replays do.
/// </summary>
/// <remarks>
/// There is no external oracle here — the invariants are the oracle, which is why the action policy
/// only ever checks, calls, folds or makes the smallest legal raise on an unraised street, and never re-raises.
/// Bet sizing is already covered by the thousands of recorded raises the PHH replay drives through the engine.
/// <para>
/// Run at two table sizes. Heads-up is not a shorter ring but a different one — the button posts the
/// small blind and the acting order flips after the pre-flop — and it is the only shape where the
/// same two seats meet every hand, so the button has to alternate rather than walk.
/// </para>
/// </remarks>
[TestClass]
public sealed class MultiHandSessionTests
{
    private const int HandCount = 200;
    private const decimal SmallBlind = 10;
    private const decimal BigBlind = SmallBlind * 2;
    private const decimal BuyIn = 10_000;
    private const byte FirstDealerSeatNo = 1;

    /// <summary>A backstop, so a stuck hand fails the test instead of looping forever.</summary>
    private const int MaxDecisionsPerHand = 200;

    // The churn policy: a seated player leaves one hand in LeaveOdds, an empty seat is taken one hand in
    // JoinOdds, and at ExodusHandNo everybody but one player leaves, so the table is certain to run dry.
    private const int LeaveOdds = 12;
    private const int JoinOdds = 3;
    private const int ExodusHandNo = 60;
    private const int MinBuyInBigBlinds = 50;
    private const int MaxBuyInBigBlinds = 200;

    // The top-up policy: blinds 1/2, every join at 100 inside a 50 to 200 buy-in window, two rebuys a player.
    private const decimal TopUpBlind = 1;
    private const decimal TopUpBuyIn = 100;
    private const decimal TopUpMinBuyIn = 50;
    private const decimal TopUpMaxBuyIn = 200;
    private const int MaxRebuys = 2;
    private const double RebuyChance = 0.7;
    private const double TopUpChance = 0.3;
    private const double KickChance = 0.05;
    private const int ShoveOdds = 10;

    // The blind schedule policy: blinds 1/2 doubling every 20 hands up to a small blind of 16, buy-ins of 1000.
    private const decimal ScheduleInitialSmallBlind = 1;
    private const decimal ScheduleMaxSmallBlind = 16;
    private const decimal ScheduleBuyIn = 1000;
    private const int ScheduleIncreaseEveryHands = 20; // the invariant side; the game options below spell the 20 out so a probe can change one without the other

    /// <summary>Fixed so a failure is reproducible.</summary>
    private const int Seed = 20260825;

    [TestMethod]
    [DataRow(6, TestDatabaseKind.Sqlite, DisplayName = "six-handed, Sqlite")]
    [DataRow(2, TestDatabaseKind.Sqlite, DisplayName = "heads-up, Sqlite")]
    [DataRow(9, TestDatabaseKind.Sqlite, DisplayName = "nine-handed, Sqlite")]
    [DataRow(10, TestDatabaseKind.Sqlite, DisplayName = "ten-handed, Sqlite")]
    [DataRow(6, TestDatabaseKind.InMemory, DisplayName = "six-handed, InMemory")]
    [DataRow(2, TestDatabaseKind.InMemory, DisplayName = "heads-up, InMemory")]
    [DataRow(9, TestDatabaseKind.InMemory, DisplayName = "nine-handed, InMemory")]
    [DataRow(10, TestDatabaseKind.InMemory, DisplayName = "ten-handed, InMemory")]
    [DataRow(6, TestDatabaseKind.PostgreSql, DisplayName = "six-handed, PostgreSql")]
    [DataRow(2, TestDatabaseKind.PostgreSql, DisplayName = "heads-up, PostgreSql")]
    [DataRow(9, TestDatabaseKind.PostgreSql, DisplayName = "nine-handed, PostgreSql")]
    [DataRow(10, TestDatabaseKind.PostgreSql, DisplayName = "ten-handed, PostgreSql")]
    [DataRow(6, TestDatabaseKind.SqlServer, DisplayName = "six-handed, SqlServer")]
    [DataRow(2, TestDatabaseKind.SqlServer, DisplayName = "heads-up, SqlServer")]
    [DataRow(9, TestDatabaseKind.SqlServer, DisplayName = "nine-handed, SqlServer")]
    [DataRow(10, TestDatabaseKind.SqlServer, DisplayName = "ten-handed, SqlServer")]
    public async Task MultiHandSession_KeepsTheLedgerBalanced(int seatCount, TestDatabaseKind database)
    {
        var gameId = Guid.Parse("{22222222-2222-2222-2222-222222222222}");
        var random = new Random(Seed);
        var cardsByHand = new Dictionary<int, (Dictionary<byte, Card[]> Hole, Card[] Board)>();

        using var host = TestEngineHost.Setup(out var holdemEngine, out _,
            shuffler: new TestCardShuffler(
                getHoleFunc: (handNo, seatNo) => cardsByHand[handNo].Hole[seatNo],
                getDeckFunc: handNo => cardsByHand[handNo].Board),
            database: database);

        (await holdemEngine.ApplyAsync(new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData { TableFormat = GetTableFormat(seatCount), MaxPlayersCount = seatCount, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = FirstDealerSeatNo }))).IsSuccess.Should().BeTrue();

        var joinSeats = Enumerable.Range(1, seatCount)
            .Select(seatNo => new PlayerJoinData($"Seat{seatNo}", (byte)seatNo, BuyIn))
            .ToArray();

        // Stacks the previous hand ended on, which the next hand must start from.
        var expectedStartStacks = joinSeats.ToDictionary(seat => seat.SeatNo, seat => seat.BuyInAmount);
        byte? previousDealerSeatNo = null;

        for (var handNo = 1; handNo <= HandCount; handNo++)
        {
            cardsByHand[handNo] = DealCards(random, seatCount);

            using var scope = new AssertionScope($"{seatCount}-handed session hand #{handNo}");

            HoldemAction startAction = handNo == 1
                ? new HoldemActionStartGame(gameId, joinSeats)
                : new HoldemActionStartHand(gameId);
            var startResult = await holdemEngine.ApplyAsync(startAction);
            startResult.IsSuccess.Should().BeTrue(
                $"hand #{handNo} should start: {string.Join("; ", startResult.Errors.Select(error => error.Message))}");

            var gameData = startResult.Value;
            var handData = gameData.HandData!;

            handData.No.Should().Be(handNo);
            AssertButtonWalksRound(handData, previousDealerSeatNo, seatCount);
            AssertBlindSeats(handData, seatCount);

            foreach (var seatData in handData.Seats)
            {
                seatData.StartHandStack.Should().Be(expectedStartStacks[seatData.SeatNo],
                    $"seat {seatData.SeatNo} should start on what it finished the previous hand with");
            }

            handData.Seats.Sum(seat => seat.StartHandStack).Should()
                .Be(seatCount * BuyIn, "nobody buys in again, so the chips on the table never change");

            gameData = await PlayOutHandAsync(holdemEngine, gameId, gameData, random);
            handData = gameData.HandData!;

            handData.Seats.Sum(seat => seat.HandResult?.Net ?? 0).Should()
                .Be(0, "a hand neither creates nor destroys chips");

            foreach (var seatData in handData.Seats)
            {
                seatData.HandResult.Should().NotBeNull($"seat {seatData.SeatNo} should have a result");
                expectedStartStacks[seatData.SeatNo] = seatData.StartHandStack + (seatData.HandResult?.Net ?? 0);

                // Deliberately more than "not negative": the session is sized (deep stacks, small
                // blinds, minimum raises only) so that nobody can ever bust. A seat sitting on
                // nothing would take the game somewhere the engine has no answer for — it has no
                // rebuy or kick-off.
                expectedStartStacks[seatData.SeatNo].Should().BePositive(
                    $"seat {seatData.SeatNo} should never run out of chips in this session");
            }

            previousDealerSeatNo = handData.DealerSeatNo;
        }
    }

    /// <summary>
    /// The first session again with blinds that double every 20 hands from 1/2, capped at a small blind of 16.
    /// Rising blinds change who can afford them and how fast stacks bust, so a seat that runs dry sits out
    /// and the session ends once fewer than two seats have chips. The invariants are the first test's, plus the
    /// schedule's: the blinds of every hand follow the level, which follows the hand number, and never fall.
    /// </summary>
    [TestMethod]
    [DataRow(6, TestDatabaseKind.Sqlite, DisplayName = "six-handed blind schedule, Sqlite")]
    [DataRow(2, TestDatabaseKind.Sqlite, DisplayName = "heads-up blind schedule, Sqlite")]
    [DataRow(6, TestDatabaseKind.InMemory, DisplayName = "six-handed blind schedule, InMemory")]
    [DataRow(2, TestDatabaseKind.InMemory, DisplayName = "heads-up blind schedule, InMemory")]
    [DataRow(6, TestDatabaseKind.PostgreSql, DisplayName = "six-handed blind schedule, PostgreSql")]
    [DataRow(2, TestDatabaseKind.PostgreSql, DisplayName = "heads-up blind schedule, PostgreSql")]
    [DataRow(6, TestDatabaseKind.SqlServer, DisplayName = "six-handed blind schedule, SqlServer")]
    [DataRow(2, TestDatabaseKind.SqlServer, DisplayName = "heads-up blind schedule, SqlServer")]
    public async Task MultiHandSession_WithBlindSchedule_KeepsTheLedgerBalanced(int seatCount, TestDatabaseKind database)
    {
        var gameId = Guid.Parse("{55555555-5555-5555-5555-555555555555}");
        var random = new Random(Seed + 2000 + seatCount);
        var cardsByHand = new Dictionary<int, (Dictionary<byte, Card[]> Hole, Card[] Board)>();

        using var host = TestEngineHost.Setup(out var holdemEngine, out _,
            shuffler: new TestCardShuffler(
                getHoleFunc: (handNo, seatNo) => cardsByHand[handNo].Hole[seatNo],
                getDeckFunc: handNo => cardsByHand[handNo].Board),
            database: database);

        (await holdemEngine.ApplyAsync(new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData
            {
                TableFormat = GetTableFormat(seatCount),
                MaxPlayersCount = seatCount,
                InitialSmallBlind = ScheduleInitialSmallBlind,
                InitialDealerSeatNo = FirstDealerSeatNo,
                BlindSchedule = new BlindScheduleData
                {
                    IncreaseEveryHands = 20,
                    Mode = BlindIncreaseMode.Multiplier,
                    Multiplier = 2,
                    MaxSmallBlind = ScheduleMaxSmallBlind
                }
            }))).IsSuccess.Should().BeTrue();

        var joinSeats = Enumerable.Range(1, seatCount)
            .Select(seatNo => new PlayerJoinData($"Seat{seatNo}", (byte)seatNo, ScheduleBuyIn))
            .ToArray();

        var stacks = joinSeats.ToDictionary(seat => seat.SeatNo, seat => seat.BuyInAmount);
        byte? previousDealerSeatNo = null;
        var previousSmallBlind = 0M;
        var handsPlayed = 0;

        for (var handNo = 1; handNo <= HandCount; handNo++)
        {
            var dealtIn = stacks.Where(stack => stack.Value > 0).Select(stack => stack.Key).Order().ToArray();
            if (dealtIn.Length < 2)
            {
                break;
            }

            cardsByHand[handNo] = DealCards(random, seatCount);

            using var scope = new AssertionScope($"{seatCount}-handed blind schedule session hand #{handNo}");

            HoldemAction startAction = handNo == 1
                ? new HoldemActionStartGame(gameId, joinSeats)
                : new HoldemActionStartHand(gameId);
            var startResult = await holdemEngine.ApplyAsync(startAction);
            startResult.IsSuccess.Should().BeTrue(
                $"hand #{handNo} should start: {string.Join("; ", startResult.Errors.Select(error => error.Message))}");

            var gameData = startResult.Value;
            var handData = gameData.HandData!;
            handsPlayed++;

            handData.No.Should().Be(handNo);
            AssertScheduleButtonAndBlindSeats(handData, dealtIn, stacks, previousDealerSeatNo, seatCount);
            AssertBlindSchedule(handData, previousSmallBlind);
            previousSmallBlind = handData.SmallBlind;

            handData.Seats.Select(seat => seat.SeatNo).Should().Contain(dealtIn, "every seat with chips is at the table; a busted one may still sit there out");
            foreach (var seatData in handData.Seats)
            {
                seatData.StartHandStack.Should().Be(stacks[seatData.SeatNo],
                    $"seat {seatData.SeatNo} should start on what it finished the previous hand with");
            }

            gameData = await PlayOutHandAsync(holdemEngine, gameId, gameData, random);
            handData = gameData.HandData!;

            handData.Seats.Sum(seat => seat.HandResult?.Net ?? 0).Should()
                .Be(0, "a hand neither creates nor destroys chips");

            foreach (var seatData in handData.Seats)
            {
                seatData.HandResult.Should().NotBeNull($"seat {seatData.SeatNo} should have a result");
                stacks[seatData.SeatNo] = seatData.StartHandStack + (seatData.HandResult?.Net ?? 0);
                stacks[seatData.SeatNo].Should().BeGreaterOrEqualTo(0, $"seat {seatData.SeatNo} cannot end below nothing");
            }

            stacks.Values.Sum().Should().Be(seatCount * ScheduleBuyIn, "nobody buys in again, so the chips on the table never change");

            previousDealerSeatNo = handData.DealerSeatNo;
        }

        handsPlayed.Should().BeGreaterThan(ScheduleIncreaseEveryHands, "the session must outlive the first level");
    }

    /// <summary>
    /// The button moves to the next seat with chips. Who posts is walked from the stacks: a seat that cannot
    /// cover the small blind, or the big blind, is stepped over and sits the hand out, and with only two seats
    /// funded the button posts the small blind.
    /// </summary>
    private static void AssertScheduleButtonAndBlindSeats(HandData handData, byte[] dealtIn, Dictionary<byte, decimal> stacks, byte? previousDealerSeatNo, int seatCount)
    {
        var expectedDealerSeatNo = previousDealerSeatNo == null
            ? FirstDealerSeatNo
            : NextDealtInSeatNo(dealtIn, previousDealerSeatNo.Value, seatCount);
        handData.DealerSeatNo.Should().Be(expectedDealerSeatNo, "the button moves to the next seat with chips");

        byte expectedSmallBlindSeatNo;
        byte expectedBigBlindSeatNo;
        if (dealtIn.Length == 2)
        {
            expectedSmallBlindSeatNo = handData.DealerSeatNo;
            expectedBigBlindSeatNo = NextDealtInSeatNo(dealtIn, handData.DealerSeatNo, seatCount);
        }
        else
        {
            expectedSmallBlindSeatNo = NextSeatNoWithAtLeast(stacks, handData.DealerSeatNo, seatCount, handData.SmallBlind);
            expectedBigBlindSeatNo = NextSeatNoWithAtLeast(stacks, expectedSmallBlindSeatNo, seatCount, handData.BigBlind, fallback: expectedSmallBlindSeatNo);
        }

        handData.SmallBlindSeatNo.Should().Be(expectedSmallBlindSeatNo, "where the small blind sits");
        handData.BigBlindSeatNo.Should().Be(expectedBigBlindSeatNo, "where the big blind sits");
    }

    private static byte NextSeatNoWithAtLeast(Dictionary<byte, decimal> stacks, byte afterSeatNo, int seatCount, decimal amount, byte fallback = 0)
    {
        for (var step = 1; step < seatCount; step++)
        {
            var seatNo = (byte)((afterSeatNo - 1 + step) % seatCount + 1);
            if (stacks[seatNo] >= amount)
            {
                return seatNo;
            }
        }

        return fallback;
    }

    private static void AssertBlindSchedule(HandData handData, decimal previousSmallBlind)
    {
        var expectedLevel = (handData.No - 1) / ScheduleIncreaseEveryHands;
        handData.BlindLevel.Should().Be(expectedLevel, "the only trigger is every set number of hands");
        handData.BigBlind.Should().Be(2 * handData.SmallBlind, "the big blind is twice the small one");
        handData.SmallBlind.Should().Be((decimal)Math.Min(Math.Pow(2, handData.BlindLevel), (double)ScheduleMaxSmallBlind),
            "the small blind doubles with every level up to the cap");
        handData.SmallBlind.Should().BeGreaterOrEqualTo(previousSmallBlind, "the blinds never fall");

        var smallBlindRow = handData.Actions.Single(action => action.ActionType == HandActionType.SmallBlind);
        smallBlindRow.Size.Should().Be(handData.SmallBlind, "the ledger keeps the nominal small blind");
        smallBlindRow.Amount.Should().BeLessOrEqualTo(handData.SmallBlind, "a blind post is capped at the stack");

        var bigBlindRow = handData.Actions.Single(action => action.ActionType == HandActionType.BigBlind);
        bigBlindRow.Size.Should().Be(handData.BigBlind, "the ledger keeps the nominal big blind");
        bigBlindRow.Amount.Should().BeLessOrEqualTo(handData.BigBlind, "a blind post is capped at the stack");
    }

    /// <summary>
    /// The same 200-hand session with the table's membership changing between hands: players leave
    /// (a busted one always does), new players take the empty seats with a buy-in of their own, and once
    /// a scripted exodus leaves one player at the table the next hand cannot start until someone joins.
    /// The oracle is the invariants — chips bought in equal chips on the table plus chips taken away,
    /// and the button and blinds walk clockwise over the seats that were dealt in. The expected seats
    /// are walked in the test, not taken from the engine's own table helpers.
    /// </summary>
    [TestMethod]
    [DataRow(6, TestDatabaseKind.Sqlite, DisplayName = "six-handed churn, Sqlite")]
    [DataRow(10, TestDatabaseKind.Sqlite, DisplayName = "ten-handed churn, Sqlite")]
    [DataRow(6, TestDatabaseKind.InMemory, DisplayName = "six-handed churn, InMemory")]
    [DataRow(10, TestDatabaseKind.InMemory, DisplayName = "ten-handed churn, InMemory")]
    [DataRow(6, TestDatabaseKind.PostgreSql, DisplayName = "six-handed churn, PostgreSql")]
    [DataRow(10, TestDatabaseKind.PostgreSql, DisplayName = "ten-handed churn, PostgreSql")]
    [DataRow(6, TestDatabaseKind.SqlServer, DisplayName = "six-handed churn, SqlServer")]
    [DataRow(10, TestDatabaseKind.SqlServer, DisplayName = "ten-handed churn, SqlServer")]
    public async Task MultiHandSession_WithPlayersJoiningAndLeaving_KeepsTheLedgerBalanced(int seatCount, TestDatabaseKind database)
    {
        var gameId = Guid.Parse("{33333333-3333-3333-3333-333333333333}");
        var random = new Random(Seed + seatCount);
        var cardsByHand = new Dictionary<int, (Dictionary<byte, Card[]> Hole, Card[] Board)>();

        using var host = TestEngineHost.Setup(out var holdemEngine, out _,
            shuffler: new TestCardShuffler(
                getHoleFunc: (handNo, seatNo) => cardsByHand[handNo].Hole[seatNo],
                getDeckFunc: handNo => cardsByHand[handNo].Board),
            database: database);

        (await holdemEngine.ApplyAsync(new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData { TableFormat = GetTableFormat(seatCount), MaxPlayersCount = seatCount, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = FirstDealerSeatNo }))).IsSuccess.Should().BeTrue();

        var allSeatNos = Enumerable.Range(1, seatCount).Select(seatNo => (byte)seatNo).ToArray();

        // What the test believes about the table: the stack of every seat with a player in it.
        var stacks = new Dictionary<byte, decimal>();
        var names = new Dictionary<byte, string>();
        var everSeated = new HashSet<byte>();
        var totalBuyIns = 0M;
        var totalTakenAway = 0M;
        var joinerCount = 0;
        var leaves = 0;
        var rejoins = 0;
        var pauses = 0;
        var pauseChecked = false;
        byte? previousDealerSeatNo = null;

        PlayerJoinData Join(byte seatNo, string name)
        {
            var buyIn = random.Next(MinBuyInBigBlinds, MaxBuyInBigBlinds + 1) * BigBlind;
            stacks[seatNo] = buyIn;
            names[seatNo] = name;
            totalBuyIns += buyIn;
            if (!everSeated.Add(seatNo))
            {
                rejoins++;
            }

            return new PlayerJoinData(name, seatNo, buyIn);
        }

        for (var handNo = 1; handNo <= HandCount; handNo++)
        {
            cardsByHand[handNo] = DealCards(random, seatCount);

            using var scope = new AssertionScope($"{seatCount}-handed churn session hand #{handNo}");

            HoldemAction startAction;
            if (handNo == 1)
            {
                startAction = new HoldemActionStartGame(gameId, [.. allSeatNos.Select(seatNo => Join(seatNo, $"Seat{seatNo}"))]);
            }
            else
            {
                var keepSeatNo = allSeatNos[random.Next(seatCount)];
                foreach (var seatNo in allSeatNos.Where(stacks.ContainsKey).ToArray())
                {
                    var shouldLeave = stacks[seatNo] <= 0
                        || (handNo == ExodusHandNo && seatNo != keepSeatNo)
                        || random.Next(LeaveOdds) == 0;
                    if (!shouldLeave)
                    {
                        continue;
                    }

                    var leaveResult = await holdemEngine.ApplyAsync(HoldemAction.LeaveSeat(gameId, seatNo));
                    leaveResult.IsSuccess.Should().BeTrue(
                        $"seat {seatNo} should be able to leave between hands: {string.Join("; ", leaveResult.Errors.Select(error => error.Message))}");
                    var leaver = leaveResult.Value.HandData!.Seats.Single(seat => seat.SeatNo == seatNo);
                    leaver.HasLeft.Should().BeTrue($"seat {seatNo} has left");
                    (leaver.StartHandStack + leaver.HandResult!.Net).Should().Be(stacks[seatNo],
                        "a leaver takes the stack its last hand ended on");

                    totalTakenAway += stacks[seatNo];
                    stacks.Remove(seatNo);
                    names.Remove(seatNo);
                    leaves++;
                }

                var emptySeatNos = allSeatNos.Where(seatNo => !stacks.ContainsKey(seatNo)).ToArray();
                var joiningSeatNos = emptySeatNos.Where(_ => random.Next(JoinOdds) == 0).ToList();

                if (stacks.Count < 2)
                {
                    // Fewer than two players are left, so nobody can be dealt in without a join.
                    pauses++;
                    if (!pauseChecked)
                    {
                        pauseChecked = true;
                        var before = await holdemEngine.ApplyAsync(HoldemAction.GetGame(gameId));
                        var stalled = await holdemEngine.ApplyAsync(HoldemAction.StartHand(gameId));
                        stalled.IsFailed.Should().BeTrue("a hand cannot be dealt to fewer than two players");
                        stalled.Errors.Single().Code.Should().Be(HoldemErrorCode.NotEnoughPlayers);
                        (await holdemEngine.ApplyAsync(HoldemAction.GetGame(gameId))).Value.Should().Be(before.Value,
                            "the refused hand saved nothing");
                    }

                    foreach (var seatNo in emptySeatNos.Except(joiningSeatNos).Take(2 - stacks.Count - joiningSeatNos.Count))
                    {
                        joiningSeatNos.Add(seatNo);
                    }
                }

                startAction = HoldemAction.StartHand(gameId,
                    [.. joiningSeatNos.Select(seatNo => Join(seatNo, $"Joiner {++joinerCount}"))]);
            }

            var startResult = await holdemEngine.ApplyAsync(startAction);
            startResult.IsSuccess.Should().BeTrue(
                $"hand #{handNo} should start: {string.Join("; ", startResult.Errors.Select(error => error.Message))}");

            var handData = startResult.Value.HandData!;
            handData.No.Should().Be(handNo);

            var dealtIn = allSeatNos.Where(seatNo => stacks.TryGetValue(seatNo, out var stack) && stack > 0).ToArray();
            AssertDealtInSeats(handData, dealtIn, stacks, names);
            AssertChurnButtonAndBlinds(handData, dealtIn, previousDealerSeatNo, seatCount);

            var gameData = await PlayOutHandAsync(holdemEngine, gameId, startResult.Value, random);
            handData = gameData.HandData!;

            handData.Seats.Sum(seat => seat.HandResult?.Net ?? 0).Should()
                .Be(0, "a hand neither creates nor destroys chips");

            foreach (var seatData in handData.Seats)
            {
                seatData.HandResult.Should().NotBeNull($"seat {seatData.SeatNo} should have a result");
                stacks[seatData.SeatNo] = seatData.StartHandStack + (seatData.HandResult?.Net ?? 0);
                stacks[seatData.SeatNo].Should().BeGreaterOrEqualTo(0, $"seat {seatData.SeatNo} cannot end below nothing");
            }

            (stacks.Values.Sum() + totalTakenAway).Should().Be(totalBuyIns,
                "every chip bought in is on the table or went away with a leaver");

            previousDealerSeatNo = handData.DealerSeatNo;
        }

        using var counters = new AssertionScope("the session must actually have churned");
        leaves.Should().BeGreaterThan(0, "somebody left");
        rejoins.Should().BeGreaterThan(0, "a seat somebody had left was taken by a new player");
        pauses.Should().BeGreaterThan(0, "the table was down to fewer than two players at least once");
        pauseChecked.Should().BeTrue("the first pause was put to the engine");
    }

    /// <summary>
    /// The churn session with buy-ins on top: a busted seat rebuys (until its player has used up the rebuys,
    /// then leaves), a short seat tops up, a shove now and then busts somebody, and seats are kicked both
    /// between hands and at a random decision mid-hand. Chips bought in, topped up and rebought must always
    /// equal chips on the table, pending, and taken away by whoever left or was kicked. The invariant is
    /// computed from what the engine reports, so a pending buy-in the engine loses breaks it.
    /// </summary>
    [TestMethod]
    [DataRow(6, TestDatabaseKind.Sqlite, DisplayName = "six-handed top-ups, Sqlite")]
    [DataRow(10, TestDatabaseKind.Sqlite, DisplayName = "ten-handed top-ups, Sqlite")]
    [DataRow(6, TestDatabaseKind.InMemory, DisplayName = "six-handed top-ups, InMemory")]
    [DataRow(10, TestDatabaseKind.InMemory, DisplayName = "ten-handed top-ups, InMemory")]
    [DataRow(6, TestDatabaseKind.PostgreSql, DisplayName = "six-handed top-ups, PostgreSql")]
    [DataRow(10, TestDatabaseKind.PostgreSql, DisplayName = "ten-handed top-ups, PostgreSql")]
    [DataRow(6, TestDatabaseKind.SqlServer, DisplayName = "six-handed top-ups, SqlServer")]
    [DataRow(10, TestDatabaseKind.SqlServer, DisplayName = "ten-handed top-ups, SqlServer")]
    public async Task MultiHandSession_WithTopUpsRebuysAndKicks_KeepsTheLedgerBalanced(int seatCount, TestDatabaseKind database)
    {
        var gameId = Guid.Parse("{44444444-4444-4444-4444-444444444444}");
        var random = new Random(Seed + 1000 + seatCount);
        var cardsByHand = new Dictionary<int, (Dictionary<byte, Card[]> Hole, Card[] Board)>();

        using var host = TestEngineHost.Setup(out var holdemEngine, out _,
            shuffler: new TestCardShuffler(
                getHoleFunc: (handNo, seatNo) => cardsByHand[handNo].Hole[seatNo],
                getDeckFunc: handNo => cardsByHand[handNo].Board),
            database: database);

        (await holdemEngine.ApplyAsync(new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData
            {
                TableFormat = GetTableFormat(seatCount),
                MaxPlayersCount = seatCount,
                InitialSmallBlind = TopUpBlind,
                InitialDealerSeatNo = FirstDealerSeatNo,
                AllowTopUp = true,
                AllowRebuy = true,
                MaxRebuys = MaxRebuys,
                MinBuyIn = TopUpMinBuyIn,
                MaxBuyIn = TopUpMaxBuyIn
            }))).IsSuccess.Should().BeTrue();

        var allSeatNos = Enumerable.Range(1, seatCount).Select(seatNo => (byte)seatNo).ToArray();

        // What the test believes about the table: one entry per seat with a player in it.
        var occupants = new Dictionary<byte, Occupant>();
        var kickedNames = new HashSet<string>();
        var totalIn = 0M;
        var totalTakenAway = 0M;
        var playerCount = 0;
        var topUps = 0;
        var rebuys = 0;
        var rebuysRefused = 0;
        var kicksBetween = 0;
        var kicksMid = 0;
        var stoppedEarly = false;
        byte? previousDealerSeatNo = null;

        PlayerJoinData Join(byte seatNo)
        {
            var occupant = new Occupant { Name = $"Player {++playerCount}", Stack = TopUpBuyIn };
            occupants[seatNo] = occupant;
            totalIn += TopUpBuyIn;
            return new PlayerJoinData(occupant.Name, seatNo, TopUpBuyIn);
        }

        async Task<GameData> ApplyOkAsync(HoldemAction action)
        {
            var result = await holdemEngine.ApplyAsync(action);
            result.IsSuccess.Should().BeTrue(
                $"{action.GetType().Name} should succeed: {string.Join("; ", result.Errors.Select(error => error.Message))}");
            return result.Value;
        }

        // Σ buy-ins + Σ accepted top-ups and rebuys == Σ stacks of seated players + Σ pending + Σ taken away,
        // all read from the engine's own report of the hand.
        void AssertConserved(HandData handData, string when)
        {
            var onTheTable = handData.Seats.Where(seat => !seat.HasLeft)
                .Sum(seat => seat.StartHandStack + (seat.HandResult?.Net ?? 0) + seat.PendingBuyIn);
            (onTheTable + totalTakenAway).Should().Be(totalIn,
                $"every chip put in is on the table, pending, or went away with a player {when}");
        }

        async Task RemoveAsync(HoldemAction action, byte seatNo)
        {
            var occupant = occupants[seatNo];
            var handData = (await ApplyOkAsync(action)).HandData!;
            var gone = handData.Seats.Single(seat => seat.SeatNo == seatNo);
            gone.HasLeft.Should().BeTrue($"seat {seatNo} has left");
            gone.PendingBuyIn.Should().Be(0, "a pending buy-in leaves with the player");
            (gone.StartHandStack + gone.HandResult!.Net).Should().Be(occupant.Stack, "a leaver takes the stack its last hand ended on");

            // A cancelled buy-in never went into play, so it goes back with the player as well.
            totalTakenAway += occupant.Stack + occupant.Pending;
            occupants.Remove(seatNo);
            AssertConserved(handData, $"after seat {seatNo} was removed");
        }

        for (var handNo = 1; handNo <= HandCount; handNo++)
        {
            cardsByHand[handNo] = DealCards(random, seatCount);

            using var scope = new AssertionScope($"{seatCount}-handed top-up session hand #{handNo}");

            var toppedUpSeatNos = new HashSet<byte>();
            HoldemAction startAction;
            if (handNo == 1)
            {
                startAction = new HoldemActionStartGame(gameId, [.. allSeatNos.Select(Join)]);
            }
            else
            {
                foreach (var seatNo in allSeatNos.Where(occupants.ContainsKey).ToArray())
                {
                    var occupant = occupants[seatNo];
                    if (occupant.Stack <= 0)
                    {
                        if (random.NextDouble() >= RebuyChance)
                        {
                            await RemoveAsync(HoldemAction.LeaveSeat(gameId, seatNo), seatNo);
                        }
                        else if (occupant.Rebuys < MaxRebuys)
                        {
                            var rebought = (await ApplyOkAsync(HoldemAction.TopUp(gameId, seatNo, TopUpBuyIn))).HandData!;
                            occupant.Pending += TopUpBuyIn;
                            occupant.Rebuys++;
                            totalIn += TopUpBuyIn;
                            rebuys++;
                            toppedUpSeatNos.Add(seatNo);
                            rebought.Seats.Single(seat => seat.SeatNo == seatNo).PendingBuyIn.Should().Be(occupant.Pending);
                            AssertConserved(rebought, $"after seat {seatNo} rebought");
                        }
                        else
                        {
                            var refused = await holdemEngine.ApplyAsync(HoldemAction.TopUp(gameId, seatNo, TopUpBuyIn));
                            refused.IsFailed.Should().BeTrue($"seat {seatNo}'s player has used up {MaxRebuys} rebuys");
                            refused.Errors.Single().Code.Should().Be(HoldemErrorCode.RebuyNotAllowed);
                            rebuysRefused++;
                            await RemoveAsync(HoldemAction.LeaveSeat(gameId, seatNo), seatNo);
                        }
                    }
                    else if (occupant.Stack < TopUpBuyIn && random.NextDouble() < TopUpChance)
                    {
                        var amount = TopUpMaxBuyIn - occupant.Stack;
                        var toppedUp = (await ApplyOkAsync(HoldemAction.TopUp(gameId, seatNo, amount))).HandData!;
                        occupant.Pending += amount;
                        totalIn += amount;
                        topUps++;
                        toppedUpSeatNos.Add(seatNo);
                        toppedUp.Seats.Single(seat => seat.SeatNo == seatNo).PendingBuyIn.Should().Be(occupant.Pending);
                        AssertConserved(toppedUp, $"after seat {seatNo} topped up");
                    }
                }

                if (occupants.Count > 0 && random.NextDouble() < KickChance)
                {
                    var seatNo = occupants.Keys.ElementAt(random.Next(occupants.Count));
                    kickedNames.Add(occupants[seatNo].Name);
                    await RemoveAsync(HoldemAction.KickSeat(gameId, seatNo), seatNo);
                    kicksBetween++;
                }

                var emptySeatNos = allSeatNos.Where(seatNo => !occupants.ContainsKey(seatNo)).ToList();
                var joiningSeatNos = emptySeatNos.Where(_ => random.Next(JoinOdds) == 0).ToList();
                foreach (var seatNo in emptySeatNos.Except(joiningSeatNos).Take(Math.Max(0, 2 - occupants.Count - joiningSeatNos.Count)))
                {
                    joiningSeatNos.Add(seatNo);
                }

                if (occupants.Count + joiningSeatNos.Count < 2)
                {
                    var stalled = await holdemEngine.ApplyAsync(HoldemAction.StartHand(gameId));
                    stalled.Errors.Single().Code.Should().Be(HoldemErrorCode.NotEnoughPlayers);
                    stoppedEarly = true;
                    break;
                }

                startAction = HoldemAction.StartHand(gameId, [.. joiningSeatNos.Select(Join)]);
            }

            var startResult = await holdemEngine.ApplyAsync(startAction);
            startResult.IsSuccess.Should().BeTrue(
                $"hand #{handNo} should start: {string.Join("; ", startResult.Errors.Select(error => error.Message))}");

            var handData = startResult.Value.HandData!;
            handData.No.Should().Be(handNo);

            var dealtIn = allSeatNos.Where(seatNo => occupants.TryGetValue(seatNo, out var occupant) && occupant.Stack + occupant.Pending > 0).ToArray();
            var startStacks = dealtIn.ToDictionary(seatNo => seatNo, seatNo => occupants[seatNo].Stack + occupants[seatNo].Pending);
            var names = dealtIn.ToDictionary(seatNo => seatNo, seatNo => occupants[seatNo].Name);
            AssertDealtInSeats(handData, dealtIn, startStacks, names);
            AssertChurnButtonAndBlinds(handData, dealtIn, previousDealerSeatNo, seatCount);
            handData.Seats.Select(seat => seat.Name).Should().NotIntersectWith(kickedNames, "a kicked player is never dealt in again");

            foreach (var seatData in handData.Seats)
            {
                var occupant = occupants[seatData.SeatNo];
                seatData.PendingBuyIn.Should().Be(0, "the pending buy-in went into the stack");
                seatData.Rebuys.Should().Be(occupant.Rebuys).And.BeLessOrEqualTo(MaxRebuys);
                if (toppedUpSeatNos.Contains(seatData.SeatNo))
                {
                    seatData.StartHandStack.Should().BeLessOrEqualTo(TopUpMaxBuyIn, "a top-up never takes a stack above the maximum buy-in");
                }

                occupant.Stack += occupant.Pending;
                occupant.Pending = 0;
            }

            AssertConserved(handData, "at the start of the hand");

            var kickDecisionNo = random.NextDouble() < KickChance ? random.Next(6) : -1;
            var kickedMid = new Dictionary<byte, bool>();
            var gameData = await PlayOutHandAsync(holdemEngine, gameId, startResult.Value, random, ShoveOdds,
                async (current, decisionNo) =>
                {
                    if (decisionNo != kickDecisionNo)
                    {
                        return current;
                    }

                    var candidates = current.HandData!.Seats.Where(seat => !seat.HasLeft).ToArray();
                    var victim = candidates[random.Next(candidates.Length)];
                    var kicked = await ApplyOkAsync(HoldemAction.KickSeat(gameId, victim.SeatNo));
                    kicked.HandData!.Seats.Single(seat => seat.SeatNo == victim.SeatNo).HasLeft.Should().BeTrue("a kicked seat has left");
                    kickedMid[victim.SeatNo] = victim.IsAllIn;
                    kickedNames.Add(victim.Name);
                    kicksMid++;
                    return kicked;
                });
            handData = gameData.HandData!;

            handData.Seats.Sum(seat => seat.HandResult?.Net ?? 0).Should()
                .Be(0, "a hand neither creates nor destroys chips");

            foreach (var seatData in handData.Seats)
            {
                seatData.HandResult.Should().NotBeNull($"seat {seatData.SeatNo} should have a result");
                var stack = seatData.StartHandStack + (seatData.HandResult?.Net ?? 0);
                stack.Should().BeGreaterOrEqualTo(0, $"seat {seatData.SeatNo} cannot end below nothing");
                occupants[seatData.SeatNo].Stack = stack;
                if (kickedMid.TryGetValue(seatData.SeatNo, out var wasAllIn))
                {
                    seatData.HasLeft.Should().BeTrue($"seat {seatData.SeatNo} was kicked");
                    (wasAllIn || seatData.IsFolded).Should().BeTrue(
                        $"kicked seat {seatData.SeatNo} ends the hand folded or, when it was all in, with its result");
                }
            }

            foreach (var seatData in handData.Seats.Where(seat => seat.HasLeft))
            {
                totalTakenAway += occupants[seatData.SeatNo].Stack;
                occupants.Remove(seatData.SeatNo);
            }

            AssertConserved(handData, "at the end of the hand");

            previousDealerSeatNo = handData.DealerSeatNo;
        }

        using var counters = new AssertionScope("the session must actually have used every buy-in path");
        stoppedEarly.Should().BeFalse("a free seat can always be filled, so the session plays all hands");
        topUps.Should().BeGreaterThan(0, "somebody topped up");
        rebuys.Should().BeGreaterThan(0, "somebody rebought");
        rebuysRefused.Should().BeGreaterThan(0, "somebody asked for a third rebuy");
        kicksBetween.Should().BeGreaterThan(0, "somebody was kicked between hands");
        kicksMid.Should().BeGreaterThan(0, "somebody was kicked mid-hand");
    }

    /// <summary>
    /// The first session again under Pot-Limit and Fixed-Limit, with a policy that re-raises and sometimes
    /// raises to the maximum, so the Fixed-Limit cap and the Pot-Limit maximum are reached. Some buy-ins are 5 to 20
    /// big blinds, and a seat that falls below one big blind is topped up or rebought, so short stacks and
    /// all-ins keep happening while every seat is still dealt in. Besides the first test's invariants, every
    /// hand's ledger is checked against the structure's own limits, computed here from the ledger alone.
    /// </summary>
    [TestMethod]
    [DataRow(BettingStructure.PotLimit, 6, TestDatabaseKind.Sqlite, DisplayName = "Pot-Limit six-handed, Sqlite")]
    [DataRow(BettingStructure.PotLimit, 2, TestDatabaseKind.Sqlite, DisplayName = "Pot-Limit heads-up, Sqlite")]
    [DataRow(BettingStructure.PotLimit, 10, TestDatabaseKind.Sqlite, DisplayName = "Pot-Limit ten-handed, Sqlite")]
    [DataRow(BettingStructure.PotLimit, 6, TestDatabaseKind.InMemory, DisplayName = "Pot-Limit six-handed, InMemory")]
    [DataRow(BettingStructure.PotLimit, 2, TestDatabaseKind.InMemory, DisplayName = "Pot-Limit heads-up, InMemory")]
    [DataRow(BettingStructure.PotLimit, 6, TestDatabaseKind.PostgreSql, DisplayName = "Pot-Limit six-handed, PostgreSql")]
    [DataRow(BettingStructure.PotLimit, 6, TestDatabaseKind.SqlServer, DisplayName = "Pot-Limit six-handed, SqlServer")]
    [DataRow(BettingStructure.FixedLimit, 6, TestDatabaseKind.Sqlite, DisplayName = "Fixed-Limit six-handed, Sqlite")]
    [DataRow(BettingStructure.FixedLimit, 2, TestDatabaseKind.Sqlite, DisplayName = "Fixed-Limit heads-up, Sqlite")]
    [DataRow(BettingStructure.FixedLimit, 10, TestDatabaseKind.Sqlite, DisplayName = "Fixed-Limit ten-handed, Sqlite")]
    [DataRow(BettingStructure.FixedLimit, 6, TestDatabaseKind.InMemory, DisplayName = "Fixed-Limit six-handed, InMemory")]
    [DataRow(BettingStructure.FixedLimit, 2, TestDatabaseKind.InMemory, DisplayName = "Fixed-Limit heads-up, InMemory")]
    [DataRow(BettingStructure.FixedLimit, 6, TestDatabaseKind.PostgreSql, DisplayName = "Fixed-Limit six-handed, PostgreSql")]
    [DataRow(BettingStructure.FixedLimit, 6, TestDatabaseKind.SqlServer, DisplayName = "Fixed-Limit six-handed, SqlServer")]
    public async Task MultiHandSession_LimitStructures_KeepTheLedgerBalanced(BettingStructure structure, int seatCount, TestDatabaseKind database)
    {
        var gameId = Guid.Parse("{66666666-6666-6666-6666-666666666666}");
        var random = new Random(Seed + 3000 + seatCount + (int)structure);
        var cardsByHand = new Dictionary<int, (Dictionary<byte, Card[]> Hole, Card[] Board)>();

        using var host = TestEngineHost.Setup(out var holdemEngine, out _,
            shuffler: new TestCardShuffler(
                getHoleFunc: (handNo, seatNo) => cardsByHand[handNo].Hole[seatNo],
                getDeckFunc: handNo => cardsByHand[handNo].Board),
            database: database);

        var options = new GameOptionData
        {
            TableFormat = GetTableFormat(seatCount),
            MaxPlayersCount = seatCount,
            InitialSmallBlind = SmallBlind,
            InitialDealerSeatNo = FirstDealerSeatNo,
            BettingStructure = structure,
            AllowTopUp = true,
            AllowRebuy = true
        };
        (await holdemEngine.ApplyAsync(new HoldemActionCreateGame(GameId: gameId, Options: options))).IsSuccess.Should().BeTrue();

        var joinSeats = Enumerable.Range(1, seatCount)
            .Select(seatNo => new PlayerJoinData($"Seat{seatNo}", (byte)seatNo, seatNo % 3 == 1 ? random.Next(5, 21) * BigBlind : 100 * BigBlind))
            .ToArray();

        var stacks = joinSeats.ToDictionary(seat => seat.SeatNo, seat => seat.BuyInAmount);
        var totalIn = stacks.Values.Sum();
        var counters = new LimitCounters();
        byte? previousDealerSeatNo = null;

        for (var handNo = 1; handNo <= HandCount; handNo++)
        {
            cardsByHand[handNo] = DealCards(random, seatCount);

            using var scope = new AssertionScope($"{structure} {seatCount}-handed session hand #{handNo}");

            HoldemAction startAction;
            if (handNo == 1)
            {
                startAction = new HoldemActionStartGame(gameId, joinSeats);
            }
            else
            {
                // A seat below one big blind is stepped over by the blind posts, so it is topped up (or rebought
                // when it has nothing) to a fresh short or deep stack, which keeps every seat dealt in.
                foreach (var seatNo in stacks.Where(stack => stack.Value < BigBlind).Select(stack => stack.Key).Order().ToArray())
                {
                    var amount = NextLimitBuyIn(random);
                    var refill = await holdemEngine.ApplyAsync(HoldemAction.TopUp(gameId, seatNo, amount));
                    refill.IsSuccess.Should().BeTrue(
                        $"seat {seatNo} should be able to refill: {string.Join("; ", refill.Errors.Select(error => error.Message))}");
                    stacks[seatNo] += amount;
                    totalIn += amount;
                }

                startAction = new HoldemActionStartHand(gameId);
            }

            var startResult = await holdemEngine.ApplyAsync(startAction);
            startResult.IsSuccess.Should().BeTrue(
                $"hand #{handNo} should start: {string.Join("; ", startResult.Errors.Select(error => error.Message))}");

            var gameData = startResult.Value;
            var handData = gameData.HandData!;

            handData.No.Should().Be(handNo);
            AssertButtonWalksRound(handData, previousDealerSeatNo, seatCount);
            AssertBlindSeats(handData, seatCount);

            foreach (var seatData in handData.Seats)
            {
                seatData.StartHandStack.Should().Be(stacks[seatData.SeatNo],
                    $"seat {seatData.SeatNo} should start on what it finished the previous hand with, plus what it was refilled with");
            }

            handData.Seats.Sum(seat => seat.StartHandStack).Should().Be(totalIn, "the chips on the table are the chips bought in");

            gameData = await PlayOutLimitHandAsync(holdemEngine, gameId, gameData, random);
            handData = gameData.HandData!;

            handData.Seats.Sum(seat => seat.HandResult?.Net ?? 0).Should()
                .Be(0, "a hand neither creates nor destroys chips");

            foreach (var seatData in handData.Seats)
            {
                seatData.HandResult.Should().NotBeNull($"seat {seatData.SeatNo} should have a result");
                stacks[seatData.SeatNo] = seatData.StartHandStack + (seatData.HandResult?.Net ?? 0);
                stacks[seatData.SeatNo].Should().BeGreaterOrEqualTo(0, $"seat {seatData.SeatNo} cannot end below nothing");
            }

            stacks.Values.Sum().Should().Be(totalIn, "every chip bought in is on the table");

            AssertLimitLedger(handData, structure, options.RaiseCap, counters);

            previousDealerSeatNo = handData.DealerSeatNo;
        }

        Console.WriteLine($"{structure} {seatCount}-handed {database}: {counters.CappedStreets} capped streets, " +
            $"{counters.HandsWithAllIn} hands with an all-in, {counters.RaisesAtMaximum} raises exactly at the pot limit, {counters.Raises} raises");

        using var reached = new AssertionScope("the policy must actually reach the structure's limits");
        counters.HandsWithAllIn.Should().BeGreaterThan(0, "short stacks and shoves put somebody all in");
        counters.Raises.Should().BeGreaterThan(0, "somebody raised");
        if (structure == BettingStructure.FixedLimit)
        {
            counters.CappedStreets.Should().BeGreaterThan(0, "a street reached the raise cap");
        }
        else
        {
            counters.RaisesAtMaximum.Should().BeGreaterThan(0, "somebody raised exactly the pot");
        }
    }

    private sealed class LimitCounters
    {
        public int CappedStreets { get; set; }

        public int HandsWithAllIn { get; set; }

        public int RaisesAtMaximum { get; set; }

        public int Raises { get; set; }
    }

    /// <summary>One time in three a short stack of 5 to 20 big blinds, otherwise a deep one of 100.</summary>
    private static decimal NextLimitBuyIn(Random random)
        => random.Next(3) == 0 ? random.Next(5, 21) * BigBlind : 100 * BigBlind;

    /// <summary>
    /// Walks one hand's ledger in order and checks it against the structure's limits, which it works out from the
    /// ledger alone. Fixed-Limit: a street holds at most <paramref name="raiseCap"/> bets, the big blind counting
    /// pre-flop, and a bet or raise that leaves its seat with chips lifts the street total by exactly one bet.
    /// Pot-Limit: no raise-to is above the standing wager plus the pot after the raiser calls.
    /// </summary>
    private static void AssertLimitLedger(HandData handData, BettingStructure structure, int? raiseCap, LimitCounters counters)
    {
        var startStacks = handData.Seats.ToDictionary(seat => seat.SeatNo, seat => seat.StartHandStack);
        var wagers = new Dictionary<(byte SeatNo, Street Street), decimal>();
        var betCounts = new Dictionary<Street, int>();
        var anyAllIn = false;

        foreach (var action in handData.Actions)
        {
            var standing = wagers.Where(wager => wager.Key.Street == action.Street).Select(wager => wager.Value).DefaultIfEmpty(0M).Max();
            if (action.Street == Street.PreFlop)
            {
                standing = Math.Max(standing, handData.BigBlind);
            }

            var ownWager = wagers.GetValueOrDefault((action.SeatNo, action.Street));
            var pot = wagers.Values.Sum();
            wagers[(action.SeatNo, action.Street)] = Math.Max(ownWager, action.Amount);

            var committed = wagers.Where(wager => wager.Key.SeatNo == action.SeatNo).Sum(wager => wager.Value);
            var isAllIn = committed >= startStacks[action.SeatNo];
            anyAllIn |= isAllIn && action.ActionType is not (HandActionType.SmallBlind or HandActionType.BigBlind or HandActionType.Fold);

            var isRaiseKind = action.ActionType is HandActionType.Raise or HandActionType.Bet or HandActionType.AllIn;
            if (!isRaiseKind || action.Amount <= standing)
            {
                continue;
            }

            counters.Raises++;
            var where = $"{action.ActionType} to {action.Amount} by seat {action.SeatNo} on {action.Street} (action #{action.No}, standing {standing})";

            if (structure == BettingStructure.FixedLimit)
            {
                var bet = action.Street is Street.Turn or Street.River ? 2 * handData.BigBlind : handData.BigBlind;
                if (!isAllIn)
                {
                    (action.Amount - standing).Should().Be(bet, $"a Fixed-Limit raise that is not all in lifts the street by one bet: {where}");
                }
                else
                {
                    (action.Amount - standing).Should().BeLessOrEqualTo(bet, $"an all-in cannot lift the street by more than one bet: {where}");
                }

                var counted = betCounts.GetValueOrDefault(action.Street, action.Street == Street.PreFlop ? 1 : 0);
                if (action.Amount - standing >= bet / 2)
                {
                    counted++;
                }

                betCounts[action.Street] = counted;
                if (raiseCap is { } cap)
                {
                    counted.Should().BeLessOrEqualTo(cap, $"a street holds at most {cap} bets: {where}");
                    if (counted == cap && action.Amount - standing >= bet / 2)
                    {
                        counters.CappedStreets++;
                    }
                }
            }
            else
            {
                var potLimit = standing + pot + (standing - ownWager);
                action.Amount.Should().BeLessOrEqualTo(potLimit, $"a Pot-Limit raise stays within the pot after the call: {where}");
                if (action.Amount == potLimit)
                {
                    counters.RaisesAtMaximum++;
                }
            }
        }

        if (anyAllIn)
        {
            counters.HandsWithAllIn++;
        }
    }

    /// <summary>
    /// The limit policy: checks when it can, otherwise calls or folds, shoves now and then, and raises about one
    /// decision in three wherever a raise is offered, re-raises included, to the minimum or to the maximum of the window.
    /// </summary>
    private static async Task<GameData> PlayOutLimitHandAsync(IHoldemTable holdemEngine, Guid gameId, GameData gameData, Random random)
    {
        var decisionsLeft = MaxDecisionsPerHand;

        while (!gameData.HandData!.Seats.Any(seat => seat.HandResult != null))
        {
            (--decisionsLeft).Should().BePositive(
                $"hand #{gameData.HandData!.No} should end within {MaxDecisionsPerHand} decisions");

            var handData = gameData.HandData!;
            var check = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Check);
            var call = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Call);
            var raise = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);

            var actor = handData.Seats.Single(seat => seat.SeatNo == handData.NextPlayerSeatNo);
            // An all-in is legal as a call, or as a raise only when the whole stack fits the window.
            var canShove = (raise != null && actor.Stack + (actor.BetSize ?? 0) <= raise.MaxValue)
                || (call != null && actor.Stack <= call.MaxValue);

            HoldemActionPlay playerAction;
            if (canShove && random.Next(10) == 0)
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.AllIn, 0);
            }
            else if (raise != null && random.Next(3) == 0)
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.Raise, random.Next(2) == 0 ? raise.MinValue : raise.MaxValue);
            }
            else if (check != null)
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.Check, 0);
            }
            else if (call != null && random.Next(5) > 0)
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.Call, 0);
            }
            else
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.Fold, 0);
            }

            var result = await holdemEngine.ApplyAsync(playerAction);
            result.IsSuccess.Should().BeTrue(
                $"{playerAction.ActionType} of {playerAction.Size} by seat {handData.NextPlayerSeatNo} on {handData.Street} " +
                $"should be legal: {string.Join("; ", result.Errors.Select(error => error.Message))}");

            gameData = result.Value;

            foreach (var seatData in gameData.HandData!.Seats)
            {
                seatData.Stack.Should().BeGreaterOrEqualTo(0,
                    $"seat {seatData.SeatNo} cannot wager more than it has");
            }
        }

        return gameData;
    }

    private sealed class Occupant
    {
        public required string Name { get; init; }

        /// <summary>What the seat's player has, as its last hand ended.</summary>
        public decimal Stack { get; set; }

        /// <summary>Chips added between hands that the next hand deals.</summary>
        public decimal Pending { get; set; }

        public int Rebuys { get; set; }
    }

    private static void AssertDealtInSeats(HandData handData, byte[] dealtIn, Dictionary<byte, decimal> stacks, Dictionary<byte, string> names)
    {
        handData.Seats.Select(seat => seat.SeatNo).Should().Equal(dealtIn,
            "every dealt-in seat appears exactly once, in seat order, and nobody else does");

        foreach (var seatData in handData.Seats)
        {
            seatData.Name.Should().Be(names[seatData.SeatNo], $"who sits in seat {seatData.SeatNo}");
            seatData.StartHandStack.Should().Be(stacks[seatData.SeatNo],
                $"seat {seatData.SeatNo} should start on what its player has");
            seatData.HasLeft.Should().BeFalse($"seat {seatData.SeatNo} is dealt in");
        }
    }

    private static void AssertChurnButtonAndBlinds(HandData handData, byte[] dealtIn, byte? previousDealerSeatNo, int seatCount)
    {
        var expectedDealerSeatNo = previousDealerSeatNo == null
            ? FirstDealerSeatNo
            : NextDealtInSeatNo(dealtIn, previousDealerSeatNo.Value, seatCount);

        handData.DealerSeatNo.Should().Be(expectedDealerSeatNo,
            "the button moves to the next dealt-in seat clockwise of where it was");

        // Heads-up the button posts the small blind; with more players the small blind is the next dealt-in seat.
        var expectedSmallBlindSeatNo = dealtIn.Length == 2
            ? handData.DealerSeatNo
            : NextDealtInSeatNo(dealtIn, handData.DealerSeatNo, seatCount);

        handData.SmallBlindSeatNo.Should().Be(expectedSmallBlindSeatNo, "where the small blind sits");
        handData.BigBlindSeatNo.Should().Be(NextDealtInSeatNo(dealtIn, handData.SmallBlindSeatNo, seatCount),
            "the big blind sits at the next dealt-in seat after the small blind");
        handData.SmallBlindSeatNo.Should().NotBe(handData.BigBlindSeatNo, "two seats post the blinds");
        dealtIn.Should().Contain([handData.DealerSeatNo, handData.SmallBlindSeatNo, handData.BigBlindSeatNo]);
    }

    /// <summary>Walks the ring one seat at a time from <paramref name="afterSeatNo"/>, wrapping, until it meets a dealt-in seat.</summary>
    private static byte NextDealtInSeatNo(byte[] dealtIn, byte afterSeatNo, int seatCount)
    {
        for (var step = 1; step <= seatCount; step++)
        {
            var seatNo = (byte)((afterSeatNo - 1 + step) % seatCount + 1);
            if (dealtIn.Contains(seatNo))
            {
                return seatNo;
            }
        }

        throw new InvalidOperationException("Nobody is dealt in.");
    }

    /// <summary>
    /// Drives the hand to its end. The policy checks when it can, otherwise calls or folds, and
    /// occasionally opens a street with the smallest legal raise, but never re-raises. <see cref="PlayerActionEvaluationData.MinValue"/>
    /// on a raise is the "raise to" total the engine wants as the size, regardless of where in the
    /// street the decision falls.
    /// </summary>
    private static async Task<GameData> PlayOutHandAsync(IHoldemTable holdemEngine, Guid gameId, GameData gameData, Random random,
        int shoveOdds = 0, Func<GameData, int, Task<GameData>>? beforeDecision = null)
    {
        var decisionsLeft = MaxDecisionsPerHand;
        var decisionNo = 0;

        // A hand is over once the engine has written results — the same test the engine itself makes
        // in IsCurrentHandEnded. Evaluations is not a reliable signal: a hand that ends on a fold
        // still reports a next player, and so still offers actions.
        while (!gameData.HandData!.Seats.Any(seat => seat.HandResult != null))
        {
            (--decisionsLeft).Should().BePositive(
                $"hand #{gameData.HandData!.No} should end within {MaxDecisionsPerHand} decisions");

            if (beforeDecision != null)
            {
                gameData = await beforeDecision(gameData, decisionNo++);
                if (gameData.HandData!.Seats.Any(seat => seat.HandResult != null))
                {
                    break;
                }
            }

            var handData = gameData.HandData!;
            var check = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Check);
            var call = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Call);
            var raise = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);

            var streetRaised = handData.Actions.Any(action => action.Street == handData.Street
                && action.ActionType is HandActionType.Raise or HandActionType.Bet or HandActionType.AllIn);

            // An all-in is legal as a call, or as a raise where raising is offered: a short all-in
            // does not reopen the betting to a seat that has already acted.
            var actor = handData.Seats.Single(seat => seat.SeatNo == handData.NextPlayerSeatNo);
            var canShove = raise != null || (call != null && actor.Stack <= call.MaxValue);

            HoldemActionPlay playerAction;
            if (shoveOdds > 0 && random.Next(shoveOdds) == 0 && canShove)
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.AllIn, 0);
            }
            else if (raise != null && !streetRaised && random.Next(4) == 0)
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.Raise, raise.MinValue);
            }
            else if (check != null)
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.Check, 0);
            }
            else if (call != null && random.Next(5) > 0)
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.Call, 0);
            }
            else
            {
                playerAction = new HoldemActionPlay(gameId, PlayerActionType.Fold, 0);
            }

            var result = await holdemEngine.ApplyAsync(playerAction);
            result.IsSuccess.Should().BeTrue(
                $"{playerAction.ActionType} of {playerAction.Size} by seat {handData.NextPlayerSeatNo} on {handData.Street} " +
                $"should be legal: {string.Join("; ", result.Errors.Select(error => error.Message))}");

            gameData = result.Value;

            foreach (var seatData in gameData.HandData!.Seats)
            {
                seatData.Stack.Should().BeGreaterOrEqualTo(0,
                    $"seat {seatData.SeatNo} cannot wager more than it has");
            }
        }

        return gameData;
    }

    private static void AssertButtonWalksRound(HandData handData, byte? previousDealerSeatNo, int seatCount)
    {
        var expectedDealerSeatNo = previousDealerSeatNo == null
            ? FirstDealerSeatNo
            : NextSeatNo(previousDealerSeatNo.Value, seatCount);

        handData.DealerSeatNo.Should().Be(expectedDealerSeatNo,
            "the button moves one seat on at every hand while the table stays full");
    }

    private static void AssertBlindSeats(HandData handData, int seatCount)
    {
        // Heads-up the button posts the small blind; at a fuller table the small blind sits left of it.
        var expectedSmallBlindSeatNo = seatCount == 2
            ? handData.DealerSeatNo
            : NextSeatNo(handData.DealerSeatNo, seatCount);

        handData.SmallBlindSeatNo.Should().Be(expectedSmallBlindSeatNo, "where the small blind sits");
        handData.BigBlindSeatNo.Should().Be(NextSeatNo(handData.SmallBlindSeatNo, seatCount),
            "the big blind sits left of the small blind");
        handData.StreetPot.Should().Be(SmallBlind + BigBlind, "only the blinds are in yet");
    }

    private static byte NextSeatNo(byte seatNo, int seatCount) => (byte)(seatNo % seatCount + 1);

    private static HoldemTableFormat GetTableFormat(int seatCount)
    {
        return seatCount switch
        {
            2 => HoldemTableFormat.HeadsUp,
            >= 3 and <= 6 => HoldemTableFormat.SixMax,
            >= 7 and <= 10 => HoldemTableFormat.FullRing,
            _ => throw new ArgumentOutOfRangeException(nameof(seatCount))
        };
    }

    /// <summary>
    /// A full shuffle per hand: two cards for each seat, then the five board cards. Driven by the
    /// test's seeded generator, so the whole session is reproducible.
    /// </summary>
    private static (Dictionary<byte, Card[]> Hole, Card[] Board) DealCards(Random random, int seatCount)
    {
        var deck = Phh.PhhCards.FullDeck.OrderBy(_ => random.Next()).ToArray();

        var hole = Enumerable.Range(0, seatCount)
            .ToDictionary(
                seatIx => (byte)(seatIx + 1),
                seatIx => deck.Skip(seatIx * 2).Take(2).ToArray());

        return (hole, [.. deck.Skip(seatCount * 2).Take(5)]);
    }
}
