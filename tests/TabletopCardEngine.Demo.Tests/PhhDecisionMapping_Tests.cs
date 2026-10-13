using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Demo.Phh;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed class PhhDecisionMapping_Tests
{
    private static IHoldemTable CreateTestEngine()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(
            dbOptionsAction: opts => opts.UseHoldemSqlite(connection),
            configure: _ => { }
        ));

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();

        return serviceProvider.GetRequiredService<IHoldemTable>();
    }

    [TestMethod]
    public async Task FoldAction_MapsToFold()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var gameId = Guid.NewGuid();
        var options = new GameOptionData { MaxPlayersCount = 2, InitialSmallBlind = 50m, InitialDealerSeatNo = 1 };

        var joinSeats = new PlayerJoinData[]
        {
            new(SeatNo: 1, Name: "Player 1", BuyInAmount: 1000m),
            new(SeatNo: 2, Name: "Player 2", BuyInAmount: 1000m),
        };

        // Act - Create game and start hand
        await session.CreateGameAsync(options, gameId);
        await session.StartFirstHandAsync(joinSeats);

        var actionType = PlayerActionType.Fold;
        var result = await session.ApplyPlayerActionAsync(actionType);

        // Assert
        result.Should().BeTrue();
        session.CurrentHand.Should().NotBeNull();
        var lastEntry = session.Journal.Entries[session.Journal.Entries.Count - 1];
        lastEntry.Action.Should().BeOfType<HoldemActionPlay>();
        ((HoldemActionPlay)lastEntry.Action).ActionType.Should().Be(PlayerActionType.Fold);
    }

    [TestMethod]
    public void PhhActionParser_FoldAction_ParsesCorrectly()
    {
        // Arrange
        var rawAction = "p1 f";

        // Act
        var action = PhhActionParser.Parse(rawAction);

        // Assert
        action.Should().BeOfType<PhhFoldAction>();
        ((PhhFoldAction)action).PlayerIndex.Should().Be(0);
    }

    [TestMethod]
    public void PhhActionParser_CheckCallAction_ParsesCorrectly()
    {
        // Arrange
        var rawAction = "p2 cc";

        // Act
        var action = PhhActionParser.Parse(rawAction);

        // Assert
        action.Should().BeOfType<PhhCheckCallAction>();
        ((PhhCheckCallAction)action).PlayerIndex.Should().Be(1);
    }

    [TestMethod]
    public void PhhActionParser_CompleteBetRaiseAction_ParsesCorrectly()
    {
        // Arrange
        var rawAction = "p3 cbr 300";

        // Act
        var action = PhhActionParser.Parse(rawAction);

        // Assert
        action.Should().BeOfType<PhhCompleteBetRaiseAction>();
        var raise = (PhhCompleteBetRaiseAction)action;
        raise.PlayerIndex.Should().Be(2);
        raise.Amount.Should().Be(300m);
    }

    [TestMethod]
    public void PhhActionParser_DealHoleAction_ParsesCorrectly()
    {
        // Arrange - Cards should be concatenated without spaces
        var rawAction = "d dh p1 AhKd";

        // Act
        var action = PhhActionParser.Parse(rawAction);

        // Assert
        action.Should().BeOfType<PhhDealHoleAction>();
        var dealHole = (PhhDealHoleAction)action;
        dealHole.PlayerIndex.Should().Be(0);
        dealHole.Cards.Should().HaveCount(2);
    }

    [TestMethod]
    public void PhhActionParser_DealBoardAction_ParsesCorrectly()
    {
        // Arrange - Cards should be concatenated without spaces
        var rawAction = "d db 2h3s4c";

        // Act
        var action = PhhActionParser.Parse(rawAction);

        // Assert
        action.Should().BeOfType<PhhDealBoardAction>();
        var dealBoard = (PhhDealBoardAction)action;
        dealBoard.Cards.Should().HaveCount(3);
    }

    [TestMethod]
    public void PhhActionParser_ActionWithComment_ParsesCorrectly()
    {
        // Arrange
        var rawAction = "p1 f # This is a comment";

        // Act
        var action = PhhActionParser.Parse(rawAction);

        // Assert
        action.Should().BeOfType<PhhFoldAction>();
        ((PhhFoldAction)action).PlayerIndex.Should().Be(0);
    }

    [TestMethod]
    public void PhhActionParser_InvalidAction_ThrowsFormatException()
    {
        // Arrange
        var rawAction = "invalid action";

        // Act & Assert
        try
        {
            PhhActionParser.Parse(rawAction);
            Assert.Fail("Expected FormatException");
        }
        catch (FormatException)
        {
            // Expected
        }
    }
}
