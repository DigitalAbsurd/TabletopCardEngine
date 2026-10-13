using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class GameLifecycleTests
{
    private static readonly Guid _gameId = Guid.Parse("{44444444-4444-4444-4444-444444444444}");

    private static readonly GameOptionData _gameOptions = new() { MaxPlayersCount = 6, InitialSmallBlind = 5, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _seats =
    [
        new("Player 1", 1, 1000),
        new("Player 2", 2, 1000),
        new("Player 3", 3, 1000),
    ];

    private sealed record UnknownAction(Guid GameId) : HoldemAction(GameId);

    [TestMethod]
    public async Task Lifecycle_CreateStartPlayFinish_ActionAfterFinishFailsAndGetGameStillWorks()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await ApplyOkAsync(engine, new HoldemActionCreateGame(_gameId, _gameOptions));
        var started = await ApplyOkAsync(engine, new HoldemActionStartGame(_gameId, _seats));
        started.IsFinished.Should().BeFalse();
        started.HandData!.No.Should().Be(1);

        await PlayHandToEndAsync(engine);

        var second = await ApplyOkAsync(engine, new HoldemActionStartHand(_gameId));
        second.HandData!.No.Should().Be(2);
        await PlayHandToEndAsync(engine);

        var finished = await ApplyOkAsync(engine, new HoldemActionFinishGame(_gameId));
        finished.IsFinished.Should().BeTrue();

        var action = await engine.ApplyAsync(new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0));
        action.IsFailed.Should().BeTrue();
        action.Errors.Single().Code.Should().Be(HoldemErrorCode.GameFinished);

        var read = await ApplyOkAsync(engine, new HoldemActionGetGame(_gameId));
        read.IsFinished.Should().BeTrue();
    }

    [TestMethod]
    public async Task Lifecycle_AfterFinish_EveryActionExceptGetGameFailsWithGameFinished()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        await ApplyOkAsync(engine, new HoldemActionCreateGame(_gameId, _gameOptions));
        await ApplyOkAsync(engine, new HoldemActionFinishGame(_gameId));
        var rowsBefore = await CountRowsAsync(contextFactory);

        HoldemAction[] actions =
        [
            new HoldemActionFinishGame(_gameId),
            new HoldemActionStartGame(_gameId, _seats),
            new HoldemActionStartHand(_gameId),
            new HoldemActionCreateGame(_gameId, _gameOptions),
            new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0),
        ];
        foreach (var action in actions)
        {
            var result = await engine.ApplyAsync(action);

            result.IsFailed.Should().BeTrue(action.GetType().Name);
            result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameFinished, action.GetType().Name);
        }

        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task Lifecycle_StartHandBeforeStartGame_FailsWithGameNotStarted()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        await ApplyOkAsync(engine, new HoldemActionCreateGame(_gameId, _gameOptions));
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await engine.ApplyAsync(new HoldemActionStartHand(_gameId) { JoinSeats = _seats });

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameNotStarted);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task Lifecycle_StartGameTwice_FailsWithGameAlreadyStarted()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        await ApplyOkAsync(engine, new HoldemActionCreateGame(_gameId, _gameOptions));
        await ApplyOkAsync(engine, new HoldemActionStartGame(_gameId, _seats));
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, _seats));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameAlreadyStarted);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task Lifecycle_StartGameOnUnknownGame_FailsAndPersistsNothing()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);

        var result = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, _seats));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameNotFound);
        (await CountRowsAsync(contextFactory)).Should().OnlyContain(count => count == 0);
    }

    [TestMethod]
    public async Task Lifecycle_StartGameWithInvalidSeat_FailsWithInvalidSeat()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        await ApplyOkAsync(engine, new HoldemActionCreateGame(_gameId, _gameOptions));
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, [new("Player 1", 9, 1000), new("Player 2", 2, 1000)]));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidSeat);
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task Lifecycle_FinishGameMidHand_FailsWithPreviousHandNotEndedAndHandContinues()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        await ApplyOkAsync(engine, new HoldemActionCreateGame(_gameId, _gameOptions));
        await ApplyOkAsync(engine, new HoldemActionStartGame(_gameId, _seats));
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await engine.ApplyAsync(new HoldemActionFinishGame(_gameId));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.PreviousHandNotEnded);
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);

        var read = await ApplyOkAsync(engine, new HoldemActionGetGame(_gameId));
        read.IsFinished.Should().BeFalse();
        await ApplyOkAsync(engine, new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0));
    }

    [TestMethod]
    public async Task Lifecycle_FinishGameNeverStarted_Succeeds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await ApplyOkAsync(engine, new HoldemActionCreateGame(_gameId, _gameOptions));

        var finished = await ApplyOkAsync(engine, new HoldemActionFinishGame(_gameId));

        finished.IsFinished.Should().BeTrue();
        finished.HandData.Should().BeNull();
    }

    [TestMethod]
    public async Task Lifecycle_FinishGameOnUnknownGame_FailsAndPersistsNothing()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);

        var result = await engine.ApplyAsync(new HoldemActionFinishGame(_gameId));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameNotFound);
        (await CountRowsAsync(contextFactory)).Should().OnlyContain(count => count == 0);
    }

    [TestMethod]
    [DataRow(nameof(HoldemActionGetGame))]
    [DataRow(nameof(HoldemActionStartGame))]
    [DataRow(nameof(HoldemActionStartHand))]
    [DataRow(nameof(HoldemActionPlay))]
    [DataRow(nameof(HoldemActionFinishGame))]
    public async Task Lifecycle_EveryActionButCreateGameOnUnknownGame_FailsWithGameNotFound(string actionName)
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        HoldemAction action = actionName switch
        {
            nameof(HoldemActionGetGame) => new HoldemActionGetGame(_gameId),
            nameof(HoldemActionStartGame) => new HoldemActionStartGame(_gameId, _seats),
            nameof(HoldemActionStartHand) => new HoldemActionStartHand(_gameId),
            nameof(HoldemActionPlay) => new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0),
            nameof(HoldemActionFinishGame) => new HoldemActionFinishGame(_gameId),
            _ => throw new ArgumentOutOfRangeException(nameof(actionName), actionName, null),
        };

        var result = await engine.ApplyAsync(action);

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameNotFound);
        (await CountRowsAsync(contextFactory)).Should().OnlyContain(count => count == 0);
    }

    [TestMethod]
    public async Task Lifecycle_UnknownGameWithExpectedVersion_FailsWithGameNotFound()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);

        var result = await engine.ApplyAsync(new HoldemActionGetGame(_gameId) { ExpectedVersion = 3 });

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameNotFound);
        (await CountRowsAsync(contextFactory)).Should().OnlyContain(count => count == 0);
    }

    [TestMethod]
    public async Task Lifecycle_StartGameSeatsInDescendingOrder_PutsButtonWhereAscendingOrderDoes()
    {
        var ascending = await StartAndReadHandAsync(_seats);
        var descending = await StartAndReadHandAsync([.. _seats.Reverse()]);

        descending.DealerSeatNo.Should().Be(ascending.DealerSeatNo);
        descending.SmallBlindSeatNo.Should().Be(ascending.SmallBlindSeatNo);
        descending.BigBlindSeatNo.Should().Be(ascending.BigBlindSeatNo);
        descending.NextPlayerSeatNo.Should().Be(ascending.NextPlayerSeatNo);
    }

    [TestMethod]
    public async Task Lifecycle_UnknownActionRecord_FailsWithInvalidAction()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        await ApplyOkAsync(engine, new HoldemActionCreateGame(_gameId, _gameOptions));
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await engine.ApplyAsync(new UnknownAction(_gameId));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidAction);
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    private static async Task<HandData> StartAndReadHandAsync(PlayerJoinData[] joinSeats)
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await ApplyOkAsync(engine, new HoldemActionCreateGame(_gameId, _gameOptions));
        return (await ApplyOkAsync(engine, new HoldemActionStartGame(_gameId, joinSeats))).HandData!;
    }

    private static async Task PlayHandToEndAsync(IHoldemTable engine)
    {
        // Three seats: the first two to act fold, and the last one takes the pot.
        await ApplyOkAsync(engine, new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0));
        var ended = await ApplyOkAsync(engine, new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0));
        ended.HandData!.Street.Should().Be(Street.Finished);
    }

    private static async Task<GameData> ApplyOkAsync(IHoldemTable engine, HoldemAction action)
    {
        var result = await engine.ApplyAsync(action);
        result.IsSuccess.Should().BeTrue($"{action.GetType().Name} should be accepted: {string.Join("; ", result.Errors.Select(error => error.Message))}");
        return result.Value;
    }

    private static async Task<int[]> CountRowsAsync(IDbContextFactory<HoldemDbContext> contextFactory)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return
        [
            await dbContext.Set<Game>().CountAsync(),
            await dbContext.Set<Player>().CountAsync(),
            await dbContext.Set<BuyIn>().CountAsync(),
            await dbContext.Set<Hand>().CountAsync(),
            await dbContext.Set<HandAction>().CountAsync(),
            await dbContext.Set<HandDeckCard>().CountAsync(),
            await dbContext.Set<HandPlayerStartState>().CountAsync(),
            await dbContext.Set<HandPlayerEndState>().CountAsync(),
        ];
    }
}
