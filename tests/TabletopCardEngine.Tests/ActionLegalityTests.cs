using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Data;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// An action <see cref="HandData.Evaluations"/> does not offer, or a raise outside its window, is
/// rejected with <see cref="HoldemErrorCode.IllegalAction"/> and changes nothing. The engine used to
/// fold the seat silently instead, while the ledger recorded the requested type, so the returned
/// <see cref="GameData"/> contradicted itself. Every case runs on a fresh heads-up 1/2 game with
/// stacks of 100: the button (seat 1) posts the small blind and is offered
/// <c>[Fold, Call(1..1), Raise(3..100)]</c> pre-flop.
/// </summary>
[TestClass]
public sealed class ActionLegalityTests
{
    private const byte ButtonSeatNo = 1;
    private const byte BigBlindSeatNo = 2;

    private static readonly GameOptionData _options = new() { MaxPlayersCount = 2, InitialSmallBlind = 1, InitialDealerSeatNo = ButtonSeatNo };

    [TestMethod]
    [DataRow(PlayerActionType.Check, 0)]
    [DataRow(PlayerActionType.Raise, 2.5)]
    [DataRow(PlayerActionType.Raise, 0)]
    [DataRow(PlayerActionType.Raise, 500)]
    public async Task Play_SmallBlindIllegalAction_FailsWithIllegalActionAndChangesNothing(PlayerActionType actionType, double size)
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        var gameId = Guid.NewGuid();
        var started = await StartHeadsUpAsync(engine, gameId);
        started.HandData!.NextPlayerSeatNo.Should().Be(ButtonSeatNo);

