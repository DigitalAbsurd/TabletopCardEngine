using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DemoGameSession_Tests
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
    public async Task CreateGame_SetsCurrentGameId()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var options = new GameOptionData(
            MaxPlayersCount: 2,
            InitialSmallBlind: 50m,
            InitialDealerSeatNo: 0
        );

        // Act
        var result = await session.CreateGameAsync(options);

        // Assert
        result.Should().BeTrue();
        session.CurrentGameId.Should().NotBe(Guid.Empty);
        session.CurrentGame.Should().NotBeNull();
        session.CurrentHand.Should().BeNull(); // No hand yet
    }

    [TestMethod]
    public async Task StartFirstHand_WithSeats_DealsHand()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var gameId = Guid.NewGuid();
        var options = new GameOptionData(
            MaxPlayersCount: 2,
            InitialSmallBlind: 50m,
            InitialDealerSeatNo: 0
        );

        var joinSeats = new PlayerJoinData[]
        {
            new(SeatNo: 1, Name: "Player 1", BuyInAmount: 1000m),
            new(SeatNo: 2, Name: "Player 2", BuyInAmount: 1000m),
        };

        // Act
        var gameCreated = await session.CreateGameAsync(options, gameId);
        gameCreated.Should().BeTrue();

        var result = await session.StartFirstHandAsync(joinSeats);

        // Assert
        result.Should().BeTrue();
        session.CurrentHand.Should().NotBeNull();
        session.CurrentHand!.No.Should().Be(1);
        session.CurrentHand.Seats.Should().HaveCount(2);
        session.CurrentHand.NextPlayerSeatNo.Should().NotBe(0);
    }

    [TestMethod]
    public async Task JournalRecords_CreateGame_StartHand_PlayerAction()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var gameId = Guid.NewGuid();
        var options = new GameOptionData(
            MaxPlayersCount: 2,
            InitialSmallBlind: 50m,
            InitialDealerSeatNo: 0
        );

        var joinSeats = new PlayerJoinData[]
        {
            new(SeatNo: 1, Name: "Player 1", BuyInAmount: 1000m),
            new(SeatNo: 2, Name: "Player 2", BuyInAmount: 1000m),
        };

        // Act
        await session.CreateGameAsync(options, gameId);
        await session.StartFirstHandAsync(joinSeats);
        await session.ApplyPlayerActionAsync(PlayerActionType.Fold);

        // Assert
        session.Journal.Count.Should().Be(3);

        var entries = session.Journal.Entries;
        entries[0].SequenceNumber.Should().Be(1);
        entries[0].Action.Should().BeOfType<HoldemActionCreateGame>();
        entries[0].Succeeded.Should().BeTrue();
        entries[0].ErrorMessages.Should().BeEmpty();

        entries[1].SequenceNumber.Should().Be(2);
        entries[1].Action.Should().BeOfType<HoldemActionStartHand>();
        entries[1].Succeeded.Should().BeTrue();
        entries[1].ErrorMessages.Should().BeEmpty();

        entries[2].SequenceNumber.Should().Be(3);
        entries[2].Action.Should().BeOfType<HoldemActionPlayerAction>();
        entries[2].Succeeded.Should().BeTrue();
        entries[2].ErrorMessages.Should().BeEmpty();
    }

    [TestMethod]
    public async Task FailedAction_IsRecordedInJournal_WithErrorMessages()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var gameId = Guid.NewGuid();
        var options = new GameOptionData(
            MaxPlayersCount: 2,
            InitialSmallBlind: 50m,
            InitialDealerSeatNo: 0
        );

        // Act
        await session.CreateGameAsync(options, gameId);

        // Try to create a game with the same ID - this should fail with validation error
        var result = await session.CreateGameAsync(options, gameId);

        // Assert
        result.Should().BeFalse();
        session.Journal.Count.Should().Be(2);

        var failedEntry = session.Journal.Entries[1];
        failedEntry.Succeeded.Should().BeFalse();
        failedEntry.ErrorMessages.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task StateChanged_EventRaised_OnSuccessfulCall()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var options = new GameOptionData(
            MaxPlayersCount: 2,
            InitialSmallBlind: 50m,
            InitialDealerSeatNo: 0
        );

        var eventRaised = false;
        session.StateChanged += (s, e) =>
        {
            eventRaised = true;
        };

        // Act
        await session.CreateGameAsync(options);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [TestMethod]
    public async Task EngineError_EventRaised_OnFailedCall()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var options = new GameOptionData(
            MaxPlayersCount: 2,
            InitialSmallBlind: 50m,
            InitialDealerSeatNo: 0
        );

        string? errorMessage = null;
        session.EngineError += (s, e) =>
        {
            errorMessage = e.Message;
        };

        // Act
        await session.CreateGameAsync(options, Guid.NewGuid());
        var secondAttempt = await session.CreateGameAsync(options, session.CurrentGameId);

        // Assert
        secondAttempt.Should().BeFalse();
        errorMessage.Should().NotBeNullOrEmpty();
        errorMessage.Should().Contain("already exists");
    }
}
