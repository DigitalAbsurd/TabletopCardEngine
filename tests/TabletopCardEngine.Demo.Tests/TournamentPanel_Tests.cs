using AntDesign.JsInterop;
using Bunit;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Demo.Components.Tournaments;
using TabletopCardEngine.Demo.Engine;
using TabletopCardEngine.Demo.Phh;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Tournaments;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed class TournamentPanel_Tests
{
    private static void SetupBrowserGeometryInterop(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop
            .Setup<Window>(AntDesign.JSInteropConstants.GetWindow)
            .SetResult(new Window { InnerWidth = 1920, InnerHeight = 1080 });
        ctx.JSInterop
            .Setup<DomRect>("AntDesign.interop.domInfoHelper.getBoundingClientRect", _ => true)
            .SetResult(new DomRect { Width = 320, Height = 32 });
        ctx.JSInterop
            .Setup<HtmlElement>("AntDesign.interop.domInfoHelper.getInfo", _ => true)
            .SetResult(new HtmlElement());
    }

    private static void RegisterDemoServices(IServiceCollection services)
    {
        services.AddAntDesign();
        services.AddScoped<BrowserStorage>();
        services.AddScoped<DemoTheme>();
        services.AddScoped<WalkthroughState>();
        services.AddScoped<NewGameState>();

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var clock = new DemoClock();
        services.AddSingleton(clock);
        services.AddTabletopCardEngine(tce => tce.AddHoldem(
            dbOptionsAction: opts => opts.UseHoldemSqlite(connection),
            configure: options => options.WithTimeProvider(clock)));
        services.AddSingleton(connection);
        services.AddSingleton<PhhCardShuffler>();
        services.AddSingleton(serviceProvider =>
            DemoReplayEngine.Create(connection, serviceProvider.GetRequiredService<PhhCardShuffler>()));

        services.AddScoped<SqliteBrowser>();
        services.AddScoped<DemoDatabase>();
        services.AddScoped<DemoGameSession>();
        services.AddScoped<DemoTournamentSession>();
        services.AddScoped<EngineCallSnippetWriter>();
        services.AddScoped<PhhReplayController>();
    }

    private static TournamentOptionData TwelvePlayerOptions()
        => DemoTournamentDefaults.CreateOptions(
            12,
            HoldemTableFormat.SixMax,
            6,
            BettingStructure.NoLimit,
            raiseCap: null,
            DemoTournamentDefaults.StartingStack,
            DemoTournamentDefaults.BuyIn,
            DemoTournamentDefaults.Regular,
            AnteType.Classic,
            DemoTournamentDefaults.PayoutPresets[0]);

    private static async Task<TournamentData> StartTournamentAsync(BunitContext ctx)
    {
        ctx.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();
        var session = ctx.Services.GetRequiredService<DemoTournamentSession>();
        (await session.CreateAndStartAsync(TwelvePlayerOptions(), DemoTournamentDefaults.CreatePlayerNames(12), seatDrawSeed: 1)).Should().BeTrue();
        return session.Tournament!;
    }

    [TestMethod]
    public void TournamentLevelClock_ForAGivenClock_ShowsTheTimeLeftAsMinutesAndSeconds()
    {
        // Arrange
        using var ctx = new BunitContext();
        var clock = new DemoClock();
        ctx.Services.AddSingleton(clock);
        var endsAt = clock.GetUtcNow() + TimeSpan.FromMinutes(5);

        // Act
        var before = ctx.Render<TournamentLevelClock>(parameters => parameters.Add(p => p.LevelEndsAt, endsAt));
        clock.Advance(TimeSpan.FromSeconds(270));
        var after = ctx.Render<TournamentLevelClock>(parameters => parameters.Add(p => p.LevelEndsAt, endsAt));
        clock.Advance(TimeSpan.FromMinutes(10));
        var past = ctx.Render<TournamentLevelClock>(parameters => parameters.Add(p => p.LevelEndsAt, endsAt));

        // Assert
        before.Find(".tournament-level-clock").TextContent.Should().Be("05:00");
        after.Find(".tournament-level-clock").TextContent.Should().Be("00:30");
        past.Find(".tournament-level-clock").TextContent.Should().Be("00:00");
    }

    [TestMethod]
    public void TournamentLevelClock_Format_RoundsUpAndAddsHoursFromAnHour()
    {
        TournamentLevelClock.Format(TimeSpan.FromMilliseconds(59_001)).Should().Be("01:00");
        TournamentLevelClock.Format(TimeSpan.FromSeconds(3725)).Should().Be("1:02:05");
        TournamentLevelClock.Format(TimeSpan.FromSeconds(-3)).Should().Be("00:00");
    }

    [TestMethod]
    public async Task TournamentPanel_SkipToNextLevel_MovesTheClockToTheEndOfTheLevel()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        var tournament = await StartTournamentAsync(ctx);
        var clock = ctx.Services.GetRequiredService<DemoClock>();
        var component = ctx.Render<TournamentPanel>(parameters => parameters
            .Add(p => p.Tournament, tournament)
            .Add(p => p.ViewedTableNo, 1));
        component.Find(".tournament-level-now").TextContent.Should().Be("Level 1 · Blinds 10/20");
        component.Find(".tournament-level-next").TextContent.Should().StartWith("Next: ");

        // Act
        component.Find(".tournament-skip-level").Click();

        // Assert
        clock.GetUtcNow().Should().BeOnOrAfter(tournament.LevelEndsAt!.Value);
        component.Find(".tournament-level-clock").TextContent.Should().Be("00:00");
    }

    [TestMethod]
    public async Task TournamentPanel_SkipToNextLevelTwice_DealsTheNextHandAtLevelThree()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        var tournament = await StartTournamentAsync(ctx);
        var session = ctx.Services.GetRequiredService<DemoTournamentSession>();
        var game = ctx.Services.GetRequiredService<DemoGameSession>();
        var component = ctx.Render<TournamentPanel>(parameters => parameters
            .Add(p => p.Tournament, tournament)
            .Add(p => p.ViewedTableNo, 1));
        for (var step = 0; step < 20 && game.CurrentHand!.Street != Street.Finished; step++)
        {
            (await game.ApplyPlayerActionAsync(PlayerActionType.Fold)).Should().BeTrue();
        }

        // Act - the panel still holds the tournament as it was before the first skip, as it does until a hand is dealt
        component.Find(".tournament-skip-level").Click();
        component.Find(".tournament-skip-level").Click();
        (await session.DealTableAsync(1)).Should().BeTrue();

        // Assert
        var level = tournament.Options.Levels[2];
        game.CurrentHand!.BlindLevel.Should().Be(2);
        game.CurrentHand.SmallBlind.Should().Be(level.SmallBlind);
        game.CurrentHand.Ante.Should().Be(level.Ante);
        session.Tournament!.LevelIndex.Should().Be(2);
    }

    [TestMethod]
    public async Task TournamentPanel_OnTheLastLevel_SaysFinalLevelAndOffersNoSkip()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        var tournament = await StartTournamentAsync(ctx);
        var lastIndex = tournament.Options.Levels.Count - 1;
        var last = tournament with { LevelIndex = lastIndex, LevelEndsAt = null };

        // Act
        var component = ctx.Render<TournamentPanel>(parameters => parameters.Add(p => p.Tournament, last));

        // Assert
        component.Find(".tournament-level-next").TextContent.Should().Be("Final level");
        component.FindAll(".tournament-skip-level").Should().BeEmpty();
        component.FindAll(".tournament-level-clock").Should().BeEmpty();
    }

    [TestMethod]
    public async Task TournamentPanel_Render_ListsPlayersTablesAndPayouts()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        var tournament = await StartTournamentAsync(ctx);
        var picked = 0;

        // Act
        var component = ctx.Render<TournamentPanel>(parameters => parameters
            .Add(p => p.Tournament, tournament)
            .Add(p => p.ViewedTableNo, 1)
            .Add(p => p.OnSelectTable, tableNo => picked = tableNo));
        component.Find(".tournament-panel-table-btn[data-table-no='2']").Click();

        // Assert
        component.FindAll(".tournament-entry-in").Should().HaveCount(12);
        component.Find(".tournament-standings-summary").TextContent.Should().Contain("Players left 12/12").And.Contain("Average stack");
        component.Find(".tournament-prize-pool").TextContent.Should().Contain(AmountFormat.Format(tournament.PrizePool));
        component.FindAll(".tournament-panel-table-btn").Select(button => button.TextContent.Trim())
            .Should().Equal("Table 1 · 6 players", "Table 2 · 6 players");
        picked.Should().Be(2);
    }

    [TestMethod]
    public void TournamentPanel_StandingsOrder_PutsPlayersInByTableThenThoseOutByPlace()
    {
        // Arrange
        var entries = new[]
        {
            new TournamentEntryData(1, "Out third") { State = TournamentEntryState.Eliminated, Place = 3 },
            new TournamentEntryData(2, "Table 2 seat 1") { State = TournamentEntryState.Seated, TableNo = 2, SeatNo = 1 },
            new TournamentEntryData(3, "Out fourth") { State = TournamentEntryState.Eliminated, Place = 4 },
            new TournamentEntryData(4, "Table 1 seat 5") { State = TournamentEntryState.Seated, TableNo = 1, SeatNo = 5 },
            new TournamentEntryData(5, "Table 1 seat 2") { State = TournamentEntryState.Seated, TableNo = 1, SeatNo = 2 },
            new TournamentEntryData(6, "Moving") { State = TournamentEntryState.Moving },
        };

        // Act
        var ordered = TournamentPanel.StandingsOrder(entries).Select(entry => entry.Name);

        // Assert
        ordered.Should().Equal("Table 1 seat 2", "Table 1 seat 5", "Table 2 seat 1", "Moving", "Out third", "Out fourth");
    }

    [TestMethod]
    public void TournamentPanel_PayoutRows_SplitThePoolByThePercentages()
    {
        // Act
        var rows = TournamentPanel.PayoutRows([50m, 30m, 20m], 60m);

        // Assert
        rows.Select(row => row.Place).Should().Equal(1, 2, 3);
        rows.Select(row => row.Amount).Should().Equal(30m, 18m, 12m);
    }

    [TestMethod]
    public async Task TournamentPanel_AfterABust_ShowsThePlaceAndGreysABrokenTable()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        var tournament = await StartTournamentAsync(ctx);
        var first = tournament.Entries[0];
        var entries = tournament.Entries
            .Select(entry => entry.EntryNo == first.EntryNo
                ? entry with { State = TournamentEntryState.Eliminated, TableNo = null, SeatNo = null, Place = 12 }
                : entry)
            .ToArray();
        var afterBust = tournament with
        {
            Entries = entries,
            Tables = [tournament.Tables[0] with { IsBroken = true, PlayerCount = 0 }, tournament.Tables[1]],
        };

        // Act
        var component = ctx.Render<TournamentPanel>(parameters => parameters.Add(p => p.Tournament, afterBust));

        // Assert
        component.Find(".tournament-entry-out .tournament-entry-where").TextContent.Should().Be("12th");
        component.Find(".tournament-standings-summary").TextContent.Should().Contain("Players left 11/12");
        component.Find(".tournament-panel-table-broken").HasAttribute("disabled").Should().BeTrue();
    }
}
