using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Tests;

[TestClass]
public sealed class ActionTypeContractTests
{
    private static readonly Guid _gameId = Guid.Parse("{33333333-3333-3333-3333-333333333333}");

    private static readonly GameOptionData _gameOptions = new() { MaxPlayersCount = 6, InitialSmallBlind = 5, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _seats =
    [
        new("Player 1", 1, 1000),
        new("Player 2", 2, 1000),
        new("Player 3", 3, 1000),
    ];

    [TestMethod]
    public void PlayerActionType_Members_AreWhatACallerMayRequest()
        => Enum.GetNames<PlayerActionType>().Should().Equal("Fold", "Check", "Call", "Raise", "Bet", "AllIn");

    [TestMethod]
    public void HandActionType_Members_AreWhatTheLedgerRecords()
        => Enum.GetNames<HandActionType>().Should().Equal("SmallBlind", "BigBlind", "Fold", "Check", "Call", "Raise", "Bet", "AllIn");

    [TestMethod]
    public void HandActionType_SharedMembers_KeepPlayerActionTypeNumbers()
    {
        foreach (var name in Enum.GetNames<PlayerActionType>())
        {
            ((byte)Enum.Parse<HandActionType>(name)).Should().Be((byte)Enum.Parse<PlayerActionType>(name), name);
        }
    }

    [TestMethod]
    public async Task HoldemAction_UndefinedPlayerActionType_FailsWithInvalidActionAndPersistsNothing()
    {
        using var host = TestEngineHost.Setup(out var holdemEngine, out var contextFactory);
        await holdemEngine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, _gameOptions));
        (await holdemEngine.ApplyHoldemActionAsync(new HoldemActionStartGame(_gameId, _seats))).IsSuccess.Should().BeTrue();
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await holdemEngine.ApplyHoldemActionAsync(new HoldemActionPlayerAction(_gameId, (PlayerActionType)42, 0));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidAction);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task HoldemAction_GetGameRightAfterCreateGame_HasNoHandData()
    {
        using var host = TestEngineHost.Setup(out var holdemEngine, out _);
        await holdemEngine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, _gameOptions));

        var result = await holdemEngine.ApplyHoldemActionAsync(new HoldemActionGetGame(_gameId));

        result.IsSuccess.Should().BeTrue();
        result.Value.HandData.Should().BeNull();
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
