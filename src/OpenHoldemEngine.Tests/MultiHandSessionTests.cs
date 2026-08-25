using FluentAssertions;
using FluentAssertions.Execution;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.Tests;

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
/// </remarks>
[TestClass]
public sealed class MultiHandSessionTests
{
    private const int SeatCount = 6;
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
    public async Task MultiHandSession_KeepsTheLedgerBalanced()
    {
        var gameId = Guid.Parse("{22222222-2222-2222-2222-222222222222}");
        var random = new Random(Seed);
        var cardsByHand = new Dictionary<int, (Dictionary<byte, Card[]> Hole, Card[] Board)>();

        using var host = TestEngineHost.Setup(out var holdemEngine, out _,
            shuffler: new TestCardShuffler(
                _getHoleFunc: (handNo, seatNo) => cardsByHand[handNo].Hole[seatNo],
                _getDeckFunc: handNo => cardsByHand[handNo].Board));

        (await holdemEngine.ApplyHoldemActionAsync(new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData(
                MaxPlayersCount: SeatCount,
                InitialSmallBlind: SmallBlind,
                InitialDealerSeatNo: FirstDealerSeatNo,
                AllowRebuy: false)))).IsSuccess.Should().BeTrue();

        var joinSeats = Enumerable.Range(1, SeatCount)
            .Select(seatNo => new PlayerJoinData($"Seat{seatNo}", (byte)seatNo, BuyIn))
            .ToArray();

        // Stacks the previous hand ended on, which the next hand must start from.
        var expectedStartStacks = joinSeats.ToDictionary(seat => seat.SeatNo, seat => seat.BuyInAmount);
        byte? previousDealerSeatNo = null;

