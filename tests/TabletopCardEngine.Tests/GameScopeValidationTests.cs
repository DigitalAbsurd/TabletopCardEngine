using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Data;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.HoldemActionHandlers;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class GameScopeValidationTests
{
    private static readonly Guid _gameId = Guid.Parse("{33333333-3333-3333-3333-333333333333}");

    private static readonly PlayerJoinData[] _seats =
    [
        new("Player 1", 1, 1000),
        new("Player 2", 2, 1000),
    ];

    [TestMethod]
    public void GameOptionData_HasNoConstructorWithParameters()
    {
        typeof(GameOptionData).GetConstructors().Should().OnlyContain(constructor => constructor.GetParameters().Length == 0);
    }

    [TestMethod]
    public async Task CreateGame_UndefinedGameType_FailsWithUnsupportedGameOptions()
        => await AssertCreateGameFailsAsync(new GameOptionData { GameType = (GameType)99 }, HoldemErrorCode.UnsupportedGameOptions);

    [TestMethod]
    public async Task CreateGame_UndefinedBettingStructure_FailsWithUnsupportedGameOptions()
        => await AssertCreateGameFailsAsync(new GameOptionData { BettingStructure = (BettingStructure)99 }, HoldemErrorCode.UnsupportedGameOptions);

    [TestMethod]
    public async Task CreateGame_EveryDefinedVariant_AcceptedOnlyWhenSupported()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);

        var supportedVariants = SupportedGameVariants.All.ToList();
        supportedVariants.Should().Equal([(GameType.Holdem, BettingStructure.NoLimit)]);

        foreach (var gameType in Enum.GetValues<GameType>())
        {
            foreach (var bettingStructure in Enum.GetValues<BettingStructure>())
            {
                var gameId = Guid.NewGuid();
                var options = new GameOptionData { GameType = gameType, BettingStructure = bettingStructure };
                var result = await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(gameId, options));

                var isSupported = SupportedGameVariants.IsSupported(gameType, bettingStructure);
                if (isSupported)
                {
                    result.IsSuccess.Should().BeTrue($"variant {gameType}/{bettingStructure} should be supported");
                }
                else
                {
                    result.IsFailed.Should().BeTrue($"variant {gameType}/{bettingStructure} should not be supported");
                    result.Errors.Single().Code.Should().Be(HoldemErrorCode.UnsupportedGameOptions);
                }
            }
        }
    }

    [TestMethod]
    public async Task CreateGame_OneSeat_FailsWithInvalidGameOptions()
        => await AssertCreateGameFailsAsync(new GameOptionData { MaxPlayersCount = 1 }, HoldemErrorCode.InvalidGameOptions);

    [TestMethod]
    public async Task CreateGame_SevenSeats_FailsWithInvalidGameOptions()
        => await AssertCreateGameFailsAsync(new GameOptionData { MaxPlayersCount = 7 }, HoldemErrorCode.InvalidGameOptions);

    [TestMethod]
    public async Task CreateGame_ZeroSmallBlind_FailsWithInvalidGameOptions()
        => await AssertCreateGameFailsAsync(new GameOptionData { InitialSmallBlind = 0 }, HoldemErrorCode.InvalidGameOptions);

    [TestMethod]
    public async Task CreateGame_DealerSeatZero_FailsWithInvalidGameOptions()
        => await AssertCreateGameFailsAsync(new GameOptionData { InitialDealerSeatNo = 0 }, HoldemErrorCode.InvalidGameOptions);

    [TestMethod]
    public async Task CreateGame_DealerSeatBeyondTable_FailsWithInvalidGameOptions()
        => await AssertCreateGameFailsAsync(new GameOptionData { MaxPlayersCount = 4, InitialDealerSeatNo = 5 }, HoldemErrorCode.InvalidGameOptions);

    [TestMethod]
    public async Task CreateGame_SmallBlindWithThreeDecimals_FailsWithInvalidGameOptions()
        => await AssertCreateGameFailsAsync(new GameOptionData { InitialSmallBlind = 0.125m }, HoldemErrorCode.InvalidGameOptions);

    [TestMethod]
    public async Task CreateGame_SmallBlindWithTwoDecimals_Succeeds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        var result = await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { InitialSmallBlind = 0.25m }));

        result.IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateGame_NarrowedSettingsMax_RejectsFiveSeatGame()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory, configureSettings: settings => settings.GamePlayersMaxCount = 4);

        var result = await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { MaxPlayersCount = 5 }));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidGameOptions);
        (await CountRowsAsync(contextFactory)).Should().OnlyContain(count => count == 0);
    }

    [TestMethod]
    public async Task StartGame_SeatZero_FailsWithInvalidSeat()
        => await AssertStartGameFailsAsync([new("Player 1", 0, 1000), new("Player 2", 2, 1000)]);

    [TestMethod]
    public async Task StartGame_SeatBeyondTable_FailsWithInvalidSeat()
        => await AssertStartGameFailsAsync([new("Player 1", 1, 1000), new("Player 2", 4, 1000)]);

    [TestMethod]
    public async Task StartGame_DuplicateSeat_FailsWithInvalidSeat()
        => await AssertStartGameFailsAsync([new("Player 1", 1, 1000), new("Player 2", 1, 1000)]);

    [TestMethod]
    public async Task StartGame_ZeroBuyIn_FailsWithInvalidSeat()
        => await AssertStartGameFailsAsync([new("Player 1", 1, 1000), new("Player 2", 2, 0)]);

    [TestMethod]
    public async Task StartGame_BuyInWithThreeDecimals_FailsWithInvalidSeat()
        => await AssertStartGameFailsAsync([new("Player 1", 1, 1000), new("Player 2", 2, 100.005m)]);

    [TestMethod]
    public async Task StartGame_NameIsEmpty_FailsWithInvalidSeat()
        => await AssertStartGameFailsAsync([new("", 1, 1000), new("Player 2", 2, 1000)]);

    [TestMethod]
    public async Task StartGame_NameIsWhitespaceOnly_FailsWithInvalidSeat()
        => await AssertStartGameFailsAsync([new("  ", 1, 1000), new("Player 2", 2, 1000)]);

    [TestMethod]
    public async Task StartGame_NameExceeds64Characters_FailsWithInvalidSeat()
    {
        var longName = new string('x', 65);
        await AssertStartGameFailsAsync([new(longName, 1, 1000), new("Player 2", 2, 1000)]);
    }

    [TestMethod]
    public async Task StartGame_NameIs64Characters_Succeeds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        (await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { MaxPlayersCount = 3 }))).IsSuccess.Should().BeTrue();
        var exactName = new string('x', 64);

        var result = await engine.ApplyHoldemActionAsync(new HoldemActionStartGame(_gameId, [new(exactName, 1, 1000), new("Player 2", 2, 1000)]));

        result.IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public async Task StartHand_OccupiedSeat_FailsWithInvalidSeat()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        (await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { MaxPlayersCount = 3 }))).IsSuccess.Should().BeTrue();
        (await engine.ApplyHoldemActionAsync(new HoldemActionStartGame(_gameId, _seats))).IsSuccess.Should().BeTrue();
        (await engine.ApplyHoldemActionAsync(new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0))).IsSuccess.Should().BeTrue();
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await engine.ApplyHoldemActionAsync(new HoldemActionStartHand(_gameId) { JoinSeats = [new("Player 1", 1, 500)] });

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidSeat);
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task Play_RaiseSizeWithThreeDecimals_FailsWithInvalidActionAndPersistsNothing()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        (await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { MaxPlayersCount = 3 }))).IsSuccess.Should().BeTrue();
        (await engine.ApplyHoldemActionAsync(new HoldemActionStartGame(_gameId, _seats))).IsSuccess.Should().BeTrue();
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await engine.ApplyHoldemActionAsync(new HoldemActionPlay(_gameId, PlayerActionType.Raise, 4.001m));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidAction);
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public void WithSettings_MaxAboveSix_Throws()
    {
        var act = () => TestEngineHost.Setup(out _, out _, configureSettings: settings => settings.GamePlayersMaxCount = 7);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void WithSettings_MinBelowTwo_Throws()
    {
        var act = () => TestEngineHost.Setup(out _, out _, configureSettings: settings => settings.GamePlayersMinCount = 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void WithSettings_MinAboveMax_Throws()
    {
        var act = () => TestEngineHost.Setup(out _, out _, configureSettings: settings =>
        {
            settings.GamePlayersMinCount = 5;
            settings.GamePlayersMaxCount = 4;
        });

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static async Task AssertCreateGameFailsAsync(GameOptionData options, HoldemErrorCode expected)
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);

        var result = await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, options));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(expected);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await CountRowsAsync(contextFactory)).Should().OnlyContain(count => count == 0);
    }

    private static async Task AssertStartGameFailsAsync(PlayerJoinData[] joinSeats)
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        (await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { MaxPlayersCount = 3 }))).IsSuccess.Should().BeTrue();
        var rowsBefore = await CountRowsAsync(contextFactory);

        var result = await engine.ApplyHoldemActionAsync(new HoldemActionStartGame(_gameId, joinSeats));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidSeat);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await CountRowsAsync(contextFactory)).Should().Equal(rowsBefore);
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
