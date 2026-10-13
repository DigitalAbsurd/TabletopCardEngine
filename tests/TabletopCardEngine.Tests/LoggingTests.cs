using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The engine's log events: their IDs and levels are a contract, so they are pinned here one flow at a time.
/// Every test owns its container and its collector, because the assembly runs tests in parallel.
/// </summary>
[TestClass]
public sealed class LoggingTests
{
    private static readonly Guid _gameId = Guid.Parse("{44444444-4444-4444-4444-444444444444}");

    private static readonly GameOptionData _options = new() { MaxPlayersCount = 2, InitialSmallBlind = 50M, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _joinSeats = [new("Button", 1, 10_000M), new("BigBlind", 2, 10_000M)];

    [TestMethod]
    public async Task Logging_HeadsUpHandPlayedToTheEnd_LogsLifecycleInOrder()
    {
        using var host = Setup(out var engine, out var collector);

        await ApplyAsync(engine, new HoldemActionCreateGame(_gameId, _options));
        await ApplyAsync(engine, new HoldemActionStartGame(_gameId, _joinSeats));
        await ApplyAsync(engine, new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0));
        await ApplyAsync(engine, new HoldemActionFinishGame(_gameId));

        var records = EngineRecords(collector);
        records.Select(record => (record.Id.Id, record.Level)).Should().Equal(
            (1010, LogLevel.Information),
            (1001, LogLevel.Debug),
            (1011, LogLevel.Information),
            (1001, LogLevel.Debug),
            (1012, LogLevel.Information),
            (1001, LogLevel.Debug),
            (1013, LogLevel.Information),
            (1001, LogLevel.Debug));
        records.Count(record => record.Id.Id == 1001).Should().Be(4);
        records.Select(record => record.Id.Name).Should().Contain(["GameCreated", "HandStarted", "HandFinished", "GameFinished", "ActionApplied"]);
    }

    [TestMethod]
    public async Task Logging_StartHandBeforeStartGame_LogsActionRejected()
    {
        using var host = Setup(out var engine, out var collector);
        await ApplyAsync(engine, new HoldemActionCreateGame(_gameId, _options));
        collector.Clear();

        var result = await engine.ApplyHoldemActionAsync(new HoldemActionStartHand(_gameId));

        result.IsSuccess.Should().BeFalse();
        var record = EngineRecords(collector).Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(1002);
        record.Level.Should().Be(LogLevel.Information);
        record.Message.Should().Be($"StartHand on game {_gameId} was rejected with GameNotStarted");
    }

    [TestMethod]
    public async Task Logging_ExpectedVersionMismatch_LogsConcurrencyConflict()
    {
        using var host = Setup(out var engine, out var collector);
        await ApplyAsync(engine, new HoldemActionCreateGame(_gameId, _options));
        collector.Clear();

        var result = await engine.ApplyHoldemActionAsync(new HoldemActionStartGame(_gameId, _joinSeats) { ExpectedVersion = 99 });

        result.IsSuccess.Should().BeFalse();
        var record = EngineRecords(collector).Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(1003);
        record.Level.Should().Be(LogLevel.Warning);
        record.Message.Should().Be($"StartGame on game {_gameId} failed with a concurrency conflict (ExpectedVersion)");
    }

    [TestMethod]
    public async Task Logging_PlayCall_EveryRecordCarriesTheGameAndHandScope()
    {
        using var host = Setup(out var engine, out var collector);
        await ApplyAsync(engine, new HoldemActionCreateGame(_gameId, _options));
        await ApplyAsync(engine, new HoldemActionStartGame(_gameId, _joinSeats));
        collector.Clear();

        await ApplyAsync(engine, new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0));

        var records = EngineRecords(collector);
        records.Should().NotBeEmpty();
        foreach (var record in records)
        {
            var scope = record.Scopes.Should().ContainSingle().Subject
                .Should().BeAssignableTo<IEnumerable<KeyValuePair<string, object?>>>().Subject
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            scope["GameId"].Should().Be(_gameId);
            scope["HandNo"].Should().Be(1);
        }
    }

    private static TestEngineHost.DisposableTestData Setup(out HoldemEngine engine, out FakeLogCollector collector)
    {
        var fakeCollector = new FakeLogCollector();
        collector = fakeCollector;
        return TestEngineHost.Setup(out engine, out _,
            shuffler: new TestCardShuffler(
                getHoleFunc: (_, seatNo) => seatNo == 1 ? CardsFrom("ac", "kc") : CardsFrom("7d", "2h"),
                getDeckFunc: _ => CardsFrom("4s", "9h", "qd", "3c", "8s")),
            configureServices: services => services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddProvider(new FakeLoggerProvider(fakeCollector))));
    }

    /// <summary>EF Core logs through the same factory, so keep only what the engine wrote.</summary>
    private static IReadOnlyList<FakeLogRecord> EngineRecords(FakeLogCollector collector)
        => [.. collector.GetSnapshot().Where(record => record.Category == typeof(HoldemEngine).FullName)];

    private static async Task ApplyAsync(HoldemEngine engine, HoldemAction action)
    {
        var result = await engine.ApplyHoldemActionAsync(action);
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
    }

    private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];
}
