using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class HoldemResultTests
{
    private static readonly Guid _gameId = Guid.Parse("{22222222-2222-2222-2222-222222222222}");

    private static readonly GameOptionData _gameOptions = new() { MaxPlayersCount = 6, InitialSmallBlind = 5, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _seats =
    [
        new("Player 1", 1, 1000),
        new("Player 2", 2, 1000),
        new("Player 3", 3, 1000),
    ];

    [TestMethod]
    public async Task HoldemResult_CreateGameTwice_FailsWithGameAlreadyExists()
    {
        using var host = TestEngineHost.Setup(out var holdemEngine, out var contextFactory);
        await holdemEngine.ApplyAsync(new HoldemActionCreateGame(_gameId, _gameOptions));
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await holdemEngine.ApplyAsync(new HoldemActionCreateGame(_gameId, _gameOptions));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameAlreadyExists);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task HoldemResult_PlayerActionAfterHandEnded_FailsWithCurrentHandEnded()
    {
        using var host = TestEngineHost.Setup(out var holdemEngine, out var contextFactory);
        await holdemEngine.ApplyAsync(new HoldemActionCreateGame(_gameId, _gameOptions));
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await holdemEngine.ApplyAsync(new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.CurrentHandEnded);
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task HoldemResult_StartHandWhileHandRunning_FailsWithPreviousHandNotEnded()
    {
        using var host = TestEngineHost.Setup(out var holdemEngine, out var contextFactory);
        await holdemEngine.ApplyAsync(new HoldemActionCreateGame(_gameId, _gameOptions));
        var firstHand = await holdemEngine.ApplyAsync(new HoldemActionStartGame(_gameId, _seats));
        firstHand.IsSuccess.Should().BeTrue();
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await holdemEngine.ApplyAsync(new HoldemActionStartHand(_gameId) { JoinSeats = _seats });

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.PreviousHandNotEnded);
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task HoldemResult_FailedResult_ValueThrowsInvalidOperation()
    {
        using var host = TestEngineHost.Setup(out var holdemEngine, out _);
        await holdemEngine.ApplyAsync(new HoldemActionCreateGame(_gameId, _gameOptions));

        var result = await holdemEngine.ApplyAsync(new HoldemActionCreateGame(_gameId, _gameOptions));

        var read = () => result.Value;
        read.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public async Task HoldemResult_SuccessfulResult_HasNoErrorsAndAValue()
    {
        using var host = TestEngineHost.Setup(out var holdemEngine, out _);

        var result = await holdemEngine.ApplyAsync(new HoldemActionCreateGame(_gameId, _gameOptions));

        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
        result.Errors.Should().BeEmpty();
        result.Value.GameId.Should().Be(_gameId);
    }

    [TestMethod]
    public void HoldemErrorCode_NumericValues_AreTheFrozenContract()
    {
        var expected = new Dictionary<HoldemErrorCode, int>
        {
            [HoldemErrorCode.GameAlreadyExists] = 1,
            [HoldemErrorCode.CurrentHandEnded] = 2,
            [HoldemErrorCode.PreviousHandNotEnded] = 3,
            [HoldemErrorCode.InvalidAction] = 4,
            [HoldemErrorCode.UnsupportedGameOptions] = 5,
            [HoldemErrorCode.InvalidGameOptions] = 6,
            [HoldemErrorCode.InvalidSeat] = 7,
            [HoldemErrorCode.GameAlreadyStarted] = 8,
            [HoldemErrorCode.GameNotStarted] = 9,
            [HoldemErrorCode.GameFinished] = 10,
            [HoldemErrorCode.Conflict] = 11,
            [HoldemErrorCode.GameNotFound] = 12,
            [HoldemErrorCode.IllegalAction] = 13,
        };

        Enum.GetValues<HoldemErrorCode>().Should().BeEquivalentTo(expected.Keys, "a new code needs a row in this table");
        foreach (var (code, number) in expected)
        {
            ((int)code).Should().Be(number, $"{code} must never be renumbered");
        }
    }

    private static async Task<int[]> CountRowsAsync(IDbContextFactory<HoldemDbContext> contextFactory)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return
        [
            await dbContext.Set<Game>().CountAsync(),
            await dbContext.Set<Hand>().CountAsync(),
            await dbContext.Set<HandAction>().CountAsync(),
            await dbContext.Set<HandDeckCard>().CountAsync(),
            await dbContext.Set<HandPlayerStartState>().CountAsync(),
            await dbContext.Set<HandPlayerEndState>().CountAsync(),
        ];
    }
}
