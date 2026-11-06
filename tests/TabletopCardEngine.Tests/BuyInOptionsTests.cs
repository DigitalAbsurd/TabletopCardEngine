using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class BuyInOptionsTests
{
    private static readonly Guid _gameId = Guid.Parse("{44444444-4444-4444-4444-444444444444}");

    [TestMethod]
    public async Task CreateGame_Defaults_KeepTheOneDotOneGame()
    {
        var options = await RoundTripAsync(new GameOptionData());

        options.AllowTopUp.Should().BeFalse();
        options.AllowRebuy.Should().BeFalse();
        options.MaxRebuys.Should().BeNull();
        options.MinBuyIn.Should().BeNull();
        options.MaxBuyIn.Should().BeNull();
    }

    [TestMethod]
    public async Task CreateGame_AllowTopUp_RoundTrips()
        => (await RoundTripAsync(new GameOptionData { AllowTopUp = true })).AllowTopUp.Should().BeTrue();

    [TestMethod]
    public async Task CreateGame_AllowRebuy_RoundTrips()
        => (await RoundTripAsync(new GameOptionData { AllowRebuy = true })).AllowRebuy.Should().BeTrue();

    [TestMethod]
    public async Task CreateGame_MaxRebuys_RoundTrips()
    {
        var options = await RoundTripAsync(new GameOptionData { AllowRebuy = true, MaxRebuys = 3 });

        options.MaxRebuys.Should().Be(3);
    }

    [TestMethod]
    public async Task CreateGame_ZeroMaxRebuys_RoundTrips()
        => (await RoundTripAsync(new GameOptionData { AllowRebuy = true, MaxRebuys = 0 })).MaxRebuys.Should().Be(0);

    [TestMethod]
    public async Task CreateGame_BuyInWindow_RoundTrips()
    {
        var options = await RoundTripAsync(new GameOptionData { MinBuyIn = 20.50M, MaxBuyIn = 200M });

        options.MinBuyIn.Should().Be(20.50M);
        options.MaxBuyIn.Should().Be(200M);
    }

    [TestMethod]
    public async Task CreateGame_EqualMinAndMaxBuyIn_RoundTrips()
    {
        var options = await RoundTripAsync(new GameOptionData { MinBuyIn = 100M, MaxBuyIn = 100M });

        options.MinBuyIn.Should().Be(100M);
        options.MaxBuyIn.Should().Be(100M);
    }

    [TestMethod]
    public async Task CreateGame_OnlyMinBuyIn_RoundTrips()
    {
        var options = await RoundTripAsync(new GameOptionData { MinBuyIn = 50M });

        options.MinBuyIn.Should().Be(50M);
        options.MaxBuyIn.Should().BeNull();
    }

    [TestMethod]
    public async Task StartGame_WritesJoinBuyIns()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        (await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, new GameOptionData()))).IsSuccess.Should().BeTrue();
        (await engine.ApplyAsync(new HoldemActionStartGame(_gameId, [new("A", 1, 100), new("B", 2, 100)]))).IsSuccess.Should().BeTrue();

        await using var context = await contextFactory.CreateDbContextAsync();
        var kinds = await context.Set<BuyIn>().Select(buyIn => buyIn.Kind).ToListAsync();

        kinds.Should().Equal(BuyInKind.Join, BuyInKind.Join);
    }

    [TestMethod]
    public async Task CreateGame_ZeroMinBuyIn_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { MinBuyIn = 0 });

    [TestMethod]
    public async Task CreateGame_NegativeMinBuyIn_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { MinBuyIn = -1 });

    [TestMethod]
    public async Task CreateGame_ZeroMaxBuyIn_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { MaxBuyIn = 0 });

    [TestMethod]
    public async Task CreateGame_NegativeMaxBuyIn_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { MaxBuyIn = -5 });

    [TestMethod]
    public async Task CreateGame_MinBuyInWithThreeDecimals_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { MinBuyIn = 1.234M });

    [TestMethod]
    public async Task CreateGame_MaxBuyInWithThreeDecimals_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { MaxBuyIn = 100.001M });

    [TestMethod]
    public async Task CreateGame_MinBuyInAboveMaxBuyIn_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { MinBuyIn = 200, MaxBuyIn = 100 });

    [TestMethod]
    public async Task CreateGame_NegativeMaxRebuys_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { AllowRebuy = true, MaxRebuys = -1 });

    [TestMethod]
    public async Task CreateGame_MaxRebuysWithoutAllowRebuy_FailsWithInvalidGameOptions()
        => await AssertFailsAsync(new GameOptionData { AllowRebuy = false, MaxRebuys = 2 });

    [TestMethod]
    public async Task StartGame_BelowMinBuyIn_FailsWithInvalidBuyIn()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        (await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { MinBuyIn = 50M }))).IsSuccess.Should().BeTrue();

        var result = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, [new("A", 1, 49M), new("B", 2, 100)]));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidBuyIn);
        await using var context = await contextFactory.CreateDbContextAsync();
        (await context.Set<Hand>().CountAsync()).Should().Be(0);
    }

    [TestMethod]
    public async Task StartGame_AboveMaxBuyIn_FailsWithInvalidBuyIn()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        (await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { MaxBuyIn = 200M }))).IsSuccess.Should().BeTrue();

        var result = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, [new("A", 1, 100), new("B", 2, 201M)]));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidBuyIn);
        await using var context = await contextFactory.CreateDbContextAsync();
        (await context.Set<Hand>().CountAsync()).Should().Be(0);
    }

    [TestMethod]
    public async Task StartGame_AtMinBuyIn_Succeeds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        (await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { MinBuyIn = 50M }))).IsSuccess.Should().BeTrue();

        var result = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, [new("A", 1, 50M), new("B", 2, 100)]));

        result.IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public async Task StartGame_AtMaxBuyIn_Succeeds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        (await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, new GameOptionData { MaxBuyIn = 100M }))).IsSuccess.Should().BeTrue();

        var result = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, [new("A", 1, 100), new("B", 2, 100)]));

        result.IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public async Task StartGame_WithoutBuyInWindow_AcceptsAnyAmount()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        (await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, new GameOptionData()))).IsSuccess.Should().BeTrue();

        var result = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, [new("A", 1, 0.01M), new("B", 2, 1000000M)]));

        result.IsSuccess.Should().BeTrue();
    }

    private static async Task<GameOptionData> RoundTripAsync(GameOptionData options)
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        var created = await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, options));
        created.IsSuccess.Should().BeTrue(string.Join("; ", created.Errors.Select(error => error.Message)));
        created.Value.Options.ShouldAsJToken().BeEquivalentToAsJToken(options);

        var read = await engine.ApplyAsync(new HoldemActionGetGame(_gameId));
        read.IsSuccess.Should().BeTrue();
        read.Value.Options.ShouldAsJToken().BeEquivalentToAsJToken(options);
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
