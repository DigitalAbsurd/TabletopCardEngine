using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Options;
using TabletopCardEngine.InMemory;
using TabletopCardEngine.OpenTelemetry;
using TabletopCardEngine.PostgreSql;
using TabletopCardEngine.Sqlite;
using TabletopCardEngine.SqlServer;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Verifies that the public API surface complies with the 1.0 design: <see cref="IHoldemTable"/>
/// is a public interface whose implementation is internal and resolvable only through dependency injection.
/// The root namespace holds only game-agnostic types, each game lives in <c>TabletopCardEngine.&lt;Game&gt;</c>,
/// and the retired engine-named API (ADR-0008) is kept out.
/// </summary>
[TestClass]
public sealed class PublicSurfaceTests
{
    [TestMethod]
    public void HoldemTable_IsInternal()
    {
        typeof(IHoldemTable).IsPublic.Should().BeTrue("IHoldemTable is the public entry point");
        typeof(IHoldemTable).IsInterface.Should().BeTrue("consumers must be able to substitute the table in their tests");

        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(opts => opts.UseHoldemInMemory(Guid.NewGuid().ToString("N"))));
        using var provider = services.BuildServiceProvider();

        var table = provider.GetRequiredService<IHoldemTable>();

        table.GetType().IsPublic.Should().BeFalse("the implementation is reached only through IHoldemTable");
        typeof(IHoldemTable).Assembly.GetExportedTypes()
            .Where(type => type != typeof(IHoldemTable) && typeof(IHoldemTable).IsAssignableFrom(type))
            .Should().BeEmpty("no public type implements IHoldemTable");
    }

    [TestMethod]
    public void HoldemActionCommands_AreNamedAfterTheirVerb()
    {
        var commands = GetCommandTypes();

        commands.Should().NotBeEmpty();
        commands.Should().OnlyContain(
            type => type.Name.StartsWith("HoldemAction", StringComparison.Ordinal) && type.Name.Length > "HoldemAction".Length);
    }

    [TestMethod]
    public void HoldemAction_HasExactlyOneFactoryPerCommand()
    {
        var factories = typeof(HoldemAction)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToList();

        factories.Select(method => method.ReturnType).Should().BeEquivalentTo(
            GetCommandTypes(), "each command has exactly one factory and it returns that command");
        factories.Should().OnlyContain(method => "HoldemAction" + method.Name == method.ReturnType.Name);
    }

    private static List<Type> GetCommandTypes()
        => typeof(HoldemAction).Assembly.GetExportedTypes()
            .Where(type => type.IsSubclassOf(typeof(HoldemAction)) && !type.IsAbstract)
            .ToList();

    [TestMethod]
    public void PublicTypes_AreNotInDataNamespace()
    {
        var offenders = typeof(IHoldemTable).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "TabletopCardEngine.Holdem.Data")
            .Select(type => type.FullName)
            .ToList();

        offenders.Should().BeEmpty("the Data namespace holds internal persistence entities only");
    }

    [TestMethod]
    public void HoldemOptions_IsNotPublic()
    {
        typeof(HoldemOptions).IsPublic.Should().BeFalse(
            "HoldemOptions is an implementation detail and must not be part of the public API");
    }

    [TestMethod]
    public async Task IHoldemTable_ResolvesFromContainer()
    {
        var gameId = Guid.NewGuid();
        var gameOptions = new GameOptionData { MaxPlayersCount = 6, InitialSmallBlind = 1, InitialDealerSeatNo = 6 };

        var seats = new[]
        {
            new PlayerJoinData("Alice", 1, 100m),
            new PlayerJoinData("Bob", 2, 100m),
            new PlayerJoinData("Charlie", 3, 100m),
            new PlayerJoinData("Diana", 4, 100m),
            new PlayerJoinData("Eve", 5, 100m),
            new PlayerJoinData("Frank", 6, 100m),
        };

        using (TestEngineHost.Setup(out var engine, out _))
        {
            // Create a game
            var createResult = await engine.ApplyAsync(
                new HoldemActionCreateGame(gameId, gameOptions),
                CancellationToken.None);

            createResult.IsSuccess.Should().BeTrue("creating a game through the resolved engine should succeed");
            createResult.Value.GameId.Should().Be(gameId);

            // Start a hand to verify the resolved engine is fully functional
            var startResult = await engine.ApplyAsync(
                new HoldemActionStartGame(gameId, seats),
                CancellationToken.None);

            startResult.IsSuccess.Should().BeTrue("starting a hand should succeed");
        }
    }

    [TestMethod]
    public void RootNamespace_HoldsOnlyTheGameAgnosticTypes()
    {
        var rootTypes = typeof(IHoldemTable).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "TabletopCardEngine")
            .Select(type => type.Name)
            .ToList();

        var expected = new[] { "Card", "CardSuit", "CardType", "TabletopCardEngineBuilder", "TabletopCardEngineServiceCollectionExtensions", "TabletopCardEngineTelemetry" };
        rootTypes.Should().BeEquivalentTo(expected, "the root namespace holds only game-agnostic types; game-specific types live in TabletopCardEngine.<Game>");
    }

    [TestMethod]
    public void PublicTypes_LiveInTheRootOrAGameNamespace()
    {
        var offenders = typeof(IHoldemTable).Assembly.GetExportedTypes()
            .Where(type => type.Namespace != "TabletopCardEngine" && !type.Namespace!.StartsWith("TabletopCardEngine.Holdem", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToList();

        offenders.Should().BeEmpty("every exported type must be in the root TabletopCardEngine namespace or under TabletopCardEngine.Holdem");
    }

    [TestMethod]
    public void PublicTypes_DoNotNameTheEngineAfterAGame()
    {
        var assemblies = new[]
        {
            typeof(IHoldemTable).Assembly,
            typeof(TabletopCardEngineInstrumentationExtensions).Assembly,
            typeof(HoldemSqliteDbContextOptionsBuilderExtensions).Assembly,
            typeof(HoldemPostgreSqlDbContextOptionsBuilderExtensions).Assembly,
            typeof(HoldemSqlServerDbContextOptionsBuilderExtensions).Assembly,
            typeof(HoldemInMemoryDbContextOptionsBuilderExtensions).Assembly,
        };

        var retiredEngineName = "Holdem" + "Engine";
        var offenders = assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.Name.Contains(retiredEngineName, StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToList();

        offenders.Should().BeEmpty("no exported type name in the core or provider packages must contain the retired engine-named API (ADR-0008)");
    }
}
