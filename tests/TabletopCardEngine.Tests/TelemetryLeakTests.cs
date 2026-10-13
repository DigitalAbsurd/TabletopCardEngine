using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using TabletopCardEngine.Diagnostics;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.OpenTelemetry;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The milestone's gap-closing gate: across logs, spans and metrics, over one whole six-handed hand
/// to showdown, no dealt card reaches a signal it must not. The earlier telemetry tests each cover one
/// signal; this one asserts the property none of them could see alone. The corpus is deliberately
/// everything a host could read (messages, structured state, scopes, exception text, every tag and
/// event), and the search is a whole-token match per dealt card, so it is neither narrowed to pass
/// nor vacuous (the with-setting test proves it finds the cards where they are allowed).
/// </summary>
[TestClass]
public sealed class TelemetryLeakTests
{
    private const int SeatCount = 6;
    private const int HoleCardEventId = 1100;
    private const int BoardCardEventId = 1101;

    private static readonly GameOptionData _options = new() { MaxPlayersCount = SeatCount, InitialSmallBlind = 50M, InitialDealerSeatNo = 1 };

    /// <summary>Twelve hole cards, two per seat in seat order, then five board cards: seventeen distinct cards.</summary>
    private static readonly Card[] _holeCards = CardsFrom("ah", "kd", "qc", "jh", "ts", "9d", "8c", "7h", "6s", "5d", "4c", "3h");

    private static readonly Card[] _board = CardsFrom("2s", "9c", "td", "jc", "qd");

    [TestMethod]
    public async Task Telemetry_WithoutTheSetting_LeaksNoCards()
    {
        var run = await RunHandAsync(enableSensitiveCardLogging: false);

        run.LogRecords.Should().NotBeEmpty("a search over an empty log proves nothing");
        run.Spans.Should().NotBeEmpty();
        run.MetricTexts.Should().NotBeEmpty();
        var allCards = _holeCards.Concat(_board).ToList();

        foreach (var record in run.LogRecords)
        {
            TelemetryLeakSearch.FindCards(record.Texts, allCards).Should().BeEmpty(
                "log event {0} ({1}) at {2} must not carry cards without the setting: {3}", record.EventId, record.EventName, record.Level, record.Texts.FirstOrDefault());
        }

        TelemetryLeakSearch.FindCards(run.SpanTexts, allCards).Should().BeEmpty("no span tag or event may carry a card");
        TelemetryLeakSearch.FindCards(run.MetricTexts, allCards).Should().BeEmpty("no metric tag may carry a card");
    }

    [TestMethod]
    public async Task Telemetry_WithTheSetting_KeepsCardsAtTrace()
    {
        var run = await RunHandAsync(enableSensitiveCardLogging: true);
        var allCards = _holeCards.Concat(_board).ToList();

        var foundAtTrace = new HashSet<Card>();
        var tokenCountAtTrace = 0;
        foreach (var record in run.LogRecords)
        {
            var found = TelemetryLeakSearch.FindCards(record.Texts, allCards);
            if (found.Count == 0)
            {
                continue;
            }

            record.Level.Should().Be(LogLevel.Trace, "cards may only appear at Trace (event {0}: {1})", record.EventId, record.Texts.FirstOrDefault());
            record.EventId.Should().BeOneOf([HoleCardEventId, BoardCardEventId], "only the two opt-in card events may carry cards");
            foundAtTrace.UnionWith(found);
            tokenCountAtTrace += found.Count;
        }

        Console.WriteLine($"Card tokens found in Trace card records: {tokenCountAtTrace} (distinct cards: {foundAtTrace.Count} of {allCards.Count})");
        foundAtTrace.Should().BeEquivalentTo(allCards, "the Trace records must contain every dealt card, or the search is vacuous");
        tokenCountAtTrace.Should().BeGreaterThan(_holeCards.Length + _board.Length - 1);

        TelemetryLeakSearch.FindCards(run.SpanTexts, allCards).Should().BeEmpty("no span may carry a card, even with the setting on");
        TelemetryLeakSearch.FindCards(run.MetricTexts, allCards).Should().BeEmpty("no metric tag may carry a card, even with the setting on");
    }

