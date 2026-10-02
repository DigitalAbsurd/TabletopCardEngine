using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Tests;

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