        for (var handNo = 1; handNo <= HandCount; handNo++)
        {
            cardsByHand[handNo] = DealCards(random);

            using var scope = new AssertionScope($"session hand #{handNo}");

            var startResult = await holdemEngine.ApplyHoldemActionAsync(new HoldemActionStartHand(gameId)
            {
                JoinSeats = handNo == 1 ? joinSeats : []
            });
            startResult.IsSuccess.Should().BeTrue(
                $"hand #{handNo} should start: {string.Join("; ", startResult.Errors.Select(error => error.Message))}");

            var gameData = startResult.Value;
            var handData = gameData.HandData;

            handData.No.Should().Be(handNo);
            AssertButtonWalksRound(handData, previousDealerSeatNo);
            AssertBlindSeats(handData);

            foreach (var seatData in handData.Seats)
            {
                seatData.StartRoundStack.Should().Be(expectedStartStacks[seatData.SeatNo],
                    $"seat {seatData.SeatNo} should start on what it finished the previous hand with");
            }

            handData.Seats.Sum(seat => seat.StartRoundStack).Should()
                .Be(SeatCount * BuyIn, "nobody buys in again, so the chips on the table never change");

            gameData = await PlayOutHandAsync(holdemEngine, gameId, gameData, random);
            handData = gameData.HandData;

            handData.Seats.Sum(seat => seat.RoundResultData?.Net ?? 0).Should()
                .Be(0, "a hand neither creates nor destroys chips");

            foreach (var seatData in handData.Seats)
            {
                seatData.RoundResultData.Should().NotBeNull($"seat {seatData.SeatNo} should have a result");
                expectedStartStacks[seatData.SeatNo] = seatData.StartRoundStack + (seatData.RoundResultData?.Net ?? 0);

                // Deliberately more than "not negative": the session is sized (deep stacks, small
                // blinds, minimum raises only) so that nobody can ever bust. A seat sitting on
                // nothing would take the game somewhere the engine has no answer for — `BuyIns` and
                // `KickOffSeats` are declared on HoldemActionStartHand but no handler reads them.
                expectedStartStacks[seatData.SeatNo].Should().BePositive(
                    $"seat {seatData.SeatNo} should never run out of chips in this session");
            }

            previousDealerSeatNo = handData.DealerSeatNo;
        }
    }

    /// <summary>
    /// Drives the hand to its end. The policy checks when it can, otherwise calls or folds, and opens
    /// with the smallest legal raise on the first decision of a street — at that point the actor has
    /// nothing in front of it yet, so the minimum from <see cref="HandData.Evaluations"/> is the
    /// street total the engine wants as the raise size.
    /// </summary>
    private static async Task<GameData> PlayOutHandAsync(HoldemEngine holdemEngine, Guid gameId, GameData gameData, Random random)
    {
        var streetOfPreviousDecision = default(Street?);
        var decisionsLeft = MaxDecisionsPerHand;

        // A hand is over once the engine has written results — the same test the engine itself makes
        // in IsCurrentHandEnded. Evaluations is not a reliable signal: a hand that ends on a fold
        // still reports a next player, and so still offers actions.
        while (!gameData.HandData.Seats.Any(seat => seat.RoundResultData != null))
        {
            (--decisionsLeft).Should().BePositive(
                $"hand #{gameData.HandData.No} should end within {MaxDecisionsPerHand} decisions");

            var handData = gameData.HandData;
            var isFirstDecisionOfStreet = streetOfPreviousDecision != handData.Street;
            var check = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Check);
            var call = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Call);
            var raise = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);

            HoldemActionPlayerAction playerAction;
            if (isFirstDecisionOfStreet && raise != null && random.Next(4) == 0)
                playerAction = new HoldemActionPlayerAction(gameId, PlayerActionType.Raise, raise.MinValue);
            else if (check != null)
                playerAction = new HoldemActionPlayerAction(gameId, PlayerActionType.Check, 0);
            else if (call != null && random.Next(5) > 0)
                playerAction = new HoldemActionPlayerAction(gameId, PlayerActionType.Call, 0);
            else
                playerAction = new HoldemActionPlayerAction(gameId, PlayerActionType.Fold, 0);

            streetOfPreviousDecision = handData.Street;

            var result = await holdemEngine.ApplyHoldemActionAsync(playerAction);
            result.IsSuccess.Should().BeTrue(
                $"{playerAction.ActionType} of {playerAction.Size} by seat {handData.NextPlayerSeatNo} on {handData.Street} " +
                $"should be legal: {string.Join("; ", result.Errors.Select(error => error.Message))}");

            gameData = result.Value;

            foreach (var seatData in gameData.HandData.Seats)
            {
                seatData.Stack.Should().BeGreaterOrEqualTo(0,
                    $"seat {seatData.SeatNo} cannot wager more than it has");
            }
        }

        return gameData;
    }

    private static void AssertButtonWalksRound(HandData handData, byte? previousDealerSeatNo)
    {
        var expectedDealerSeatNo = previousDealerSeatNo == null
            ? FirstDealerSeatNo
            : NextSeatNo(previousDealerSeatNo.Value);

        handData.DealerSeatNo.Should().Be(expectedDealerSeatNo,
            "the button moves one seat on at every hand while the table stays full");
    }

    private static void AssertBlindSeats(HandData handData)
    {
        handData.SmallBlindSeatNo.Should().Be(NextSeatNo(handData.DealerSeatNo), "the small blind sits left of the button");
        handData.BigBlindSeatNo.Should().Be(NextSeatNo(handData.SmallBlindSeatNo), "the big blind sits left of the small blind");
        handData.StreetPot.Should().Be(SmallBlind + BigBlind, "only the blinds are in yet");
    }

    private static byte NextSeatNo(byte seatNo) => (byte)(seatNo % SeatCount + 1);

    /// <summary>
    /// A full shuffle per hand: two cards for each seat, then the five board cards. Driven by the
    /// test's seeded generator, so the whole session is reproducible.
    /// </summary>
    private static (Dictionary<byte, Card[]> Hole, Card[] Board) DealCards(Random random)
    {
        var deck = Phh.PhhCards.FullDeck.OrderBy(_ => random.Next()).ToArray();

        var hole = Enumerable.Range(0, SeatCount)
            .ToDictionary(
                seatIx => (byte)(seatIx + 1),
                seatIx => deck.Skip(seatIx * 2).Take(2).ToArray());

        return (hole, [.. deck.Skip(SeatCount * 2).Take(5)]);
    }
}
