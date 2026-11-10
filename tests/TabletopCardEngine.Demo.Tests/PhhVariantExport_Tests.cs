using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Demo.Engine;
using TabletopCardEngine.Demo.Phh;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed class PhhVariantExport_Tests
{
    private static (DemoGameSession Session, PhhCardShuffler Shuffler) CreateSession()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(
            dbOptionsAction: opts => opts.UseHoldemSqlite(connection),
            configure: _ => { }));
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();

        var shuffler = new PhhCardShuffler();
        var replayEngine = DemoReplayEngine.Create(connection, shuffler);
        return (new DemoGameSession(provider.GetRequiredService<IHoldemTable>(), replayEngine), shuffler);
    }

    /// <summary>Plays a heads-up hand to the end: the first seat raises, then everybody calls or checks down.</summary>
    private static async Task<DemoGameSession> PlayHandAsync(BettingStructure structure)
    {
        var (session, _) = CreateSession();
        var options = new GameOptionData
        {
            BettingStructure = structure,
            RaiseCap = 4,
            TableFormat = HoldemTableFormat.HeadsUp,
            MaxPlayersCount = 2,
            InitialSmallBlind = 50m,
            InitialDealerSeatNo = 1,
        };

        await session.CreateGameAsync(options, Guid.NewGuid());
        await session.StartFirstHandAsync(
        [
            new(SeatNo: 1, Name: "Alice", BuyInAmount: 5000m),
            new(SeatNo: 2, Name: "Bob", BuyInAmount: 5000m),
        ]);

        var raised = false;
        while (session.CurrentHand!.NextPlayerSeatNo != 0)
        {
            var evaluations = session.CurrentHand.Evaluations;
            var raise = evaluations.FirstOrDefault(e => e.PlayerActionType == PlayerActionType.Raise);
            if (!raised && raise != null)
            {
                raised = true;
                (await session.ApplyPlayerActionAsync(PlayerActionType.Raise, raise.MinValue)).Should().BeTrue();
                continue;
            }

            var type = evaluations.Any(e => e.PlayerActionType == PlayerActionType.Check) ? PlayerActionType.Check : PlayerActionType.Call;
            (await session.ApplyPlayerActionAsync(type)).Should().BeTrue();
        }

        return session;
    }

    private static Dictionary<string, decimal> FinalStacks(GameData game)
        => game.HandData!.Seats.ToDictionary(s => s.Name, s => s.StartHandStack + s.HandResult!.Net);

    [TestMethod]
    [DataRow(BettingStructure.FixedLimit, "FT")]
    [DataRow(BettingStructure.PotLimit, "PT")]
    [DataRow(BettingStructure.NoLimit, "NT")]
    public async Task WriteSession_RoundTripsThroughTheReaderAndReplaysToTheSameStacks(BettingStructure structure, string variant)
    {
        // Arrange
        var played = await PlayHandAsync(structure);
        var expectedStacks = FinalStacks(played.CurrentGame!);

        // Act
        var text = PhhWriter.WriteSession(played.CurrentGame!, played.Journal);
        var hand = PhhReader.ReadAll(text).Single();

        // Assert: the reader and the support check accept the export
        hand.Variant.Should().Be(variant);
        PhhSupport.IsSupported(hand, out var reason).Should().BeTrue(reason);
        hand.Players.Zip(hand.FinishingStacks!).ToDictionary(p => p.First, p => p.Second).Should().Equal(expectedStacks);

        // Act: replay it in a fresh session
        var (session, shuffler) = CreateSession();
        var controller = new PhhReplayController(session, shuffler);
        (await controller.LoadHandAsync(hand)).Should().BeTrue(controller.ReplayError);
        session.CurrentGame!.Options.BettingStructure.Should().Be(structure);
        while (!controller.IsComplete)
        {
            (await controller.ReplayNextActionAsync()).Should().BeTrue(controller.ReplayError);
        }

        // Assert
        FinalStacks(session.CurrentGame!).Should().Equal(expectedStacks);
        controller.FinalStacksMatch.Should().BeTrue(controller.FinalStacksMessage);
    }

    [TestMethod]
    public async Task WriteSession_FixedLimit_WritesBetSizesAndNoMinBet()
    {
        var played = await PlayHandAsync(BettingStructure.FixedLimit);

        var text = PhhWriter.WriteSession(played.CurrentGame!, played.Journal);

        text.Should().Contain("variant = 'FT'").And.Contain("small_bet = 100").And.Contain("big_bet = 200");
        text.Should().NotContain("min_bet");
    }

    [TestMethod]
    public async Task WriteSession_PotLimit_WritesTheBigBlindAsMinBetAndAComment()
    {
        var played = await PlayHandAsync(BettingStructure.PotLimit);

        var text = PhhWriter.WriteSession(played.CurrentGame!, played.Journal);

        text.Should().Contain("variant = 'PT'").And.Contain("min_bet = 100");
        text.Split('\n')[0].Should().StartWith("#").And.Contain("non-standard");
    }

    [TestMethod]
    public async Task WriteSession_NoLimit_KeepsTheLinesItAlwaysWrote()
    {
        var played = await PlayHandAsync(BettingStructure.NoLimit);

        var text = PhhWriter.WriteSession(played.CurrentGame!, played.Journal);

        var lines = text.Split('\n');
        lines[0].Should().Be("[1]");
        lines[1].Should().Be("variant = 'NT'");
        lines.Should().Contain("min_bet = 100");
        text.Should().NotContain("small_bet").And.NotContain("#");
    }
}
