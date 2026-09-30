using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenHoldemEngine.Diagnostics;
using OpenHoldemEngine.Dto;
using System.Reflection;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// Sensitive card data: hole cards and the deck are never logged at Information level or above,
/// and never appear in span tags. They are logged only at Trace when EnableSensitiveCardLogging is true.
/// </summary>
[TestClass]
public sealed class SensitiveDataPolicyTests
{
    private static readonly Guid _gameId = Guid.Parse("{44444444-4444-4444-4444-444444444444}");

    private static readonly GameOptionData _options = new() { MaxPlayersCount = 2, InitialSmallBlind = 50M, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _joinSeats = [new("Button", 1, 10_000M), new("BigBlind", 2, 10_000M)];

    [TestMethod]
    public void LoggerMessages_CarryCardsOnlyAtTrace()
    {
        var cardEventMethods = typeof(HoldemEngineLog)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(method => method.GetCustomAttribute<LoggerMessageAttribute>() is not null)
            .Where(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Any(param =>
                    param.ParameterType == typeof(Card) ||
                    param.ParameterType == typeof(IEnumerable<Card>));
            })
            .ToList();

        cardEventMethods.Should().NotBeEmpty("at least one card-carrying event must be defined");

        foreach (var method in cardEventMethods)
        {
            var attribute = method.GetCustomAttribute<LoggerMessageAttribute>()!;
            attribute.Level.Should().Be(LogLevel.Trace,
                $"card event {method.Name} must be Trace level, not {attribute.Level}");
        }
    }

    [TestMethod]
    public async Task CardEvents_AreNotWrittenWithoutTheSetting()
    {
        using var host = Setup(enableSensitiveLogging: false, out var engine, out var collector);

        await ApplyHandAsync(engine);

        var records = EngineRecords(collector);
        records.Should().NotContain(record => record.Id.Id == 1100 || record.Id.Id == 1101,
            "no card events should be written when EnableSensitiveCardLogging is false");
    }

    [TestMethod]
    public async Task CardEvents_AreWrittenAtTraceWithTheSetting()
    {
        using var host = Setup(enableSensitiveLogging: true, out var engine, out var collector);

        await ApplyHandAsync(engine);

        var records = EngineRecords(collector);

        var holeCardRecords = records.Where(record => record.Id.Id == 1100).ToList();
        holeCardRecords.Should().HaveCount(2, "one HoleCardsDealt per seat (2 seats)");
        foreach (var record in holeCardRecords)
        {
            record.Level.Should().Be(LogLevel.Trace);
            record.Message.Should().StartWith("Hand 1 of game");
            record.Message.Should().Contain("was dealt");
        }

        var boardCardRecords = records.Where(record => record.Id.Id == 1101).ToList();
        boardCardRecords.Should().HaveCount(3, "one BoardDealt per street (flop, turn, river)");
        foreach (var record in boardCardRecords)
        {
            record.Level.Should().Be(LogLevel.Trace);
            record.Message.Should().StartWith("Hand 1 of game");
            record.Message.Should().Contain("dealt");
        }

        var messages = boardCardRecords.Select(record => record.Message).ToList();
        messages.Should().Contain(message => message.Contains("Flop"));
        messages.Should().Contain(message => message.Contains("Turn"));
        messages.Should().Contain(message => message.Contains("River"));
    }

    private static TestEngineHost.DisposableTestData Setup(bool enableSensitiveLogging, out HoldemEngine engine, out FakeLogCollector collector)
    {
        var fakeCollector = new FakeLogCollector();
        collector = fakeCollector;
        return TestEngineHost.Setup(out engine, out _,
            shuffler: new TestCardShuffler(
                getHoleFunc: (_, seatNo) => seatNo == 1 ? CardsFrom("ac", "kc") : CardsFrom("7d", "2h"),
                getDeckFunc: _ => CardsFrom("4s", "9h", "qd", "3c", "8s")),
            configureSettings: settings =>
            {
                settings.EnableSensitiveCardLogging = enableSensitiveLogging;
            },
            configureServices: services =>
            {
                services.AddLogging(builder => builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddProvider(new FakeLoggerProvider(fakeCollector)));
            });
    }

    /// <summary>EF Core logs through the same factory, so keep only what the engine wrote.</summary>
    private static IReadOnlyList<FakeLogRecord> EngineRecords(FakeLogCollector collector)
        => [.. collector.GetSnapshot().Where(record => record.Category == typeof(HoldemEngine).FullName)];

    private static async Task ApplyHandAsync(HoldemEngine engine)
    {
        var result1 = await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, _options));
        result1.IsSuccess.Should().BeTrue();

        var result2 = await engine.ApplyHoldemActionAsync(new HoldemActionStartGame(_gameId, _joinSeats));
        result2.IsSuccess.Should().BeTrue();

        var gameData = result2.Value;

        while (gameData.HandData != null && gameData.HandData.NextPlayerSeatNo != 0)
        {
            var evaluations = gameData.HandData.Evaluations;
            HoldemActionPlay action;
            if (evaluations.Any(e => e.PlayerActionType == PlayerActionType.Check))
            {
                action = new HoldemActionPlay(_gameId, PlayerActionType.Check, 0);
            }
            else if (evaluations.Any(e => e.PlayerActionType == PlayerActionType.Call))
            {
                action = new HoldemActionPlay(_gameId, PlayerActionType.Call, 0);
            }
            else if (evaluations.Any(e => e.PlayerActionType == PlayerActionType.Fold))
            {
                action = new HoldemActionPlay(_gameId, PlayerActionType.Fold, 0);
            }
            else
            {
                break;
            }

            var result = await engine.ApplyHoldemActionAsync(action);
            result.IsSuccess.Should().BeTrue($"action should be valid: {string.Join(", ", result.Errors.Select(e => e.Message))}");
            gameData = result.Value;
        }

        var resultFinish = await engine.ApplyHoldemActionAsync(new HoldemActionFinishGame(_gameId));
        resultFinish.IsSuccess.Should().BeTrue();
    }

    private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];
}
