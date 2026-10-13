using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TabletopCardEngine.Diagnostics;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The engine's OpenTelemetry activities: they are part of the contract, so they are pinned here one flow at a time.
/// Every test owns its listener, because the assembly runs tests in parallel and the ActivitySource is static.
/// </summary>
[TestClass]
public sealed class TracingTests
{
    private static readonly GameOptionData _options = new() { MaxPlayersCount = 2, InitialSmallBlind = 50M, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _joinSeats = [new("Button", 1, 10_000M), new("BigBlind", 2, 10_000M)];

    [TestMethod]
    public async Task Tracing_CreateGame_TagsActionType()
    {
        var gameId = Guid.NewGuid();
        using var host = Setup(gameId, out var engine, out var activities);

        var result = await engine.ApplyAsync(new HoldemActionCreateGame(gameId, _options));

        result.IsSuccess.Should().BeTrue();
        var activity = ActivitiesFor(gameId, activities).Should().ContainSingle().Subject;
        activity.GetTagItem(HoldemEngineActivity.ActionTypeTag).Should().Be("CreateGame");
        activity.GetTagItem(HoldemEngineActivity.OutcomeTag).Should().Be("Success");
    }

    [TestMethod]
    public async Task Tracing_GetGame_TagsActionType()
    {
        var gameId = Guid.NewGuid();
        using var host = Setup(gameId, out var engine, out var activities);
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        activities.Clear();

        var result = await engine.ApplyAsync(new HoldemActionGetGame(gameId));

        result.IsSuccess.Should().BeTrue();
        var activity = ActivitiesFor(gameId, activities).Should().ContainSingle().Subject;
        activity.GetTagItem(HoldemEngineActivity.ActionTypeTag).Should().Be("GetGame");
        activity.GetTagItem(HoldemEngineActivity.OutcomeTag).Should().Be("Success");
    }

    [TestMethod]
    public async Task Tracing_StartGame_TagsActionTypeAndStreet()
    {
        var gameId = Guid.NewGuid();
        using var host = Setup(gameId, out var engine, out var activities);
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        activities.Clear();

        var result = await engine.ApplyAsync(new HoldemActionStartGame(gameId, _joinSeats));

        result.IsSuccess.Should().BeTrue();
        var activity = ActivitiesFor(gameId, activities).Should().ContainSingle().Subject;
        activity.GetTagItem(HoldemEngineActivity.ActionTypeTag).Should().Be("StartGame");
        activity.GetTagItem(HoldemEngineActivity.OutcomeTag).Should().Be("Success");
        activity.GetTagItem(HoldemEngineActivity.HandNumberTag).Should().Be(1);
        activity.GetTagItem(HoldemEngineActivity.StreetTag).Should().Be(Street.PreFlop.ToString());
    }

    [TestMethod]
    public async Task Tracing_Play_TagsActionTypePlayerActionTypeAndStreet()
    {
        var gameId = Guid.NewGuid();
        using var host = Setup(gameId, out var engine, out var activities);
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
        activities.Clear();

        var result = await engine.ApplyAsync(new HoldemActionPlay(gameId, PlayerActionType.Fold, 0));

        result.IsSuccess.Should().BeTrue();
        var activity = ActivitiesFor(gameId, activities).Should().ContainSingle().Subject;
        activity.GetTagItem(HoldemEngineActivity.ActionTypeTag).Should().Be("Play");
        activity.GetTagItem(HoldemEngineActivity.PlayerActionTypeTag).Should().Be(PlayerActionType.Fold.ToString());
        activity.GetTagItem(HoldemEngineActivity.OutcomeTag).Should().Be("Success");
        activity.GetTagItem(HoldemEngineActivity.HandNumberTag).Should().Be(1);
        activity.GetTagItem(HoldemEngineActivity.StreetTag).Should().Be(Street.Finished.ToString());
    }

    [TestMethod]
    public async Task Tracing_ActionRejected_TagsOutcomeFailureAndErrorCode()
    {
        var gameId = Guid.NewGuid();
        using var host = Setup(gameId, out var engine, out var activities);
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        activities.Clear();

        var result = await engine.ApplyAsync(new HoldemActionStartHand(gameId));

        result.IsSuccess.Should().BeFalse();
        var activity = ActivitiesFor(gameId, activities).Should().ContainSingle().Subject;
        activity.GetTagItem(HoldemEngineActivity.ActionTypeTag).Should().Be("StartHand");
        activity.GetTagItem(HoldemEngineActivity.OutcomeTag).Should().Be("Failure");
        activity.GetTagItem(HoldemEngineActivity.ErrorCodeTag).Should().Be(HoldemErrorCode.GameNotStarted.ToString());
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [TestMethod]
    public async Task Tracing_ConcurrencyConflict_TagsOutcomeFailureAndConflictErrorCode()
    {
        var gameId = Guid.NewGuid();
        using var host = Setup(gameId, out var engine, out var activities);
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        activities.Clear();

        var result = await engine.ApplyAsync(new HoldemActionStartGame(gameId, _joinSeats) { ExpectedVersion = 99 });

        result.IsSuccess.Should().BeFalse();
        var activity = ActivitiesFor(gameId, activities).Should().ContainSingle().Subject;
        activity.GetTagItem(HoldemEngineActivity.OutcomeTag).Should().Be("Failure");
        activity.GetTagItem(HoldemEngineActivity.ErrorCodeTag).Should().Be(HoldemErrorCode.Conflict.ToString());
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [TestMethod]
    public async Task Tracing_EfCommands_RunInsideTheEngineSpan()
    {
        var gameId = Guid.NewGuid();
        var engineSpanOperationName = string.Empty;
        var efCommandsRunInsideEngineSpan = true;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TabletopCardEngineTelemetry.ActivitySourceName || source.Name == "Microsoft.EntityFrameworkCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.Source.Name == TabletopCardEngineTelemetry.ActivitySourceName)
                {
                    engineSpanOperationName = activity.OperationName;
                }
            }
        };

        ActivitySource.AddActivityListener(listener);

        using var host = Setup(gameId, out var engine, out var activities);
        var diag = new DiagnosticListener("Microsoft.EntityFrameworkCore");
        var efSubscription = diag.Subscribe(
            new DiagnosticObserver(
                onNext: e =>
                {
                    if (e.Key == "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting")
                    {
                        var current = Activity.Current;
                        if (current?.OperationName != engineSpanOperationName)
                        {
                            efCommandsRunInsideEngineSpan = false;
                        }
                    }
                }),
            eventName => eventName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));

        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));

        efCommandsRunInsideEngineSpan.Should().BeTrue();
        listener.Dispose();
        efSubscription.Dispose();
    }

    [TestMethod]
    public async Task Tracing_Exception_MarksTheSpanAsError()
    {
        var gameId = Guid.NewGuid();

        var throwingShuffler = new ThrowingShuffler();
        using var host = TestEngineHost.Setup(
            out var engine,
            out _,
            shuffler: throwingShuffler,
            configureServices: services =>
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Critical)));

        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TabletopCardEngineTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.GetTagItem(HoldemEngineActivity.GameIdTag)?.ToString() == gameId.ToString("D"))
                {
                    activities.Add(activity);
                }
            }
        };

        ActivitySource.AddActivityListener(listener);

        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
        activities.Clear();

        var playAction = new HoldemActionPlay(gameId, PlayerActionType.Check, 0);
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => engine.ApplyAsync(playAction));
        exception.Message.Should().Be("Deck threw");

        var activity = ActivitiesFor(gameId, activities).Should().ContainSingle().Subject;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem(HoldemEngineActivity.OutcomeTag).Should().Be("Exception");
        activity.Events.Should().Contain(e => e.Name == "exception");
    }

    private sealed class ThrowingShuffler : ICardShuffler
    {
        private int _callCount;

        public ValueTask<IReadOnlyList<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo, CancellationToken cancellationToken)
        {
            _callCount++;
            return ValueTask.FromResult<IReadOnlyList<Card>>(
                seatNo == 1 ? new[] { Card.Parse("ac"), Card.Parse("kc") } : new[] { Card.Parse("7d"), Card.Parse("2h") });
        }

        public ValueTask<IReadOnlyList<Card>> GetDeckCardsAsync(Guid gameId, int handNo, CancellationToken cancellationToken)
        {
            _callCount++;
            if (_callCount > 1)
            {
                throw new InvalidOperationException("Deck threw");
            }

            return ValueTask.FromResult<IReadOnlyList<Card>>(new[] { Card.Parse("4s"), Card.Parse("9h"), Card.Parse("qd"), Card.Parse("3c"), Card.Parse("8s") });
        }
    }

    [TestMethod]
    public async Task Tracing_GetGame_NoHandYet_DoesNotTagHandNumber()
    {
        var gameId = Guid.NewGuid();
        using var host = Setup(gameId, out var engine, out var activities);
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        activities.Clear();

        var result = await engine.ApplyAsync(new HoldemActionGetGame(gameId));

        result.IsSuccess.Should().BeTrue();
        var activity = ActivitiesFor(gameId, activities).Should().ContainSingle().Subject;
        activity.GetTagItem(HoldemEngineActivity.HandNumberTag).Should().BeNull();
    }

    [TestMethod]
    public async Task Tracing_OneSpanPerCall()
    {
        var gameId = Guid.NewGuid();
        using var host = Setup(gameId, out var engine, out var activities);

        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
        await ApplyAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Fold, 0));

        var operationNames = activities.Select(a => a.OperationName).ToList();
        operationNames.Should().HaveCount(3);
        operationNames.Should().AllBe(HoldemEngineActivity.Name);
    }

    private sealed class DiagnosticObserver(Action<KeyValuePair<string, object?>> onNext) : IObserver<KeyValuePair<string, object?>>
    {
        public void OnNext(KeyValuePair<string, object?> value) => onNext(value);

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    private static SetupResult Setup(Guid gameId, out IHoldemTable engine, out List<Activity> activities)
    {
        var activitiesList = new List<Activity>();
        activities = activitiesList;

        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TabletopCardEngineTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.GetTagItem(HoldemEngineActivity.GameIdTag)?.ToString() == gameId.ToString("D"))
                {
                    activitiesList.Add(activity);
                }
            }
        };

        ActivitySource.AddActivityListener(listener);

        var host = TestEngineHost.Setup(
            out engine,
            out _,
            shuffler: new TestCardShuffler(
                getHoleFunc: (_, seatNo) => seatNo == 1 ? CardsFrom("ac", "kc") : CardsFrom("7d", "2h"),
                getDeckFunc: _ => CardsFrom("4s", "9h", "qd", "3c", "8s")),
            configureServices: services => services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Critical)));

        return new SetupResult(host, listener);
    }

    private sealed record SetupResult(IDisposable Host, ActivityListener Listener) : IDisposable
    {
        public void Dispose()
        {
            Host.Dispose();
            Listener.Dispose();
        }
    }

    /// <summary>Return all activities for a specific game, filtered by game.id tag.</summary>
    private static List<Activity> ActivitiesFor(Guid gameId, IEnumerable<Activity> activities)
        => activities
            .Where(activity => activity.GetTagItem(HoldemEngineActivity.GameIdTag)?.ToString() == gameId.ToString("D"))
            .ToList();

    private static async Task ApplyAsync(IHoldemTable engine, HoldemAction action)
    {
        var result = await engine.ApplyAsync(action);
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
    }

    private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];
}
