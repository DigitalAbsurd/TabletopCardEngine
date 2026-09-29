using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Engine;
using OpenHoldemEngine.Demo.Phh;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
public sealed class PhhReplayLifecycle_Tests
{
    // pluribus/30/2.phh from the bundled sample: it reaches the turn, so the replay engine has to
    // deal the recorded board as well as the recorded hole cards.
    private const string SampleHand = """
        [1]
        variant = 'NT'
        antes = [0, 0, 0, 0, 0, 0]
        blinds_or_straddles = [50, 100, 0, 0, 0, 0]
        min_bet = 100
        starting_stacks = [10000, 10000, 10000, 10000, 10000, 10000]
        players = ['Budd', 'Eddie', 'Bill', 'Pluribus', 'MrWhite', 'Gogo']
        actions = ['d dh p1 2h3c', 'd dh p2 Jc4c', 'd dh p3 6c9c', 'd dh p4 8c4s', 'd dh p5 Jh4h', 'd dh p6 KsAd', 'p3 f', 'p4 f', 'p5 f', 'p6 cbr 200', 'p1 f', 'p2 cc', 'd db 5c9s7c', 'p2 cc', 'p6 cc', 'd db 3h', 'p2 cbr 675', 'p6 f']
        finishing_stacks = [9950, 10250, 10000, 10000, 10000, 9800]
        """;

    /// <summary>
    /// Builds a session the way the demo does: a play engine and a replay engine over one kept-alive
    /// connection, the replay engine reading the returned shuffler.
    /// </summary>
    private static (DemoGameSession Session, PhhCardShuffler Shuffler, HoldemEngine PlayEngine, DemoReplayEngine ReplayEngine) CreateTestSession()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var playEngine = CreateTestEngine(connection);
        var shuffler = new PhhCardShuffler();
        var replayEngine = DemoReplayEngine.Create(connection, shuffler);

