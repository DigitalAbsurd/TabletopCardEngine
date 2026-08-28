using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Phh;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PhhRoundTrip_Tests
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
    public async Task WriteHand_ProducesValidPhh_WithRequiredSections()
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
            new(SeatNo: 1, Name: "Alice", BuyInAmount: 1000m),
            new(SeatNo: 2, Name: "Bob", BuyInAmount: 1000m),
        };

        // Act - Play a short hand
        await session.CreateGameAsync(options, gameId);
        await session.StartFirstHandAsync(joinSeats);
        await session.ApplyPlayerActionAsync(PlayerActionType.Fold);

        // Export
        var phhHand = PhhWriter.WriteHand(session.CurrentGame!, session.Journal);

        // Assert - Check for required PHH sections
        phhHand.Should().Contain("[Game]");
        phhHand.Should().Contain("variant = \"NT\"");
        phhHand.Should().Contain("antes = ");
        phhHand.Should().Contain("blinds_or_straddles = ");
        phhHand.Should().Contain("min_bet = ");
        phhHand.Should().Contain("starting_stacks = ");
        phhHand.Should().Contain("players = ");
        phhHand.Should().Contain("actions =");
    }

    [TestMethod]
    public async Task WriteSession_ProducesValidPhhSession_WithNumberedTables()
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
            new(SeatNo: 1, Name: "Alice", BuyInAmount: 1000m),
            new(SeatNo: 2, Name: "Bob", BuyInAmount: 1000m),
        };

        // Act
        await session.CreateGameAsync(options, gameId);
        await session.StartFirstHandAsync(joinSeats);
        await session.ApplyPlayerActionAsync(PlayerActionType.Fold);

        var phhSession = PhhWriter.WriteSession(session.CurrentGame!, session.Journal);

        // Assert
        phhSession.Should().Contain("[1]");
        phhSession.Should().Contain("[Game]");
    }

    [TestMethod]
    public void WriteHand_WithoutDealtHand_ThrowsInvalidOperationException()
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

        // Create a GameData with no hand (null HandData)
        var gameData = new GameData(
            GameId: gameId,
            Options: options,
            HandData: null!
        );

        // Act & Assert - Game created but no hand dealt
        try
        {
            PhhWriter.WriteHand(gameData, session.Journal);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

}
