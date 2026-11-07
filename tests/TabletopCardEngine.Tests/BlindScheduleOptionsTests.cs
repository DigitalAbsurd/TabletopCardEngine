using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class BlindScheduleOptionsTests
{
    private static readonly Guid _gameId = Guid.Parse("{55555555-5555-5555-5555-555555555555}");

    [TestMethod]
    public async Task CreateGame_Defaults_HaveNoBlindSchedule()
        => (await RoundTripAsync(new GameOptionData())).BlindSchedule.Should().BeNull();

    [TestMethod]
    public async Task CreateGame_MultiplierSchedule_RoundTrips()
    {
        var schedule = new BlindScheduleData
        {
            IncreaseEveryHands = 10,
            PlayerThresholds = [4, 3],
            IncreaseOnElimination = true,
            Multiplier = 1.5M,
            MaxSmallBlind = 12.5M,
        };

        var options = await RoundTripAsync(new GameOptionData { InitialSmallBlind = 1, BlindSchedule = schedule });

        options.BlindSchedule!.ShouldAsJToken().BeEquivalentToAsJToken(schedule);
        options.BlindSchedule!.Mode.Should().Be(BlindIncreaseMode.Multiplier);
    }

    [TestMethod]
    public async Task CreateGame_LevelsSchedule_RoundTrips()
    {
        var schedule = new BlindScheduleData
        {
            IncreaseOnElimination = true,
            Mode = BlindIncreaseMode.Levels,
            SmallBlindLevels = [2M, 3.5M, 10M],
        };

        var options = await RoundTripAsync(new GameOptionData { BlindSchedule = schedule });

        options.BlindSchedule!.ShouldAsJToken().BeEquivalentToAsJToken(schedule);
        options.BlindSchedule!.SmallBlindLevels.Should().Equal(2M, 3.5M, 10M);
    }

    [TestMethod]
    public async Task CreateGame_SingleTriggerSchedules_Succeed()
    {
        (await RoundTripAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseEveryHands = 1 } })).BlindSchedule.Should().NotBeNull();
        (await RoundTripAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { PlayerThresholds = [2, 5] } })).BlindSchedule.Should().NotBeNull();
        (await RoundTripAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true } })).BlindSchedule.Should().NotBeNull();
    }

    [TestMethod]
    public async Task CreateGame_MaxSmallBlindEqualToInitial_RoundTrips()
    {
        var schedule = new BlindScheduleData { IncreaseOnElimination = true, MaxSmallBlind = 1M };

        (await RoundTripAsync(new GameOptionData { BlindSchedule = schedule })).BlindSchedule!.ShouldAsJToken().BeEquivalentToAsJToken(schedule);
    }

    [TestMethod]
    public async Task StartGame_WithABlindSchedule_StillDealsTheInitialSmallBlind()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var options = new GameOptionData { InitialSmallBlind = 5, BlindSchedule = new BlindScheduleData { IncreaseEveryHands = 1 } };
        (await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, options))).IsSuccess.Should().BeTrue();

        var started = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, [new("A", 1, 1000), new("B", 2, 1000)]));

        started.IsSuccess.Should().BeTrue();
        started.Value.HandData!.SmallBlind.Should().Be(5);
        started.Value.HandData.BigBlind.Should().Be(10);
    }

    [TestMethod]
    public async Task ThreeHands_NoBlindSchedule_KeepTheInitialBlinds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var options = new GameOptionData { InitialSmallBlind = 2 };
        (await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, options))).IsSuccess.Should().BeTrue();
        var players = new[] { new PlayerJoinData("A", 1, 100), new PlayerJoinData("B", 2, 100) };

        var hand = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, players));
        for (var handNo = 1; handNo <= 3; handNo++)
        {
            hand.IsSuccess.Should().BeTrue($"hand {handNo} should be dealt");
            hand.Value.HandData!.SmallBlind.Should().Be(2, $"hand {handNo}");
            hand.Value.HandData.BigBlind.Should().Be(4, $"hand {handNo}");
            hand.Value.HandData.BlindLevel.Should().Be(0, $"hand {handNo}");

            if (handNo < 3)
            {
                var fold = await engine.ApplyAsync(new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0));
                fold.IsSuccess.Should().BeTrue($"hand {handNo} should end on one fold");
                hand = await engine.ApplyAsync(new HoldemActionStartHand(_gameId));
            }
        }
    }

    [TestMethod]
    public async Task CreateGame_NoTrigger_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData() });

    [TestMethod]
    public async Task CreateGame_ZeroIncreaseEveryHands_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseEveryHands = 0 } });

    [TestMethod]
    public async Task CreateGame_NegativeIncreaseEveryHands_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseEveryHands = -3 } });

    [TestMethod]
    public async Task CreateGame_ThresholdBelowTwo_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { PlayerThresholds = [1] } });

    [TestMethod]
    public async Task CreateGame_ThresholdAtMaxPlayers_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { PlayerThresholds = [6] } });

    [TestMethod]
    public async Task CreateGame_DuplicateThresholds_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { PlayerThresholds = [3, 3] } });

    [TestMethod]
    public async Task CreateGame_UndefinedMode_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Mode = (BlindIncreaseMode)9 } });

    [TestMethod]
    public async Task CreateGame_MultiplierOfOne_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Multiplier = 1 } });

    [TestMethod]
    public async Task CreateGame_MultiplierBelowOne_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Multiplier = 0.5M } });

    [TestMethod]
    public async Task CreateGame_MultiplierWithThreeDecimals_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Multiplier = 1.555M } });

    [TestMethod]
    public async Task CreateGame_MaxSmallBlindBelowInitial_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, MaxSmallBlind = 0.5M } });

    [TestMethod]
    public async Task CreateGame_MaxSmallBlindWithThreeDecimals_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, MaxSmallBlind = 10.001M } });

    [TestMethod]
    public async Task CreateGame_MultiplierModeWithLevels_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, SmallBlindLevels = [2M] } });

    [TestMethod]
    public async Task CreateGame_LevelsModeWithoutLevels_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Mode = BlindIncreaseMode.Levels } });

    [TestMethod]
    public async Task CreateGame_LevelsModeWithMaxSmallBlind_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Mode = BlindIncreaseMode.Levels, SmallBlindLevels = [2M], MaxSmallBlind = 5M } });

    [TestMethod]
    public async Task CreateGame_LevelsModeFirstLevelNotAboveInitial_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Mode = BlindIncreaseMode.Levels, SmallBlindLevels = [1M] } });

    [TestMethod]
    public async Task CreateGame_LevelsModeNotIncreasing_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Mode = BlindIncreaseMode.Levels, SmallBlindLevels = [2M, 4M, 4M] } });

    [TestMethod]
    public async Task CreateGame_LevelsModeLevelWithThreeDecimals_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Mode = BlindIncreaseMode.Levels, SmallBlindLevels = [2.005M] } });

    [TestMethod]
    public async Task CreateGame_ScheduleTooLongToStore_FailsWithInvalidGameOptions()
    {
        var levels = Enumerable.Range(2, 400).Select(level => level * 1000.25M).ToArray();

        await AssertFailsAsync(new GameOptionData { BlindSchedule = new BlindScheduleData { IncreaseOnElimination = true, Mode = BlindIncreaseMode.Levels, SmallBlindLevels = levels } });
    }

    private static async Task<GameOptionData> RoundTripAsync(GameOptionData options)
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        var created = await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, options));
        created.IsSuccess.Should().BeTrue(string.Join("; ", created.Errors.Select(error => error.Message)));

        var read = await engine.ApplyAsync(new HoldemActionGetGame(_gameId));
        read.IsSuccess.Should().BeTrue();
        read.Value.Options.ShouldAsJToken().BeEquivalentToAsJToken(options);
        read.Value.Options.BlindSchedule.Should().Be(created.Value.Options.BlindSchedule, "the engine's lists compare element by element");
        return read.Value.Options;
    }

    private static async Task AssertFailsAsync(GameOptionData options)
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);

        var result = await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, options));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidGameOptions);
        await using var context = await contextFactory.CreateDbContextAsync();
        (await context.Set<Game>().CountAsync()).Should().Be(0);
    }
}
