using AntDesign.JsInterop;
using Bunit;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Demo.Components.Table;
using TabletopCardEngine.Demo.Engine;
using TabletopCardEngine.Demo.Phh;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Tournaments;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed class DemoTournament_Tests
{
    private static readonly DemoTournamentLevelPreset _turbo = DemoTournamentDefaults.LevelPresets[1];

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

    /// <summary>The services of the demo, over the play engine with the demo's own clock, as Program wires them.</summary>
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

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        RegisterDemoServices(services);
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();
        return provider;
    }

    private static TournamentOptionData SixMaxOptions(int playerCount)
        => DemoTournamentDefaults.CreateOptions(
            playerCount,
            HoldemTableFormat.SixMax,
            6,
            BettingStructure.NoLimit,
            raiseCap: null,
            DemoTournamentDefaults.StartingStack,
            DemoTournamentDefaults.BuyIn,
            DemoTournamentDefaults.Regular,
            AnteType.Classic,
            DemoTournamentDefaults.PayoutPresets[0]);

    /// <summary>Folds every seat in turn until the viewed hand is over.</summary>
    private static async Task FoldHandOutAsync(DemoGameSession session)
    {
        for (var step = 0; step < 20 && session.CurrentHand!.Street != Street.Finished; step++)
        {
            (await session.ApplyPlayerActionAsync(PlayerActionType.Fold)).Should().BeTrue();
        }

        session.CurrentHand!.Street.Should().Be(Street.Finished);
    }

    [TestMethod]
    public void TournamentOptions_ForAPreset_AreTenLevelsFromTenTwentyToThreeHundredSixHundred()
    {
        // Act
        var options = DemoTournamentDefaults.CreateOptions(
            playerCount: 12,
            HoldemTableFormat.SixMax,
            playersPerTable: 6,
            BettingStructure.PotLimit,
            raiseCap: null,
            startingStack: 1500m,
            buyIn: 10m,
            _turbo,
            AnteType.BigBlind,
            DemoTournamentDefaults.PayoutPresets[2]);

        // Assert
        options.BettingStructure.Should().Be(BettingStructure.PotLimit);
        options.AnteType.Should().Be(AnteType.BigBlind);
        options.MinEntries.Should().Be(12);
        options.StartingStack.Should().Be(1500m);
        options.BuyIn.Should().Be(10m);
        options.Payouts.Should().Equal(50m, 30m, 20m);
        options.Levels.Should().HaveCount(10);
        options.Levels.Should().OnlyContain(level => level.Duration == TimeSpan.FromMinutes(5));
        options.Levels[0].SmallBlind.Should().Be(10m);
        options.Levels[9].SmallBlind.Should().Be(300m);
        options.Levels.Take(3).Should().OnlyContain(level => level.Ante == 0m);
        options.Levels.Skip(3).Should().OnlyContain(level => level.Ante > 0m);
    }

    [TestMethod]
    public void TournamentPayoutPresets_OfferedOnlyWithNoMorePlacesThanPlayers()
    {
        DemoTournamentDefaults.PayoutPresetsFor(2).Select(preset => preset.Payouts.Count).Should().Equal(1, 2);
        DemoTournamentDefaults.PayoutPresetsFor(4).Select(preset => preset.Payouts.Count).Should().Equal(1, 2, 3);
        DemoTournamentDefaults.PayoutPresetsFor(30).Select(preset => preset.Payouts.Count).Should().Equal(1, 2, 3, 5);
    }

    [TestMethod]
    public void TournamentSetup_Defaults_BuildTheExpectedOptions()
    {
        // Arrange
        using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);

        // Act
        var component = ctx.Render<TournamentSetup>();
        var options = component.Instance.BuildOptions();

        // Assert - 12 players at six per table, Regular levels, winner takes all
        options.Should().BeEquivalentTo(SixMaxOptions(DemoTournamentDefaults.PlayerCount) with { RaiseCap = DemoGameDefaults.RaiseCap });
        options.Levels.Should().OnlyContain(level => level.Duration == TimeSpan.FromMinutes(15));
        component.Markup.Should().Contain("Players per table");
        component.Markup.Should().Contain("Blind levels");
        component.Markup.Should().Contain("Payouts");
    }

    [TestMethod]
    public async Task DemoTournamentSession_TwoPlayers_PlaysToTheEndWithScriptedAllIns()
    {
        // Arrange
        var provider = BuildProvider();
        var game = provider.GetRequiredService<DemoGameSession>();
        var tournament = provider.GetRequiredService<DemoTournamentSession>();
        var options = DemoTournamentDefaults.CreateOptions(
            2,
            HoldemTableFormat.HeadsUp,
            2,
            BettingStructure.NoLimit,
            raiseCap: null,
            1500m,
            10m,
            _turbo,
            AnteType.Classic,
            DemoTournamentDefaults.PayoutPresets[1]);

        // Act
        var started = await tournament.CreateAndStartAsync(options, DemoTournamentDefaults.CreatePlayerNames(2), seatDrawSeed: 42);
        for (var hand = 0; hand < 300 && !tournament.IsFinished; hand++)
        {
            // Everyone shoves, so a hand ends at once; a split pot keeps the stacks level and the next hand follows.
            while (game.CurrentHand!.Street != Street.Finished)
            {
                (await game.ApplyPlayerActionAsync(PlayerActionType.AllIn)).Should().BeTrue();
            }

            (await tournament.DealTableAsync(tournament.ViewedTableNo)).Should().BeTrue();
        }

        // Assert
        started.Should().BeTrue();
        tournament.SeatDrawSeed.Should().Be(42);
        tournament.IsFinished.Should().BeTrue();
        tournament.WinnerName.Should().NotBeNull();
        tournament.Tournament!.Entries.Select(entry => entry.Place).Should().BeEquivalentTo(new int?[] { 1, 2 });
        tournament.Tournament.Entries.Sum(entry => entry.Prize).Should().Be(tournament.Tournament.PrizePool);
        tournament.ViewedGame!.IsFinished.Should().BeTrue();
    }

    [TestMethod]
    public async Task DemoTournamentSession_TwelvePlayersAtSixPerTable_StartsWithTwoTablesAndSwitches()
    {
        // Arrange
        var provider = BuildProvider();
        var game = provider.GetRequiredService<DemoGameSession>();
        var tournament = provider.GetRequiredService<DemoTournamentSession>();

        // Act
        await tournament.CreateAndStartAsync(SixMaxOptions(12), DemoTournamentDefaults.CreatePlayerNames(12));
        var firstGameId = game.CurrentGameId;
        var switched = await tournament.ViewTableAsync(2);

        // Assert
        switched.Should().BeTrue();
        tournament.OpenTables.Should().HaveCount(2);
        tournament.OpenTables.Select(table => table.PlayerCount).Should().Equal(6, 6);
        tournament.ViewedTableNo.Should().Be(2);
        game.CurrentGameId.Should().Be(tournament.Tournament!.Tables[1].GameId).And.NotBe(firstGameId);
    }

    [TestMethod]
    public async Task Home_NextHand_OnATournamentDealsThroughTheTournament()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        ctx.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();
        var game = ctx.Services.GetRequiredService<DemoGameSession>();
        var tournament = ctx.Services.GetRequiredService<DemoTournamentSession>();
        await tournament.CreateAndStartAsync(SixMaxOptions(12), DemoTournamentDefaults.CreatePlayerNames(12));
        await FoldHandOutAsync(game);
        var versionBefore = tournament.Tournament!.Version;
        var component = ctx.Render<Pages.Home>();

        // Act - StartHand on a tournament table is refused (ManagedByTournament), so a second hand means DealTable ran
        component.Find(".next-hand-btn").Click();

        // Assert
        component.WaitForAssertion(() => game.CurrentHand!.No.Should().Be(2));
        tournament.Tournament!.Version.Should().BeGreaterThan(versionBefore);
        component.FindAll(".ant-alert-error").Should().BeEmpty();
    }

    [TestMethod]
    public async Task Home_NextHand_OnACashGameStartsTheNextHand()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        ctx.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();
        var game = ctx.Services.GetRequiredService<DemoGameSession>();
        await game.CreateGameAsync(DemoGameDefaults.CreateOptions());
        await game.StartFirstHandAsync(DemoGameDefaults.CreateJoinSeats());
        await FoldHandOutAsync(game);
        var component = ctx.Render<Pages.Home>();

        // Act
        component.Find(".next-hand-btn").Click();

        // Assert
        component.WaitForAssertion(() => game.CurrentHand!.No.Should().Be(2));
        ctx.Services.GetRequiredService<DemoTournamentSession>().IsActive.Should().BeFalse();
        component.FindAll(".tournament-switcher").Should().BeEmpty();
    }

    [TestMethod]
    public async Task Home_TournamentTable_HidesTheSeatControlsTheGuardRefuses()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        ctx.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();
        var game = ctx.Services.GetRequiredService<DemoGameSession>();
        var tournament = ctx.Services.GetRequiredService<DemoTournamentSession>();
        await tournament.CreateAndStartAsync(SixMaxOptions(12), DemoTournamentDefaults.CreatePlayerNames(12));
        await FoldHandOutAsync(game);

        // Act
        var component = ctx.Render<Pages.Home>();

        // Assert - between hands, where a cash game offers all four: the menu is there, every item disabled
        component.FindAll(".seat-menu-trigger").Should().HaveCount(DemoGameDefaults.MaxPlayers);
        component.FindAll(".seat-menu-trigger")[0].Click();
        var items = component.FindAll(".seat-menu-item");
        items.Select(i => i.TextContent.Trim()).Should().Equal("Sit down", "Top up", "Rebuy", "Stand up", "Kick", "Cancel sit-down");
        items.Should().OnlyContain(i => i.ClassList.Contains("ant-dropdown-menu-item-disabled"));
        component.FindAll(".next-hand-btn").Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Home_CashGameBetweenHands_StillOffersTheSeatControls()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        ctx.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();
        var game = ctx.Services.GetRequiredService<DemoGameSession>();
        await game.CreateGameAsync(DemoGameDefaults.CreateOptions());
        await game.StartFirstHandAsync(DemoGameDefaults.CreateJoinSeats());
        await FoldHandOutAsync(game);

        // Act
        var component = ctx.Render<Pages.Home>();

        // Assert
        component.FindAll(".seat-menu-trigger").Should().HaveCount(DemoGameDefaults.MaxPlayers);
        component.FindAll(".seat-menu-trigger")[0].Click();
        component.FindAll(".seat-menu-item").Where(i => i.TextContent.Trim() is "Stand up" or "Kick")
            .Should().OnlyContain(i => !i.ClassList.Contains("ant-dropdown-menu-item-disabled"));
    }

    [TestMethod]
    public async Task Home_TournamentSwitcher_ListsTheTablesAndChangesTheViewedOne()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        ctx.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();
        var game = ctx.Services.GetRequiredService<DemoGameSession>();
        var tournament = ctx.Services.GetRequiredService<DemoTournamentSession>();
        await tournament.CreateAndStartAsync(SixMaxOptions(12), DemoTournamentDefaults.CreatePlayerNames(12), seatDrawSeed: 7);
        var component = ctx.Render<Pages.Home>();

        // Assert - before
        component.FindAll(".tournament-table-btn").Select(button => button.TextContent.Trim())
            .Should().Equal("Table 1 · 6 players", "Table 2 · 6 players");
        component.Find(".tournament-seed").TextContent.Should().Be("7");
        component.Find(".tournament-table-btn-active").TextContent.Should().Contain("Table 1");

        // Act
        component.Find(".tournament-table-btn[data-table-no='2']").Click();

        // Assert
        component.WaitForAssertion(() => component.Find(".tournament-table-btn-active").TextContent.Should().Contain("Table 2"));
        game.CurrentGameId.Should().Be(tournament.Tournament!.Tables[1].GameId);
    }

    [TestMethod]
    public async Task Home_FinishedTournament_NamesTheWinnerAndOffersNoNextHand()
    {
        // Arrange
        await using var ctx = new BunitContext();
        SetupBrowserGeometryInterop(ctx);
        RegisterDemoServices(ctx.Services);
        ctx.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();
        var game = ctx.Services.GetRequiredService<DemoGameSession>();
        var tournament = ctx.Services.GetRequiredService<DemoTournamentSession>();
        var options = DemoTournamentDefaults.CreateOptions(
            2, HoldemTableFormat.HeadsUp, 2, BettingStructure.NoLimit, null, 1500m, 10m, _turbo, AnteType.Classic, DemoTournamentDefaults.PayoutPresets[0]);
        await tournament.CreateAndStartAsync(options, DemoTournamentDefaults.CreatePlayerNames(2));
        for (var hand = 0; hand < 300 && !tournament.IsFinished; hand++)
        {
            while (game.CurrentHand!.Street != Street.Finished)
            {
                await game.ApplyPlayerActionAsync(PlayerActionType.AllIn);
            }

            await tournament.DealTableAsync(tournament.ViewedTableNo);
        }

        // Act
        var component = ctx.Render<Pages.Home>();

        // Assert
        tournament.IsFinished.Should().BeTrue();
        component.Find(".tournament-finished").TextContent.Should().Contain($"{tournament.WinnerName} won");
        component.FindAll(".next-hand-btn").Should().BeEmpty();
    }
}