    [TestMethod]
    public void LeakSearch_FindsAPlantedCard()
    {
        var ah = Card.Parse("ah");
        var kd = Card.Parse("kd");

        TelemetryLeakSearch.FindCards(["seat 3 holds ah, kd"], [ah, kd]).Should().BeEquivalentTo([ah, kd]);
        TelemetryLeakSearch.FindCards(["Seat 3 holds Ah"], [ah]).Should().BeEquivalentTo([ah], "the PHH spelling must match too");
        TelemetryLeakSearch.FindCards(["[ah]", "dealt:kd."], [ah, kd]).Should().BeEquivalentTo([ah, kd]);

        TelemetryLeakSearch.FindCards(
            ["game 3fah2d0c-1111-4222-8333-444455556666", "shadow", "Kdx", "ahead", "xah", "9kd"],
            [ah, kd]).Should().BeEmpty("the same letters inside other words and GUIDs are not a card");
    }

    private static async Task<LeakRun> RunHandAsync(bool enableSensitiveCardLogging)
    {
        var gameId = Guid.NewGuid();
        var collector = new FakeLogCollector();
        var exported = new List<Activity>();
        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddTabletopCardEngineInstrumentation()
            .AddInMemoryExporter(exported)
            .Build();

        var metricTexts = new List<string>();
        using (var host = TestEngineHost.Setup(out var engine, out _,
            shuffler: new TestCardShuffler(
                getHoleFunc: (_, seatNo) => [_holeCards[(seatNo - 1) * 2], _holeCards[((seatNo - 1) * 2) + 1]],
                getDeckFunc: _ => _board),
            configureSettings: settings => settings.EnableSensitiveCardLogging = enableSensitiveCardLogging,
            configureServices: services =>
            {
                services.AddMetrics();
                services.AddLogging(builder => builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddProvider(new FakeLoggerProvider(collector)));
            }))
        {
            var meterFactory = host.GetRequiredService<IMeterFactory>();
            var collectors = DiscoverEngineInstruments(meterFactory);
            collectors.Should().NotBeEmpty("the engine's instruments must be discovered, not hand-listed");

            await PlayHandToShowdownAsync(engine, gameId);

            foreach (var metricCollector in collectors)
            {
                metricTexts.AddRange(metricCollector.TagTexts());
            }

            collectors.Sum(metricCollector => metricCollector.MeasurementCount).Should().BePositive("a metric search with no measurements proves nothing");
        }

        tracerProvider.Dispose();

        var spans = exported
            .Where(activity => activity.GetTagItem(HoldemEngineActivity.GameIdTag)?.ToString() == gameId.ToString("D"))
            .ToList();

        // EF Core logs through the same factory; its categories are the host's configuration, not the engine's.
        var logRecords = collector.GetSnapshot()
            .Where(record => record.Category?.StartsWith("TabletopCardEngine", StringComparison.Ordinal) == true)
            .Select(record => new LogEntry(record.Id.Id, record.Id.Name, record.Level, LogTexts(record)))
            .ToList();

        return new LeakRun(logRecords, spans, metricTexts);
    }

    private static async Task PlayHandToShowdownAsync(IHoldemTable engine, Guid gameId)
    {
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        var gameData = await ApplyAsync(engine, new HoldemActionStartGame(
            gameId, [.. Enumerable.Range(1, SeatCount).Select(seatNo => new PlayerJoinData($"Player{seatNo}", (byte)seatNo, 10_000M))]));

        // Everybody calls and checks down, so all four streets and a six-way showdown happen.
        while (gameData.HandData!.NextPlayerSeatNo != 0)
        {
            var canCall = gameData.HandData.Evaluations.Any(evaluation => evaluation.PlayerActionType == PlayerActionType.Call);
            gameData = await ApplyAsync(engine, new HoldemActionPlay(gameId, canCall ? PlayerActionType.Call : PlayerActionType.Check, 0));
        }

        gameData.HandData.CommunityCards.Should().HaveCount(_board.Length);
        gameData.HandData.Seats.Should().OnlyContain(seat => seat.HandResult != null && seat.HandResult.Description != null, "all six seats must reach showdown");
    }

