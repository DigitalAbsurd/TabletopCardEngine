using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Phh;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PhhReplayLifecycle_Tests
{
    private static HoldemEngine CreateTestEngine()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

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
    public void Unload_ClearsShufflerFromOptions()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var shuffler = new PhhCardShuffler();
        var controller = new PhhReplayController(session, shuffler);

        // Simulate installing the shuffler (like LoadHandAsync does)
        _ = new HoldemEngineOptionsBuilder().WithCardShuffler(shuffler);

        HoldemEngineOptions.Instance.CardShuffler.Should().NotBeNull("Shuffler should be installed before unload");

        // Act
        controller.Unload();

        // Assert
        HoldemEngineOptions.Instance.CardShuffler.Should().BeNull("Shuffler should be null after unload");
    }

    [TestMethod]
    public async Task AfterUnload_NewGameCanCreateAndStartWithoutError()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var shuffler = new PhhCardShuffler();
        var controller = new PhhReplayController(session, shuffler);

        // Simulate installing the shuffler
        _ = new HoldemEngineOptionsBuilder().WithCardShuffler(shuffler);
        HoldemEngineOptions.Instance.CardShuffler.Should().NotBeNull();

        // Act 1: Unload
        controller.Unload();
        HoldemEngineOptions.Instance.CardShuffler.Should().BeNull();

        // Act 2: Create a new game (this would fail if shuffler was still installed)
        var newGameId = Guid.NewGuid();
        var gameOptions = new GameOptionData(
            MaxPlayersCount: 2,
            InitialSmallBlind: 50m,
            InitialDealerSeatNo: 0
        );

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
        var options = new GameOptionData(
            MaxPlayersCount: 2,
            InitialSmallBlind: 50m,
            InitialDealerSeatNo: 0
        );

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
}
