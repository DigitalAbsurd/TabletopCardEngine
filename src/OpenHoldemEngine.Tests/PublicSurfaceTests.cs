using FluentAssertions;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine.Tests;

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
    public void PublicTypes_AreNotInDataNamespace()
    {
        var offenders = typeof(HoldemEngine).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "OpenHoldemEngine.Data")
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
        var gameOptions = new GameOptionData(
            MaxPlayersCount: 6,
            InitialSmallBlind: 1,
            InitialDealerSeatNo: 6,
            AllowRebuy: false,
            MaxHandsCount: 100,
            AutoIncrementsHands: 0);

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
                new HoldemActionStartHand(gameId) { JoinSeats = seats },
                CancellationToken.None);

            startResult.IsSuccess.Should().BeTrue("starting a hand should succeed");
        }
    }
}
