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
/// only ever checks, calls, folds or makes the smallest legal raise. Bet sizing is already covered by
/// the thousands of recorded raises the PHH replay drives through the engine.
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

    /// <summary>Fixed so a failure is reproducible.</summary>
    private const int Seed = 20260825;

    [TestMethod]
    [DataRow(6, TestDatabaseKind.Sqlite, DisplayName = "six-handed, Sqlite")]
    [DataRow(2, TestDatabaseKind.Sqlite, DisplayName = "heads-up, Sqlite")]
    [DataRow(6, TestDatabaseKind.InMemory, DisplayName = "six-handed, InMemory")]
    [DataRow(2, TestDatabaseKind.InMemory, DisplayName = "heads-up, InMemory")]
    [DataRow(6, TestDatabaseKind.PostgreSql, DisplayName = "six-handed, PostgreSql")]
    [DataRow(2, TestDatabaseKind.PostgreSql, DisplayName = "heads-up, PostgreSql")]
    [DataRow(6, TestDatabaseKind.SqlServer, DisplayName = "six-handed, SqlServer")]
    [DataRow(2, TestDatabaseKind.SqlServer, DisplayName = "heads-up, SqlServer")]
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
            Options: new GameOptionData { MaxPlayersCount = seatCount, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = FirstDealerSeatNo }))).IsSuccess.Should().BeTrue();

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
    /// Drives the hand to its end. The policy checks when it can, otherwise calls or folds, and
    /// occasionally opens with the smallest legal raise — <see cref="PlayerActionEvaluationData.MinValue"/>
    /// on a raise is the "raise to" total the engine wants as the size, regardless of where in the
    /// street the decision falls.
    /// </summary>
    private static async Task<GameData> PlayOutHandAsync(IHoldemTable holdemEngine, Guid gameId, GameData gameData, Random random)
    {
        var decisionsLeft = MaxDecisionsPerHand;

        // A hand is over once the engine has written results — the same test the engine itself makes
        // in IsCurrentHandEnded. Evaluations is not a reliable signal: a hand that ends on a fold
        // still reports a next player, and so still offers actions.
        while (!gameData.HandData!.Seats.Any(seat => seat.HandResult != null))
        {
            (--decisionsLeft).Should().BePositive(
                $"hand #{gameData.HandData!.No} should end within {MaxDecisionsPerHand} decisions");

            var handData = gameData.HandData!;
            var check = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Check);
            var call = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Call);
            var raise = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);

            HoldemActionPlay playerAction;
            if (raise != null && random.Next(4) == 0)
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