        await AssertRejectedAsync(engine, contextFactory, started, new HoldemActionPlay(gameId, actionType, (decimal)size));
    }

    [TestMethod]
    public async Task Play_BigBlindCallsAfterALimp_FailsWithIllegalActionAndChangesNothing()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        var gameId = Guid.NewGuid();
        await StartHeadsUpAsync(engine, gameId);
        var limped = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Call, 0));
        limped.HandData!.NextPlayerSeatNo.Should().Be(BigBlindSeatNo);
        limped.HandData.Evaluations.Select(evaluation => evaluation.PlayerActionType)
            .Should().Equal(PlayerActionType.Fold, PlayerActionType.Check, PlayerActionType.Raise);

        await AssertRejectedAsync(engine, contextFactory, limped, new HoldemActionPlay(gameId, PlayerActionType.Call, 0));
    }

    [TestMethod]
    [DataRow(PlayerActionType.Raise, 3)]
    [DataRow(PlayerActionType.Raise, 100)]
    [DataRow(PlayerActionType.Call, 0)]
    [DataRow(PlayerActionType.Fold, 0)]
    public async Task Play_SmallBlindLegalEdge_Succeeds(PlayerActionType actionType, double size)
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var gameId = Guid.NewGuid();
        var started = await StartHeadsUpAsync(engine, gameId);

        var played = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, actionType, (decimal)size));

        played.Version.Should().Be(started.Version + 1);
        var button = played.HandData!.Seats.Single(seat => seat.SeatNo == ButtonSeatNo);
        button.LastAction!.ActionType.Should().Be(Enum.Parse<HandActionType>(actionType.ToString()));
    }

    [TestMethod]
    public async Task Play_PreFlopBet_FailsWithIllegalActionAndChangesNothing()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        var gameId = Guid.NewGuid();
        var started = await StartHeadsUpAsync(engine, gameId);

        await AssertRejectedAsync(engine, contextFactory, started, new HoldemActionPlay(gameId, PlayerActionType.Bet, 5));
    }

    [TestMethod]
    public async Task Play_FlopBet_IsCountedAsARaiseWhenTheTableIsRebuilt()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var gameId = Guid.NewGuid();
        await StartOnFlopAsync(engine, gameId);

        var bet = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Bet, 4));

        bet.HandData!.NextPlayerSeatNo.Should().Be(ButtonSeatNo);
        bet.HandData.StreetPot.Should().Be(4);
        var evaluations = bet.HandData.Evaluations;
        evaluations.Select(evaluation => evaluation.PlayerActionType)
            .Should().Equal(PlayerActionType.Fold, PlayerActionType.Call, PlayerActionType.Raise);
        var call = evaluations.Single(evaluation => evaluation.PlayerActionType == PlayerActionType.Call);
        (call.MinValue, call.MaxValue).Should().Be((4M, 4M));
        var raise = evaluations.Single(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);
        (raise.MinValue, raise.MaxValue).Should().Be((8M, 98M));
    }

    [TestMethod]
    public async Task Play_FlopBetThenCheck_FailsWithIllegalAction()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        var gameId = Guid.NewGuid();
        await StartOnFlopAsync(engine, gameId);
        var bet = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Bet, 4));

        await AssertRejectedAsync(engine, contextFactory, bet, new HoldemActionPlay(gameId, PlayerActionType.Check, 0));
    }

    [TestMethod]
    public async Task Play_FlopBetThenCall_ClosesTheStreet()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var gameId = Guid.NewGuid();
        await StartOnFlopAsync(engine, gameId);
        await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Bet, 4));

        var called = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Call, 0));

        called.HandData!.Street.Should().Be(Street.Turn);
        called.HandData.Pot.Should().Be(12);
        called.HandData.StreetPot.Should().Be(0);
        called.HandData.NextPlayerSeatNo.Should().Be(BigBlindSeatNo);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(99)]
    public async Task Play_FlopBetOutsideTheWindow_FailsWithIllegalAction(double size)
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        var gameId = Guid.NewGuid();
        var flop = await StartOnFlopAsync(engine, gameId);

        await AssertRejectedAsync(engine, contextFactory, flop, new HoldemActionPlay(gameId, PlayerActionType.Bet, (decimal)size));
    }

    [TestMethod]
    public async Task Play_FlopBetWhileAWagerIsOpen_FailsWithIllegalAction()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        var gameId = Guid.NewGuid();
        await StartOnFlopAsync(engine, gameId);
        var bet = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Bet, 4));

        await AssertRejectedAsync(engine, contextFactory, bet, new HoldemActionPlay(gameId, PlayerActionType.Bet, 10));
    }

    [TestMethod]
    public async Task AllIn_ActsLikeTheEquivalentRaise()
    {
        var allIn = await PlayAfterPreFlopActionAsync(PlayerActionType.AllIn, 0);
        var raise = await PlayAfterPreFlopActionAsync(PlayerActionType.Raise, 100);

        allIn.HandData!.Evaluations.Should().BeEquivalentTo(raise.HandData!.Evaluations, options => options.WithStrictOrdering());
        allIn.HandData.Seats.Select(seat => (seat.SeatNo, seat.Stack, seat.BetSize))
            .Should().Equal(raise.HandData.Seats.Select(seat => (seat.SeatNo, seat.Stack, seat.BetSize)));
        allIn.HandData.StreetPot.Should().Be(raise.HandData.StreetPot);
        allIn.HandData.Pot.Should().Be(raise.HandData.Pot);
        allIn.HandData.NextPlayerSeatNo.Should().Be(raise.HandData.NextPlayerSeatNo);
        allIn.HandData.Seats.Single(seat => seat.SeatNo == ButtonSeatNo).LastAction!.ActionType.Should().Be(HandActionType.AllIn);
        raise.HandData.Seats.Single(seat => seat.SeatNo == ButtonSeatNo).LastAction!.ActionType.Should().Be(HandActionType.Raise);
        allIn.HandData.Seats.Single(seat => seat.SeatNo == ButtonSeatNo).Stack.Should().Be(0);
        allIn.HandData.NextPlayerSeatNo.Should().Be(BigBlindSeatNo);
    }

    [TestMethod]
    public async Task AllIn_ForLessThanTheCall_IsAnAllInCall()
    {
        var allIn = await PlayShortBigBlindAsync(PlayerActionType.AllIn);
        var call = await PlayShortBigBlindAsync(PlayerActionType.Call);

        allIn.HandData!.Seats.Single(seat => seat.SeatNo == BigBlindSeatNo).Stack.Should().Be(0);
        allIn.HandData.Street.Should().Be(Street.Finished);
        allIn.HandData.Seats.Sum(seat => seat.HandResult!.Net).Should().Be(0);
        allIn.HandData.Seats.Select(seat => (seat.SeatNo, seat.HandResult!.Net))
            .Should().Equal(call.HandData!.Seats.Select(seat => (seat.SeatNo, seat.HandResult!.Net)));
    }

    [TestMethod]
    public async Task AllIn_AsACall_DoesNotLowerTheMinimumRaise()
    {
        using var host = TestEngineHost.Setup(out var engine, out _, shuffler: ScriptedDeck());
        var gameId = Guid.NewGuid();
        await ApplyOkAsync(engine, new HoldemActionCreateGame(gameId, new GameOptionData { MaxPlayersCount = 3, InitialSmallBlind = 1, InitialDealerSeatNo = 1 }));
        await ApplyOkAsync(engine, new HoldemActionStartGame(gameId, [new("A", 1, 22), new("B", 2, 100), new("C", 3, 100)]));
        await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Call, 0));
        await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Call, 0));
        var flop = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Check, 0));
        flop.HandData!.Street.Should().Be(Street.Flop);
        await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Check, 0));
        var raised = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Raise, 20));
        raised.HandData!.NextPlayerSeatNo.Should().Be(1);

        var called = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.AllIn, 0));

        called.HandData!.NextPlayerSeatNo.Should().Be(2);
        called.HandData.Evaluations.Select(evaluation => (evaluation.PlayerActionType, evaluation.MinValue, evaluation.MaxValue))
            .Should().Equal(
                (PlayerActionType.Fold, 0M, 0M),
                (PlayerActionType.Call, 20M, 20M),
                (PlayerActionType.Raise, 40M, 98M));
    }

    private static Card[] Cards(params string[] cards) => [.. cards.Select(Card.Parse)];

    private static TestCardShuffler ScriptedDeck()
    {
        Card[][] holes = [Cards("ac", "kc"), Cards("7d", "2h"), Cards("js", "jd")];
        var board = Cards("4s", "9h", "qd", "3c", "8s");
        return new TestCardShuffler((_, seatNo) => holes[seatNo - 1], _ => board);
    }

    private static async Task<GameData> PlayAfterPreFlopActionAsync(PlayerActionType actionType, decimal size)
    {
        using var host = TestEngineHost.Setup(out var engine, out _, shuffler: ScriptedDeck());
        var gameId = Guid.NewGuid();
        await StartHeadsUpAsync(engine, gameId);
        return await ApplyOkAsync(engine, new HoldemActionPlay(gameId, actionType, size));
    }

    private static async Task<GameData> PlayShortBigBlindAsync(PlayerActionType bigBlindAction)
    {
        using var host = TestEngineHost.Setup(out var engine, out _, shuffler: ScriptedDeck());
        var gameId = Guid.NewGuid();
        await ApplyOkAsync(engine, new HoldemActionCreateGame(gameId, _options));
        await ApplyOkAsync(engine, new HoldemActionStartGame(gameId, [new("Button", ButtonSeatNo, 100), new("BigBlind", BigBlindSeatNo, 30)]));
        await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Raise, 50));
        return await ApplyOkAsync(engine, new HoldemActionPlay(gameId, bigBlindAction, 0));
    }

    private static async Task<GameData> StartOnFlopAsync(HoldemEngine engine, Guid gameId)
    {
        await StartHeadsUpAsync(engine, gameId);
        await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Call, 0));
        var flop = await ApplyOkAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Check, 0));
        flop.HandData!.Street.Should().Be(Street.Flop);
        flop.HandData.NextPlayerSeatNo.Should().Be(BigBlindSeatNo);
        return flop;
    }

    private static async Task AssertRejectedAsync(
        HoldemEngine engine, IDbContextFactory<HoldemDbContext> contextFactory, GameData before, HoldemActionPlay action)
    {
        var actionRowsBefore = await CountHandActionsAsync(contextFactory);

        var result = await engine.ApplyHoldemActionAsync(action);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(HoldemErrorCode.IllegalAction);
        (await CountHandActionsAsync(contextFactory)).Should().Be(actionRowsBefore);

        var after = await ApplyOkAsync(engine, new HoldemActionGetGame(action.GameId));
        after.Version.Should().Be(before.Version);
        after.HandData!.NextPlayerSeatNo.Should().Be(before.HandData!.NextPlayerSeatNo);
    }

    private static async Task<GameData> StartHeadsUpAsync(HoldemEngine engine, Guid gameId)
    {
        await ApplyOkAsync(engine, new HoldemActionCreateGame(gameId, _options));
        return await ApplyOkAsync(engine, new HoldemActionStartGame(gameId,
        [
            new("Button", ButtonSeatNo, 100),
            new("BigBlind", BigBlindSeatNo, 100),
        ]));
    }

    private static async Task<GameData> ApplyOkAsync(HoldemEngine engine, HoldemAction action)
    {
        var result = await engine.ApplyHoldemActionAsync(action);
        result.IsSuccess.Should().BeTrue($"{action.GetType().Name} should be accepted: {string.Join("; ", result.Errors.Select(error => error.Message))}");
        return result.Value;
    }

    private static async Task<int> CountHandActionsAsync(IDbContextFactory<HoldemDbContext> contextFactory)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return await dbContext.Set<HandAction>().CountAsync();
    }
}
