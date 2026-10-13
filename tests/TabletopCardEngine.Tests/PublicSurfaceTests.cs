using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Options;
using TabletopCardEngine.InMemory;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Verifies that the public API surface complies with the 1.0 design: <see cref="IHoldemTable"/>
/// is a public interface whose implementation is internal and resolvable only through dependency injection.
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
        services.AddHoldemEngine(opts => opts.UseHoldemInMemory(Guid.NewGuid().ToString("N")));
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
            .Where(type => type.Namespace == "TabletopCardEngine.Data")
            .Select(type => type.FullName)
            .ToList();

        offenders.Should().BeEmpty("the Data namespace holds internal persistence entities only");
    }

    [TestMethod]
    public void HoldemEngineOptions_IsNotPublic()
    {
        typeof(HoldemEngineOptions).IsPublic.Should().BeFalse(
            "HoldemEngineOptions is an implementation detail and must not be part of the public API");
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
}
