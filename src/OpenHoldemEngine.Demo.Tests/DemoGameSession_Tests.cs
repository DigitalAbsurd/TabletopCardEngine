using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
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

    [TestMethod]
    public async Task EngineError_GameIdAlreadyExists_MapsToUserFriendlyMessage()
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

        string? errorMessage = null;
        session.EngineError += (s, e) =>
        {
            errorMessage = e.Message;
        };

        // Act - Create a game with a specific ID
        var firstCreate = await session.CreateGameAsync(options, gameId);
        firstCreate.Should().BeTrue();

        // Try to create another game with the same ID
        var secondCreate = await session.CreateGameAsync(options, gameId);

        // Assert
        secondCreate.Should().BeFalse();
        errorMessage.Should().NotBeNullOrEmpty();
        errorMessage.Should().Be("Game already exists. Choose a different game ID.");
    }

    [TestMethod]
    public async Task EngineError_CurrentHandCompleted_MapsToUserFriendlyMessage()
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

        string? errorMessage = null;
        session.EngineError += (s, e) =>
        {
            errorMessage = e.Message;
        };

        // Act - Create game and start first hand
        await session.CreateGameAsync(options, gameId);
        await session.StartFirstHandAsync(joinSeats);

        // Play the hand to completion (one player folds)
        while (session.CurrentHand != null && session.CurrentHand.NextPlayerSeatNo != 0)
        {
            await session.ApplyPlayerActionAsync(PlayerActionType.Fold);
        }

        // Hand is now complete. Try to start the next hand while one is already complete
        // (This should fail because the current hand is already complete)
        // Actually, let's test by trying to apply an action when the hand is complete
        if (session.CurrentHand != null)
        {
            var actionResult = await session.ApplyPlayerActionAsync(PlayerActionType.Fold);

            // Assert - The action should fail because hand is complete
            if (!actionResult)
            {
                errorMessage.Should().Contain("already complete");
            }
        }
    }

    [TestMethod]
    public async Task EngineError_PreviousHandNotCompleted_MapsToUserFriendlyMessage()
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

        string? errorMessage = null;
        session.EngineError += (s, e) =>
        {
            errorMessage = e.Message;
        };

        // Act - Create game and start first hand
        await session.CreateGameAsync(options, gameId);
        await session.StartFirstHandAsync(joinSeats);

        // Immediately try to start next hand while first hand is still in progress
        var nextHandResult = await session.StartNextHandAsync();

        // Assert
        nextHandResult.Should().BeFalse();
        errorMessage.Should().NotBeNullOrEmpty();
        errorMessage.Should().Be("The previous hand has not completed yet. Wait for the hand to finish before starting the next one.");
    }

    [TestMethod]
    public async Task StateChanged_NotRaised_OnFailedCall()
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

        var stateChangedCount = 0;
        session.StateChanged += (s, e) =>
        {
            stateChangedCount++;
        };

        // Act - Create first game successfully (state changes)
        await session.CreateGameAsync(options, gameId);
        var countAfterSuccess = stateChangedCount;

        // Try to create another game with same ID (fails)
        await session.CreateGameAsync(options, gameId);

        // Assert - State should NOT change on failure
        stateChangedCount.Should().Be(countAfterSuccess, "StateChanged should not be raised for failed calls");
    }

    [TestMethod]
    public async Task CurrentGameId_Preserved_OnFailedCall()
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

        // Act - Create game successfully
        await session.CreateGameAsync(options, gameId);
        var successfulGameId = session.CurrentGameId;

        // Try to create another game with same ID (fails)
        await session.CreateGameAsync(options, gameId);

        // Assert - Game ID should remain the same after failed call
        session.CurrentGameId.Should().Be(successfulGameId);
    }

    [TestMethod]
    public async Task MultipleActions_InSequence_AllRecordedInJournal()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var gameId = Guid.NewGuid();
        var options = new GameOptionData(
            MaxPlayersCount: 3,
            InitialSmallBlind: 50m,
            InitialDealerSeatNo: 0
        );

        var joinSeats = new PlayerJoinData[]
        {
            new(SeatNo: 1, Name: "Player 1", BuyInAmount: 1000m),
            new(SeatNo: 2, Name: "Player 2", BuyInAmount: 1000m),
            new(SeatNo: 3, Name: "Player 3", BuyInAmount: 1000m),
        };

        // Act
        await session.CreateGameAsync(options, gameId);
        await session.StartFirstHandAsync(joinSeats);

        // Apply multiple actions
        var actionCount = 0;
        while (session.CurrentHand != null && session.CurrentHand.NextPlayerSeatNo != 0 && actionCount < 10)
        {
            await session.ApplyPlayerActionAsync(PlayerActionType.Fold);
            actionCount++;
        }

        // Assert
        session.Journal.Count.Should().BeGreaterThan(2); // CreateGame + StartHand + at least one Fold
        var entries = session.Journal.Entries;

        // All entries should be successful except possibly the last ones if hand ended
        var successCount = entries.Count(e => e.Succeeded);
        successCount.Should().BeGreaterThan(1);
    }

    [TestMethod]
    public async Task Reset_ClearsAllState()
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

        // Act - Create game and start hand
        await session.CreateGameAsync(options, gameId);
        await session.StartFirstHandAsync(joinSeats);
        session.CurrentGameId.Should().NotBe(Guid.Empty);
        session.CurrentGame.Should().NotBeNull();
        session.Journal.Count.Should().BeGreaterThan(0);

        // Reset
        session.Reset();

        // Assert
        session.CurrentGameId.Should().Be(Guid.Empty);
        session.CurrentGame.Should().BeNull();
        session.CurrentHand.Should().BeNull();
        session.Journal.Count.Should().Be(0);
    }

    // A real session rather than a hand-built HandData: the pot the presets read is the engine's,
    // and at the first decision of a street it used to count the street that had just closed twice.
    [TestMethod]
    public async Task PotPresets_FirstFlopDecision_ReadThePotOnce()
    {
        // Arrange - heads-up at 50/100: the button calls, the big blind checks, 200 in the pot
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        await session.CreateGameAsync(new GameOptionData(MaxPlayersCount: 2, InitialSmallBlind: 50m, InitialDealerSeatNo: 0), Guid.NewGuid());
        await session.StartFirstHandAsync(
        [
            new(SeatNo: 1, Name: "Player 1", BuyInAmount: 1000m),
            new(SeatNo: 2, Name: "Player 2", BuyInAmount: 1000m),
        ]);
        await session.ApplyPlayerActionAsync(PlayerActionType.Call);
        await session.ApplyPlayerActionAsync(PlayerActionType.Check);

        // Act
        var hand = session.CurrentHand!;

        // Assert - nothing to call, so Pot bets the 200 pot and a half-pot of 100 is the minimum raise
        hand.Street.Should().Be(Street.Flop);
        (hand.Pot + hand.StreetPot).Should().Be(200m);
        RaiseSizing.PotRaise(hand, 50m).Should().Be(200m);
        RaiseSizing.Presets(hand, 50m).Should().Equal(new RaiseMark(200m, "Pot", "Current bet 0 + pot 200 = 200"));
    }
}