    private static async Task<GameData> ApplyAsync(IHoldemTable engine, HoldemAction action)
    {
        var result = await engine.ApplyAsync(action);
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
        return result.Value;
    }

    /// <summary>Finds every instrument of this container's engine meter with a listener, rather than from a hand-kept list.</summary>
    private static List<InstrumentCollector> DiscoverEngineInstruments(IMeterFactory meterFactory)
    {
        var instruments = new List<Instrument>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, _) =>
            {
                if (instrument.Meter.Name == TabletopCardEngineTelemetry.MeterName && ReferenceEquals(instrument.Meter.Scope, meterFactory))
                {
                    instruments.Add(instrument);
                }
            }
        };
        listener.Start();

        return [.. instruments.Select(InstrumentCollector.Create)];
    }

    private static List<string> LogTexts(FakeLogRecord record)
    {
        var texts = new List<string> { record.Message };
        if (record.StructuredState != null)
        {
            foreach (var pair in record.StructuredState)
            {
                texts.Add(pair.Key);
                texts.Add(pair.Value ?? string.Empty);
            }
        }

        foreach (var scope in record.Scopes)
        {
            AddValue(texts, scope);
        }

        if (record.Exception != null)
        {
            texts.Add(record.Exception.ToString());
        }

        return texts;
    }

    private static void AddValue(List<string> texts, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case string text:
                texts.Add(text);
                break;
            case System.Collections.IEnumerable items:
                foreach (var item in items)
                {
                    AddValue(texts, item);
                }

                break;
            default:
                if (value.GetType() is { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    AddValue(texts, type.GetProperty("Key")!.GetValue(value));
                    AddValue(texts, type.GetProperty("Value")!.GetValue(value));
                }
                else
                {
                    texts.Add(value.ToString() ?? string.Empty);
                }

                break;
        }
    }

    private static List<string> SpanTextsOf(IEnumerable<Activity> spans)
    {
        var texts = new List<string>();
        foreach (var span in spans)
        {
            texts.Add(span.DisplayName);
            texts.Add(span.StatusDescription ?? string.Empty);
            foreach (var tag in span.TagObjects)
            {
                AddValue(texts, tag.Value);
            }

            foreach (var spanEvent in span.Events)
            {
                texts.Add(spanEvent.Name);
                foreach (var tag in spanEvent.Tags)
                {
                    AddValue(texts, tag.Value);
                }
            }
        }

        return texts;
    }

    private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];

    private sealed record LogEntry(int EventId, string? EventName, LogLevel Level, List<string> Texts);

    private sealed class LeakRun(List<LogEntry> logRecords, List<Activity> spans, List<string> metricTexts)
    {
        internal List<LogEntry> LogRecords { get; } = logRecords;

        internal List<Activity> Spans { get; } = spans;

        internal List<string> SpanTexts { get; } = SpanTextsOf(spans);

        internal List<string> MetricTexts { get; } = metricTexts;
    }

    /// <summary>A <see cref="MetricCollector{T}"/> of either numeric type behind one interface.</summary>
    private abstract class InstrumentCollector
    {
        internal abstract int MeasurementCount { get; }

        internal abstract IEnumerable<string> TagTexts();

        internal static InstrumentCollector Create(Instrument instrument) => instrument switch
        {
            Instrument<long> longInstrument => new Typed<long>(new MetricCollector<long>(longInstrument)),
            Instrument<double> doubleInstrument => new Typed<double>(new MetricCollector<double>(doubleInstrument)),
            _ => throw new NotSupportedException($"The test has no collector for {instrument.GetType()}."),
        };

        private sealed class Typed<T>(MetricCollector<T> collector) : InstrumentCollector where T : struct, IEquatable<T>
        {
            internal override int MeasurementCount => collector.GetMeasurementSnapshot().Count;

            internal override IEnumerable<string> TagTexts()
                => collector.GetMeasurementSnapshot()
                    .SelectMany(measurement => measurement.Tags.Select(tag => tag.Value?.ToString() ?? string.Empty));
        }
    }
}