        return (new DemoGameSession(playEngine, replayEngine), shuffler, playEngine, replayEngine);
    }

    private static HoldemEngine CreateTestEngine()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return CreateTestEngine(connection);
    }

    private static HoldemEngine CreateTestEngine(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddHoldemEngine(
            dbOptionsAction: opts => opts.UseSqlite(connection),
            configure: _ => { }
        );

        var serviceProvider = services.BuildServiceProvider();
        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();

        using (var dbContext = dbContextFactory.CreateDbContext())
        {
            dbContext.Database.EnsureCreated();
        }

        return serviceProvider.GetRequiredService<HoldemEngine>();
    }

    [TestMethod]
    public async Task LoadHand_SwitchesToTheReplayEngine_AndUnloadSwitchesBack()
    {
        // Arrange
        var (session, shuffler, playEngine, replayEngine) = CreateTestSession();
        var controller = new PhhReplayController(session, shuffler);
        session.ActiveEngine.Should().BeSameAs(playEngine, "a session starts on the play engine");

        // Act 1: Load a hand
        var loaded = await controller.LoadHandAsync(PhhReader.ReadAll(SampleHand)[0]);

        // Assert 1
        loaded.Should().BeTrue(controller.ReplayError);
        session.ActiveEngine.Should().BeSameAs(replayEngine.Engine, "a replay deals through the replay engine");
        session.IsReplaying.Should().BeTrue();

        // Act 2: Unload
        controller.Unload();

        // Assert 2
        session.ActiveEngine.Should().BeSameAs(playEngine, "unloading hands the session back to the play engine");
        session.IsReplaying.Should().BeFalse();
    }

    [TestMethod]
    public async Task AfterReplayingAHand_NewGameDealsAtRandomWithoutError()
    {
        // Arrange
        var (session, shuffler, playEngine, _) = CreateTestSession();
        var controller = new PhhReplayController(session, shuffler);
        var hand = PhhReader.ReadAll(SampleHand)[0];

        // Act 1: Replay the hand to the end
        (await controller.LoadHandAsync(hand)).Should().BeTrue(controller.ReplayError);
        session.CurrentHand!.Seats.Single(seat => seat.SeatNo == 6).Hole!
            .Select(card => card.ToString()).Should().Equal("ks", "ad");
        while (!controller.IsComplete)
        {
            (await controller.ReplayNextActionAsync()).Should().BeTrue(controller.ReplayError);
        }
        session.CurrentHand!.NextPlayerSeatNo.Should().Be(0, "the replayed hand is over");
        session.CurrentHand.CommunityCards.Select(card => card.ToString()).Should().Equal("5c", "9s", "7c", "3h");

        // Act 1b: Unload, as GameSetup.ApplyAsync does before a normal game
        controller.Unload();
        session.ActiveEngine.Should().BeSameAs(playEngine);

        // Act 2: Create a new game (through the replay engine this would fail: no deck is registered for it)
        var newGameId = Guid.NewGuid();
        var gameOptions = new GameOptionData { MaxPlayersCount = 2, InitialSmallBlind = 50m, InitialDealerSeatNo = 1 };

        var joinSeats = new PlayerJoinData[]
        {
            new(SeatNo: 1, Name: "Alice", BuyInAmount: 1000m),
            new(SeatNo: 2, Name: "Bob", BuyInAmount: 1000m),
        };

        var createResult = await session.CreateGameAsync(gameOptions, newGameId);
        createResult.Should().BeTrue("Game creation should succeed after unload");

        var startResult = await session.StartFirstHandAsync(joinSeats);
        startResult.Should().BeTrue("Hand start should succeed after unload with random cards");

        // Assert: Game is playable
        session.CurrentGame.Should().NotBeNull();
        session.CurrentHand.Should().NotBeNull();
        session.CurrentHand!.Seats.Should().HaveCount(2);
    }

    [TestMethod]
    public void EngineError_IsRaisedWhenExceptionOccurs()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        string? capturedErrorMessage = null;

        session.EngineError += (sender, args) =>
        {
            capturedErrorMessage = args.Message;
        };

        var badGameId = Guid.NewGuid();
        var options = new GameOptionData { MaxPlayersCount = 2, InitialSmallBlind = 50m, InitialDealerSeatNo = 1 };

        // Act 1: Create a game
        var createTask = session.CreateGameAsync(options, badGameId);
        createTask.Wait();
        createTask.Result.Should().BeTrue();

        // Act 2: Try to create a duplicate game (should fail with validation error)
        var badGameId2 = badGameId; // Use same ID
        var createDuplicateTask = session.CreateGameAsync(options, badGameId2);
        createDuplicateTask.Wait();

        // Assert: Error was captured
        capturedErrorMessage.Should().NotBeNullOrEmpty("EngineError event should have been raised");
        capturedErrorMessage.Should().Contain("already exists", "Error message should indicate duplicate game");
    }

    [TestMethod]
    public async Task ReplayBundledSampleHand3_FinishingStacksMatch()
    {
        // Arrange: Load hand 3 from the bundled sample
        var samplePath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "OpenHoldemEngine.Demo", "wwwroot", "sample", "sample.phhs");
        samplePath = Path.GetFullPath(samplePath);

        var phhsText = await File.ReadAllTextAsync(samplePath);

        var hands = PhhReader.ReadAll(phhsText);
        hands.Should().NotBeEmpty();
        var hand3 = hands.Count >= 3
            ? hands[2]
            : throw new InvalidOperationException("bundled sample should have at least 3 hands");

        // Set up the test session
        var (session, shuffler, _, _) = CreateTestSession();
        var controller = new PhhReplayController(session, shuffler);

        // Act: Load and replay hand 3 to completion
        var loaded = await controller.LoadHandAsync(hand3);
        loaded.Should().BeTrue(controller.ReplayError ?? "hand load failed");

        while (!controller.IsComplete)
        {
            var replayed = await controller.ReplayNextActionAsync();
            replayed.Should().BeTrue(controller.ReplayError ?? "action replay failed");
        }

        // Assert: Final stacks match the recorded finishing stacks
        controller.FinalStacksMatch.Should().BeTrue(controller.FinalStacksMessage);
    }
}
