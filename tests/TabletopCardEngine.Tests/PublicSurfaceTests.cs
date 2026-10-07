using FluentAssertions;
using System.Reflection;
using TabletopCardEngine.Dto;
using TabletopCardEngine.Options;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Verifies that the public API surface complies with the 1.0 design: <see cref="HoldemEngine"/>
/// is resolvable only through dependency injection, not through a public constructor.
/// </summary>
[TestClass]
public sealed class PublicSurfaceTests
{
    [TestMethod]
    public void HoldemEngine_HasNoPublicConstructor()
    {
        typeof(HoldemEngine).GetConstructors().Should().BeEmpty(
            "HoldemEngine must be resolvable only through dependency injection");
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
        var offenders = typeof(HoldemEngine).Assembly.GetExportedTypes()
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
    public async Task HoldemEngine_ResolvesFromContainer()
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
            var createResult = await engine.ApplyHoldemActionAsync(
                new HoldemActionCreateGame(gameId, gameOptions),
                CancellationToken.None);

            createResult.IsSuccess.Should().BeTrue("creating a game through the resolved engine should succeed");
            createResult.Value.GameId.Should().Be(gameId);

            // Start a hand to verify the resolved engine is fully functional
            var startResult = await engine.ApplyHoldemActionAsync(
                new HoldemActionStartGame(gameId, seats),
                CancellationToken.None);

            startResult.IsSuccess.Should().BeTrue("starting a hand should succeed");
        }
    }
}
